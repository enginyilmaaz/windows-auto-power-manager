using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
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
        private static BluetoothLEAdvertisementWatcher _discoveryWatcher;
        private static BluetoothLEAdvertisementWatcher _monitorWatcher;
        private static DeviceWatcher _classicBtWatcher;
        private static DeviceWatcher _classicMonitorWatcher;
        private static Timer _classicMonitorHeartbeatTimer;

        private static readonly ConcurrentDictionary<ulong, BleDeviceInfo> _discoveredDevices = new();
        private static readonly ConcurrentDictionary<ulong, DateTime> _monitorLastSeen = new();
        private static readonly ConcurrentDictionary<string, ulong> _classicMonitorIdToAddress = new();
        private static readonly ConcurrentDictionary<ulong, byte> _classicMonitorPresent = new();
        private static int _classicPresenceRefreshInProgress;
        private static long _lastClassicPresenceRefreshTick = -ClassicPresenceRefreshIntervalMs;

        private static readonly object _discoveryLock = new();
        private static readonly object _monitorLock = new();

        private static bool _isDiscovering;
        private static bool _isMonitoring;
        private static int _discoveryVersion;

        public static event Action<BleDeviceInfo> DeviceDiscovered;

        // Bluetooth Classic protocol ID for DeviceWatcher
        private const string ClassicBtProtocolId = "{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}";
        private const int ClassicPresenceRefreshIntervalMs = 5000;

        // ---------- Discovery Scan (UI device list) ----------

        public static void StartDiscoveryScan()
        {
            lock (_discoveryLock)
            {
                if (_isDiscovering) return;

                _discoveredDevices.Clear();
                Interlocked.Increment(ref _discoveryVersion);

                // BLE advertisement watcher
                _discoveryWatcher = new BluetoothLEAdvertisementWatcher
                {
                    ScanningMode = BluetoothLEScanningMode.Active
                };

                _discoveryWatcher.Received += OnDiscoveryReceived;
                _discoveryWatcher.Start();

                // Classic Bluetooth device watcher
                try
                {
                    string selector = "System.Devices.Aep.ProtocolId:=\"" + ClassicBtProtocolId + "\"";
                    var requestedProps = new string[] { "System.Devices.Aep.DeviceAddress" };
                    _classicBtWatcher = DeviceInformation.CreateWatcher(
                        selector, requestedProps, DeviceInformationKind.AssociationEndpoint);

                    _classicBtWatcher.Added += OnClassicDeviceAdded;
                    _classicBtWatcher.Updated += OnClassicDeviceUpdated;
                    _classicBtWatcher.Start();
                }
                catch
                {
                    // Classic BT not available, continue with BLE only
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

                if (_discoveryWatcher != null)
                {
                    _discoveryWatcher.Received -= OnDiscoveryReceived;
                    _discoveryWatcher.Stop();
                    _discoveryWatcher = null;
                }

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

        private static void OnDiscoveryReceived(
            BluetoothLEAdvertisementWatcher sender,
            BluetoothLEAdvertisementReceivedEventArgs args)
        {
            string name = args.Advertisement.LocalName;

            var info = new BleDeviceInfo
            {
                BluetoothAddress = args.BluetoothAddress,
                MacAddress = FormatMacAddress(args.BluetoothAddress),
                LocalName = name,
                RssiDbm = args.RawSignalStrengthInDBm,
                LastSeen = DateTime.Now
            };

            bool changed = false;
            _discoveredDevices.AddOrUpdate(
                args.BluetoothAddress,
                _ =>
                {
                    changed = true;
                    return info;
                },
                (key, existing) =>
                {
                    if (existing.RssiDbm != info.RssiDbm)
                    {
                        existing.RssiDbm = info.RssiDbm;
                        changed = true;
                    }

                    existing.LastSeen = info.LastSeen;
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (!string.Equals(existing.LocalName, name, StringComparison.Ordinal))
                        {
                            changed = true;
                        }

                        existing.LocalName = name;
                    }
                    return existing;
                });

            if (changed)
            {
                Interlocked.Increment(ref _discoveryVersion);
            }

            if (!string.IsNullOrEmpty(name))
            {
                DeviceDiscovered?.Invoke(info);
            }
        }

        private static void OnClassicDeviceAdded(DeviceWatcher sender, DeviceInformation info)
        {
            AddClassicDevice(info);
        }

        private static void OnClassicDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            // Update is mainly for connection state changes, ignore for discovery
        }

        private static void AddClassicDevice(DeviceInformation info)
        {
            string name = info.Name;
            if (string.IsNullOrEmpty(name)) return;

            string deviceAddress = "";
            if (info.Properties.TryGetValue("System.Devices.Aep.DeviceAddress", out object addr))
            {
                deviceAddress = addr?.ToString() ?? "";
            }

            if (string.IsNullOrEmpty(deviceAddress)) return;

            ulong macLong = MacStringToUlong(deviceAddress);
            if (macLong == 0) return;

            var deviceInfo = new BleDeviceInfo
            {
                BluetoothAddress = macLong,
                MacAddress = FormatMacAddress(macLong),
                LocalName = name,
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
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (!string.Equals(existing.LocalName, name, StringComparison.Ordinal))
                        {
                            changed = true;
                        }

                        existing.LocalName = name;
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

        // ---------- Background Monitoring ----------

        public static void StartMonitoring()
        {
            lock (_monitorLock)
            {
                if (_isMonitoring) return;

                _monitorWatcher = new BluetoothLEAdvertisementWatcher
                {
                    ScanningMode = BluetoothLEScanningMode.Passive
                };

                _monitorWatcher.Received += OnMonitorReceived;
                _monitorWatcher.Start();

                StartClassicMonitoringLocked();
                _isMonitoring = true;
                StartClassicMonitorHeartbeatLocked();
                ScheduleClassicPresenceRefresh(force: true);
            }
        }

        public static void StopMonitoring()
        {
            lock (_monitorLock)
            {
                if (!_isMonitoring) return;

                if (_monitorWatcher != null)
                {
                    _monitorWatcher.Received -= OnMonitorReceived;
                    _monitorWatcher.Stop();
                    _monitorWatcher = null;
                }

                StopClassicMonitoringLocked();
                StopClassicMonitorHeartbeatLocked();

                _monitorLastSeen.Clear();
                _classicMonitorIdToAddress.Clear();
                _classicMonitorPresent.Clear();
                Interlocked.Exchange(ref _classicPresenceRefreshInProgress, 0);
                Interlocked.Exchange(ref _lastClassicPresenceRefreshTick, -ClassicPresenceRefreshIntervalMs);
                _isMonitoring = false;
            }
        }

        public static bool IsMonitoring => _isMonitoring;

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

        private static void OnMonitorReceived(
            BluetoothLEAdvertisementWatcher sender,
            BluetoothLEAdvertisementReceivedEventArgs args)
        {
            _monitorLastSeen[args.BluetoothAddress] = DateTime.Now;
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
                    "System.Devices.Aep.IsPresent"
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
            if (update == null || string.IsNullOrWhiteSpace(update.Id))
            {
                return;
            }

            if (_classicMonitorIdToAddress.TryGetValue(update.Id, out ulong address) && address != 0)
            {
                _classicMonitorPresent.TryRemove(address, out _);
            }
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
                _monitorLastSeen[address] = DateTime.Now;
            }
            else
            {
                _classicMonitorPresent.TryRemove(address, out _);
            }
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
                    await RefreshClassicPresenceSnapshotAsync();
                }
                catch
                {
                }
                finally
                {
                    Interlocked.Exchange(ref _classicPresenceRefreshInProgress, 0);
                }
            });
        }

        private static async Task RefreshClassicPresenceSnapshotAsync()
        {
            try
            {
                string selector = "System.Devices.Aep.ProtocolId:=\"" + ClassicBtProtocolId + "\"";
                var requestedProps = new string[]
                {
                    "System.Devices.Aep.DeviceAddress",
                    "System.Devices.Aep.IsPresent"
                };

                DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(
                    selector,
                    requestedProps,
                    DeviceInformationKind.AssociationEndpoint);

                if (!_isMonitoring)
                {
                    return;
                }

                DateTime now = DateTime.Now;
                var presentAddresses = new HashSet<ulong>();

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
                        _monitorLastSeen[address] = now;
                    }
                }

                foreach (ulong address in _classicMonitorPresent.Keys)
                {
                    if (!presentAddresses.Contains(address))
                    {
                        _classicMonitorPresent.TryRemove(address, out _);
                    }
                }
            }
            catch
            {
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
