using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace WindowsAutoPowerManager.Functions
{
    public class BleDeviceInfo
    {
        public ulong BluetoothAddress { get; set; }
        public string MacAddress { get; set; }
        public string LocalName { get; set; }
        public short RssiDbm { get; set; }
        public DateTime LastSeen { get; set; }
    }

    internal static class BluetoothScanner
    {
        private static DeviceWatcher _classicBtWatcher;
        private static DeviceWatcher _classicMonitorWatcher;
        private static Timer _classicMonitorHeartbeatTimer;

        private static readonly ConcurrentDictionary<ulong, BleDeviceInfo> _discoveredDevices = new();
        private static readonly ConcurrentDictionary<ulong, DateTime> _monitorLastSeen = new();
        private static readonly ConcurrentDictionary<ulong, short> _monitorLastRssi = new();
        private static readonly ConcurrentDictionary<string, ulong> _classicDiscoveryIdToAddress = new();
        private static readonly ConcurrentDictionary<string, ulong> _classicMonitorIdToAddress = new();
        private static readonly ConcurrentDictionary<ulong, byte> _classicMonitorPresent = new();
        private static readonly ConcurrentDictionary<ulong, int> _classicMonitorConsecutiveMisses = new();
        private static int _classicPresenceRefreshInProgress;
        private static int _classicPresenceRefreshFailureCount;
        private static long _lastClassicPresenceRefreshTick = -ClassicPresenceRefreshIntervalMs;

        private static readonly object _discoveryLock = new();
        private static readonly object _monitorLock = new();

        private static bool _isDiscovering;
        private static bool _isMonitoring;
        private static int _discoveryVersion;

        public static event Action<BleDeviceInfo> DeviceDiscovered;

        // Bluetooth protocol IDs for DeviceWatcher
        private const string ClassicBtProtocolId = "{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}";
        private const int ClassicPresenceRefreshIntervalMs = 5000;
        private const int ClassicPresenceRefreshFailureTolerance = 3;
        private const int ClassicDeviceMissTolerance = 3;

        // ---------- Discovery Scan (UI device list) ----------

        public static void StartDiscoveryScan()
        {
            lock (_discoveryLock)
            {
                if (_isDiscovering) return;

                _discoveredDevices.Clear();
                _classicDiscoveryIdToAddress.Clear();
                Interlocked.Increment(ref _discoveryVersion);

                // Classic Bluetooth device watcher (live inquiry scan)
                try
                {
                    string classicSelector = "System.Devices.Aep.ProtocolId:=\"" + ClassicBtProtocolId + "\"";
                    var classicProps = new string[]
                    {
                        "System.Devices.Aep.DeviceAddress",
                        "System.ItemNameDisplay",
                        "System.Devices.Aep.IsPaired",
                        "System.Devices.Aep.IsPresent"
                    };
                    _classicBtWatcher = DeviceInformation.CreateWatcher(
                        classicSelector, classicProps, DeviceInformationKind.AssociationEndpoint);

                    _classicBtWatcher.Added += OnClassicDeviceAdded;
                    _classicBtWatcher.Updated += OnClassicDeviceUpdated;
                    _classicBtWatcher.Start();
                }
                catch
                {
                    _classicBtWatcher = null;
                }

                _isDiscovering = true;
            }
        }

        public static void StopDiscoveryScan()
        {
            lock (_discoveryLock)
            {
                if (!_isDiscovering) return;

                if (_classicBtWatcher != null)
                {
                    try
                    {
                        _classicBtWatcher.Added -= OnClassicDeviceAdded;
                        _classicBtWatcher.Updated -= OnClassicDeviceUpdated;
                        _classicBtWatcher.Stop();
                    }
                    catch { }
                    _classicBtWatcher = null;
                }

                _classicDiscoveryIdToAddress.Clear();
                _isDiscovering = false;
                Interlocked.Increment(ref _discoveryVersion);
            }
        }

        public static List<BleDeviceInfo> GetDiscoveredDevices()
        {
            return _discoveredDevices.Values
                .Where(d => !string.IsNullOrEmpty(d.LocalName))
                .OrderByDescending(d => d.RssiDbm)
                .ToList();
        }

        public static bool TryGetDiscoveredDevicesIfChanged(ref int knownVersion, out List<BleDeviceInfo> devices)
        {
            int latestVersion = Volatile.Read(ref _discoveryVersion);
            if (latestVersion == knownVersion)
            {
                devices = null;
                return false;
            }

            knownVersion = latestVersion;
            devices = GetDiscoveredDevices();
            return true;
        }

        private static void OnClassicDeviceAdded(DeviceWatcher sender, DeviceInformation info)
        {
            AddOrUpdateClassicDevice(info?.Id, info?.Name, info?.Properties);
        }

        private static void OnClassicDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            AddOrUpdateClassicDevice(update?.Id, null, update?.Properties);
        }

        private static void AddOrUpdateClassicDevice(
            string deviceId,
            string name,
            IReadOnlyDictionary<string, object> properties)
        {
            string deviceAddress = string.Empty;
            if (properties != null &&
                properties.TryGetValue("System.Devices.Aep.DeviceAddress", out object addr))
            {
                deviceAddress = addr?.ToString() ?? string.Empty;
            }

            ulong macLong = MacStringToUlong(deviceAddress);
            if (macLong == 0 &&
                !string.IsNullOrWhiteSpace(deviceId) &&
                _classicDiscoveryIdToAddress.TryGetValue(deviceId, out ulong knownAddress))
            {
                macLong = knownAddress;
            }

            if (macLong == 0) return;

            // Skip devices that Windows reports as not currently present (stale cache).
            if (TryGetBoolProperty(properties, "System.Devices.Aep.IsPresent", out bool isPresent) && !isPresent)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                _classicDiscoveryIdToAddress[deviceId] = macLong;
            }

            string displayName = ResolveClassicDeviceName(
                name,
                properties,
                macLong,
                out bool usedAddressFallback);
            var deviceInfo = new BleDeviceInfo
            {
                BluetoothAddress = macLong,
                MacAddress = FormatMacAddress(macLong),
                LocalName = displayName,
                RssiDbm = 0,
                LastSeen = DateTime.Now
            };

            bool changed = false;
            _discoveredDevices.AddOrUpdate(
                macLong,
                _ =>
                {
                    changed = true;
                    return deviceInfo;
                },
                (key, existing) =>
                {
                    bool canUpdateName = !usedAddressFallback || string.IsNullOrWhiteSpace(existing.LocalName);
                    if (!string.IsNullOrEmpty(displayName) && canUpdateName)
                    {
                        if (!string.Equals(existing.LocalName, displayName, StringComparison.Ordinal))
                        {
                            changed = true;
                        }

                        existing.LocalName = displayName;
                    }
                    existing.LastSeen = DateTime.Now;
                    return existing;
                });

            if (changed)
            {
                Interlocked.Increment(ref _discoveryVersion);
            }

            DeviceDiscovered?.Invoke(deviceInfo);
        }

        private static string ResolveClassicDeviceName(
            string name,
            IReadOnlyDictionary<string, object> properties,
            ulong bluetoothAddress,
            out bool usedAddressFallback)
        {
            usedAddressFallback = false;

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            if (properties != null &&
                properties.TryGetValue("System.ItemNameDisplay", out object displayName))
            {
                string resolved = displayName?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    return resolved;
                }
            }

            usedAddressFallback = true;
            return FormatMacAddress(bluetoothAddress);
        }

        // ---------- Background Monitoring ----------

        public static void StartMonitoring()
        {
            lock (_monitorLock)
            {
                if (_isMonitoring) return;

                StartClassicMonitoringLocked();
                _isMonitoring = true;
                Interlocked.Exchange(ref _classicPresenceRefreshFailureCount, 0);
                StartClassicMonitorHeartbeatLocked();
                ScheduleClassicPresenceRefresh(force: true);
            }
        }

        public static void StopMonitoring()
        {
            lock (_monitorLock)
            {
                if (!_isMonitoring) return;

                StopClassicMonitoringLocked();
                StopClassicMonitorHeartbeatLocked();

                _monitorLastSeen.Clear();
                _monitorLastRssi.Clear();
                _classicMonitorIdToAddress.Clear();
                _classicMonitorPresent.Clear();
                _classicMonitorConsecutiveMisses.Clear();
                Interlocked.Exchange(ref _classicPresenceRefreshInProgress, 0);
                Interlocked.Exchange(ref _classicPresenceRefreshFailureCount, 0);
                Interlocked.Exchange(ref _lastClassicPresenceRefreshTick, -ClassicPresenceRefreshIntervalMs);
                _isMonitoring = false;
            }
        }

        public static bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Registers a device address for active monitoring. The device is
        /// assumed present until proven otherwise (miss tolerance + active probe).
        /// Call this for every Bluetooth trigger action address after StartMonitoring.
        /// </summary>
        public static void RegisterMonitoredDevice(ulong bluetoothAddress)
        {
            if (bluetoothAddress == 0 || !_isMonitoring) return;
            _classicMonitorPresent[bluetoothAddress] = 1;
            _classicMonitorConsecutiveMisses.TryRemove(bluetoothAddress, out _);
            _monitorLastSeen[bluetoothAddress] = DateTime.Now;
        }

        public static bool IsDeviceReachable(string macAddress, int thresholdSeconds)
        {
            ulong address = MacStringToUlong(macAddress);
            if (address == 0) return false;
            return IsDeviceReachable(address, thresholdSeconds, DateTime.Now);
        }

        public static bool IsDeviceReachable(ulong bluetoothAddress, int thresholdSeconds, DateTime now)
        {
            if (bluetoothAddress == 0) return false;
            if (!_monitorLastSeen.TryGetValue(bluetoothAddress, out DateTime lastSeen))
            {
                return false;
            }

            return (now - lastSeen).TotalSeconds < thresholdSeconds;
        }

        public static bool HasDeviceEverBeenSeen(string macAddress)
        {
            ulong address = MacStringToUlong(macAddress);
            return HasDeviceEverBeenSeen(address);
        }

        public static bool HasDeviceEverBeenSeen(ulong bluetoothAddress)
        {
            return bluetoothAddress != 0 && _monitorLastSeen.ContainsKey(bluetoothAddress);
        }

        public static short GetDeviceRssi(ulong bluetoothAddress)
        {
            if (bluetoothAddress == 0) return 0;
            return _monitorLastRssi.TryGetValue(bluetoothAddress, out short rssi) ? rssi : (short)0;
        }

        public static bool IsDeviceReachable(ulong bluetoothAddress, int thresholdSeconds, int rssiMinDbm, DateTime now)
        {
            if (bluetoothAddress == 0) return false;
            if (!_monitorLastSeen.TryGetValue(bluetoothAddress, out DateTime lastSeen))
            {
                return false;
            }

            if ((now - lastSeen).TotalSeconds >= thresholdSeconds)
            {
                return false;
            }

            if (rssiMinDbm < 0 &&
                _monitorLastRssi.TryGetValue(bluetoothAddress, out short rssi) &&
                rssi < 0 &&
                rssi < rssiMinDbm)
            {
                return false;
            }

            return true;
        }

        public static void SeedMonitoredDeviceFromDiscovery(string macAddress, int maxDiscoveryAgeSeconds = 15)
        {
            ulong address = MacStringToUlong(macAddress);
            if (address == 0) return;
            if (maxDiscoveryAgeSeconds <= 0) maxDiscoveryAgeSeconds = 15;

            if (!_discoveredDevices.TryGetValue(address, out BleDeviceInfo discovered) || discovered == null)
            {
                return;
            }

            if ((DateTime.Now - discovered.LastSeen).TotalSeconds > maxDiscoveryAgeSeconds)
            {
                return;
            }

            _monitorLastSeen[address] = DateTime.Now;
        }

        private static void StartClassicMonitoringLocked()
        {
            try
            {
                if (_classicMonitorWatcher != null)
                {
                    return;
                }

                string selector = "System.Devices.Aep.ProtocolId:=\"" + ClassicBtProtocolId + "\"";
                var requestedProps = new string[]
                {
                    "System.Devices.Aep.DeviceAddress",
                    "System.Devices.Aep.IsPresent",
                    "System.Devices.Aep.SignalStrength"
                };

                _classicMonitorWatcher = DeviceInformation.CreateWatcher(
                    selector, requestedProps, DeviceInformationKind.AssociationEndpoint);

                _classicMonitorWatcher.Added += OnClassicMonitorAdded;
                _classicMonitorWatcher.Updated += OnClassicMonitorUpdated;
                _classicMonitorWatcher.Removed += OnClassicMonitorRemoved;
                _classicMonitorWatcher.Start();
            }
            catch
            {
                _classicMonitorWatcher = null;
            }
        }

        private static void StopClassicMonitoringLocked()
        {
            if (_classicMonitorWatcher == null)
            {
                return;
            }

            try
            {
                _classicMonitorWatcher.Added -= OnClassicMonitorAdded;
                _classicMonitorWatcher.Updated -= OnClassicMonitorUpdated;
                _classicMonitorWatcher.Removed -= OnClassicMonitorRemoved;
                _classicMonitorWatcher.Stop();
            }
            catch
            {
            }

            _classicMonitorWatcher = null;
        }

        private static void OnClassicMonitorAdded(DeviceWatcher sender, DeviceInformation info)
        {
            UpdateClassicMonitorPresence(info.Id, info.Properties, isPresentFallback: null);
        }

        private static void OnClassicMonitorUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            UpdateClassicMonitorPresence(update.Id, update.Properties, isPresentFallback: null);
        }

        private static void OnClassicMonitorRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            // Do not immediately remove from _classicMonitorPresent.
            // The periodic RefreshClassicPresenceSnapshotAsync will handle
            // removal with miss tolerance to avoid false triggers.
        }

        private static void UpdateClassicMonitorPresence(
            string deviceId,
            IReadOnlyDictionary<string, object> properties,
            bool? isPresentFallback)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return;
            }

            if (!_classicMonitorIdToAddress.TryGetValue(deviceId, out ulong address) || address == 0)
            {
                if (properties != null &&
                    properties.TryGetValue("System.Devices.Aep.DeviceAddress", out object addrObj))
                {
                    address = MacStringToUlong(addrObj?.ToString() ?? string.Empty);
                    if (address != 0)
                    {
                        _classicMonitorIdToAddress[deviceId] = address;
                    }
                }
            }

            if (address == 0)
            {
                return;
            }

            bool hasPresence = false;
            bool isPresent = false;
            if (TryGetBoolProperty(properties, "System.Devices.Aep.IsPresent", out bool parsedPresence))
            {
                isPresent = parsedPresence;
                hasPresence = true;
            }

            if (!hasPresence)
            {
                if (isPresentFallback.HasValue)
                {
                    isPresent = isPresentFallback.Value;
                }
                else
                {
                    return;
                }
            }

            if (isPresent)
            {
                _classicMonitorPresent[address] = 1;
                _classicMonitorConsecutiveMisses.TryRemove(address, out _);
                _monitorLastSeen[address] = DateTime.Now;

                if (properties != null &&
                    properties.TryGetValue("System.Devices.Aep.SignalStrength", out object sigObj) &&
                    sigObj != null)
                {
                    if (sigObj is int intRssi)
                    {
                        _monitorLastRssi[address] = (short)intRssi;
                    }
                    else if (int.TryParse(sigObj.ToString(), out int parsedRssi))
                    {
                        _monitorLastRssi[address] = (short)parsedRssi;
                    }
                }
            }
            // When isPresent is false, do NOT immediately remove from
            // _classicMonitorPresent. The snapshot refresh handles removal
            // with miss tolerance to prevent false triggers.
        }

        private static void StartClassicMonitorHeartbeatLocked()
        {
            if (_classicMonitorHeartbeatTimer != null)
            {
                return;
            }

            _classicMonitorHeartbeatTimer = new Timer(_ =>
            {
                if (!_isMonitoring)
                {
                    return;
                }

                DateTime now = DateTime.Now;
                foreach (ulong address in _classicMonitorPresent.Keys)
                {
                    _monitorLastSeen[address] = now;
                }

                ScheduleClassicPresenceRefresh(force: false);
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private static void StopClassicMonitorHeartbeatLocked()
        {
            if (_classicMonitorHeartbeatTimer == null)
            {
                return;
            }

            try
            {
                _classicMonitorHeartbeatTimer.Dispose();
            }
            catch
            {
            }

            _classicMonitorHeartbeatTimer = null;
        }

        private static void ScheduleClassicPresenceRefresh(bool force)
        {
            if (!_isMonitoring || _classicMonitorWatcher == null)
            {
                return;
            }

            long nowTick = Environment.TickCount64;
            long lastTick = Interlocked.Read(ref _lastClassicPresenceRefreshTick);
            if (!force && nowTick - lastTick < ClassicPresenceRefreshIntervalMs)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _classicPresenceRefreshInProgress, 1, 0) != 0)
            {
                return;
            }

            Interlocked.Exchange(ref _lastClassicPresenceRefreshTick, nowTick);

            _ = Task.Run(async () =>
            {
                try
                {
                    bool refreshed = await RefreshClassicPresenceSnapshotAsync();
                    if (refreshed)
                    {
                        Interlocked.Exchange(ref _classicPresenceRefreshFailureCount, 0);
                    }
                    else
                    {
                        int failures = Interlocked.Increment(ref _classicPresenceRefreshFailureCount);
                        if (failures >= ClassicPresenceRefreshFailureTolerance)
                        {
                            _classicMonitorPresent.Clear();
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _classicPresenceRefreshInProgress, 0);
                }
            });
        }

        private static async Task<bool> RefreshClassicPresenceSnapshotAsync()
        {
            try
            {
                var requestedProps = new string[]
                {
                    "System.Devices.Aep.DeviceAddress",
                    "System.Devices.Aep.IsPresent",
                    "System.Devices.Aep.SignalStrength"
                };

                DateTime now = DateTime.Now;
                var presentAddresses = new HashSet<ulong>();

                string selector = "System.Devices.Aep.ProtocolId:=\"" + ClassicBtProtocolId + "\"";

                DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(
                    selector,
                    requestedProps,
                    DeviceInformationKind.AssociationEndpoint);

                if (!_isMonitoring)
                {
                    return false;
                }

                foreach (DeviceInformation device in devices)
                {
                    if (!TryGetAddressFromProperties(device.Properties, out ulong address))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(device.Id))
                    {
                        _classicMonitorIdToAddress[device.Id] = address;
                    }

                    if (TryGetBoolProperty(device.Properties, "System.Devices.Aep.IsPresent", out bool isPresent) &&
                        isPresent)
                    {
                        presentAddresses.Add(address);
                        _classicMonitorPresent[address] = 1;
                        _classicMonitorConsecutiveMisses.TryRemove(address, out _);
                        _monitorLastSeen[address] = now;

                        if (device.Properties.TryGetValue("System.Devices.Aep.SignalStrength", out object sigObj) &&
                            sigObj != null)
                        {
                            if (sigObj is int intRssi)
                            {
                                _monitorLastRssi[address] = (short)intRssi;
                            }
                            else if (int.TryParse(sigObj.ToString(), out int parsedRssi))
                            {
                                _monitorLastRssi[address] = (short)parsedRssi;
                            }
                        }
                    }
                }

                foreach (ulong address in _classicMonitorPresent.Keys)
                {
                    if (!presentAddresses.Contains(address))
                    {
                        int misses = _classicMonitorConsecutiveMisses.AddOrUpdate(
                            address, 1, (_, existing) => existing + 1);

                        if (misses >= ClassicDeviceMissTolerance)
                        {
                            // Active probe: try direct SDP query before declaring device gone
                            bool stillReachable = await ProbeDeviceActivelyAsync(address);
                            if (stillReachable)
                            {
                                _classicMonitorConsecutiveMisses.TryRemove(address, out _);
                                _monitorLastSeen[address] = now;
                            }
                            else
                            {
                                _classicMonitorPresent.TryRemove(address, out _);
                                _classicMonitorConsecutiveMisses.TryRemove(address, out _);
                            }
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> ProbeDeviceActivelyAsync(ulong bluetoothAddress)
        {
            try
            {
                var connectTask = BluetoothDevice.FromBluetoothAddressAsync(bluetoothAddress).AsTask();
                if (await Task.WhenAny(connectTask, Task.Delay(3000)) != connectTask)
                {
                    return false;
                }

                using var device = connectTask.Result;
                if (device == null)
                {
                    return false;
                }

                var servicesTask = device.GetRfcommServicesAsync(BluetoothCacheMode.Uncached).AsTask();
                if (await Task.WhenAny(servicesTask, Task.Delay(3000)) != servicesTask)
                {
                    return false;
                }

                var result = servicesTask.Result;
                return result?.Error == BluetoothError.Success;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetAddressFromProperties(
            IReadOnlyDictionary<string, object> properties,
            out ulong address)
        {
            address = 0;
            if (properties == null ||
                !properties.TryGetValue("System.Devices.Aep.DeviceAddress", out object addrObj))
            {
                return false;
            }

            address = MacStringToUlong(addrObj?.ToString() ?? string.Empty);
            return address != 0;
        }

        private static bool TryGetBoolProperty(
            IReadOnlyDictionary<string, object> properties,
            string key,
            out bool value)
        {
            value = false;
            if (properties == null ||
                string.IsNullOrWhiteSpace(key) ||
                !properties.TryGetValue(key, out object rawValue) ||
                rawValue == null)
            {
                return false;
            }

            if (rawValue is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            return bool.TryParse(rawValue.ToString(), out value);
        }

        // ---------- Helpers ----------

        public static string FormatMacAddress(ulong bluetoothAddress)
        {
            var bytes = BitConverter.GetBytes(bluetoothAddress);
            return $"{bytes[5]:X2}:{bytes[4]:X2}:{bytes[3]:X2}:{bytes[2]:X2}:{bytes[1]:X2}:{bytes[0]:X2}";
        }

        public static ulong MacStringToUlong(string mac)
        {
            if (string.IsNullOrWhiteSpace(mac)) return 0;

            try
            {
                string hex = mac.Replace(":", "").Replace("-", "");
                return Convert.ToUInt64(hex, 16);
            }
            catch
            {
                return 0;
            }
        }
    }
}
