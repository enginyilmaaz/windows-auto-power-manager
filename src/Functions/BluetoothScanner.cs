using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InTheHand.Net.Sockets;
using Windows.Devices.Bluetooth.Advertisement;

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
        // ---------- Win32 Bluetooth P/Invoke ----------

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort wYear, wMonth, wDayOfWeek, wDay;
            public ushort wHour, wMinute, wSecond, wMilliseconds;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BLUETOOTH_DEVICE_INFO
        {
            public uint dwSize;
            public ulong Address;
            public uint ulClassofDevice;
            [MarshalAs(UnmanagedType.Bool)] public bool fConnected;
            [MarshalAs(UnmanagedType.Bool)] public bool fRemembered;
            [MarshalAs(UnmanagedType.Bool)] public bool fAuthenticated;
            public SYSTEMTIME stLastSeen;
            public SYSTEMTIME stLastUsed;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)] public string szName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            public uint dwSize;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnAuthenticated;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnRemembered;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnUnknown;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnConnected;
            [MarshalAs(UnmanagedType.Bool)] public bool fIssueInquiry;
            public byte cTimeoutMultiplier;
            public IntPtr hRadio;
        }

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        private static extern IntPtr BluetoothFindFirstDevice(
            ref BLUETOOTH_DEVICE_SEARCH_PARAMS searchParams,
            ref BLUETOOTH_DEVICE_INFO deviceInfo);

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BluetoothFindNextDevice(
            IntPtr hFind,
            ref BLUETOOTH_DEVICE_INFO deviceInfo);

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BluetoothFindDeviceClose(IntPtr hFind);

        // ---------- State ----------

        private static readonly ConcurrentDictionary<ulong, BleDeviceInfo> _discoveredDevices = new();
        private static readonly ConcurrentDictionary<ulong, DateTime> _monitorLastSeen = new();
        private static readonly ConcurrentDictionary<ulong, short> _monitorLastRssi = new();
        private static readonly ConcurrentDictionary<ulong, byte> _monitorPresent = new();
        private static readonly ConcurrentDictionary<ulong, int> _monitorConsecutiveMisses = new();

        private static readonly object _discoveryLock = new();
        private static readonly object _monitorLock = new();
        private static readonly object _bleLock = new();

        private static CancellationTokenSource _discoveryCts;
        private static Timer _monitorTimer;
        private static BluetoothLEAdvertisementWatcher _bleWatcher;

        private static bool _isDiscovering;
        private static bool _isMonitoring;
        private static bool _isBleWatcherRunning;
        private static int _discoveryVersion;

        public static event Action<BleDeviceInfo> DeviceDiscovered;

        private const int DiscoveryInquiryTimeoutMultiplier = 4; // ~5 seconds per scan
        private const int MonitorInquiryTimeoutMultiplier = 3;   // ~4 seconds per scan
        private const int MonitorIntervalMs = 8000;
        private const int MonitorMissTolerance = 3;
        private const int BleFreshSeenToleranceSeconds = 12;

        // ---------- Discovery Scan (UI device list) ----------

        public static void StartDiscoveryScan()
        {
            lock (_discoveryLock)
            {
                if (_isDiscovering) return;

                _discoveredDevices.Clear();
                Interlocked.Increment(ref _discoveryVersion);
                _isDiscovering = true;

                _discoveryCts = new CancellationTokenSource();
                var token = _discoveryCts.Token;

                EnsureBleWatcherRunning();
                Task.Run(() => DiscoveryLoop(token), token);
            }
        }

        public static void StopDiscoveryScan()
        {
            lock (_discoveryLock)
            {
                if (!_isDiscovering) return;

                _discoveryCts?.Cancel();
                _discoveryCts?.Dispose();
                _discoveryCts = null;
                _isDiscovering = false;
                StopBleWatcherIfUnused();
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

        private static void DiscoveryLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var devices = Perform32FeetClassicScan();

                    var inquiredDevices = PerformInquiryScan(
                        issueInquiry: true,
                        returnAuthenticated: true,
                        returnRemembered: false,
                        returnUnknown: true,
                        returnConnected: true,
                        timeoutMultiplier: DiscoveryInquiryTimeoutMultiplier);
                    devices.AddRange(inquiredDevices);

                    // Also include paired/remembered classic devices so phones
                    // that are not currently discoverable still show in picker.
                    var rememberedDevices = PerformInquiryScan(
                        issueInquiry: false,
                        returnAuthenticated: true,
                        returnRemembered: true,
                        returnUnknown: false,
                        returnConnected: true,
                        timeoutMultiplier: 1);

                    if (rememberedDevices.Count > 0)
                    {
                        devices.AddRange(rememberedDevices);
                    }

                    if (token.IsCancellationRequested) break;

                    bool changed = false;
                    foreach (var dev in devices)
                    {
                        if (_discoveredDevices.TryGetValue(dev.BluetoothAddress, out var existing))
                        {
                            bool classicNameIsFallbackMac =
                                string.Equals(dev.LocalName, dev.MacAddress, StringComparison.OrdinalIgnoreCase);

                            if (!classicNameIsFallbackMac &&
                                !string.Equals(existing.LocalName, dev.LocalName, StringComparison.Ordinal))
                            {
                                changed = true;
                            }

                            if (!classicNameIsFallbackMac || string.IsNullOrWhiteSpace(existing.LocalName))
                            {
                                existing.LocalName = dev.LocalName;
                            }
                            existing.LastSeen = dev.LastSeen;
                        }
                        else
                        {
                            _discoveredDevices[dev.BluetoothAddress] = dev;
                            changed = true;
                        }

                        DeviceDiscovered?.Invoke(dev);
                    }

                    if (changed)
                    {
                        Interlocked.Increment(ref _discoveryVersion);
                    }
                }
                catch
                {
                    // Scan failed, retry after a short delay.
                }

                // Brief pause between scans.
                if (!token.IsCancellationRequested)
                {
                    try { Task.Delay(1000, token).Wait(token); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }

        // ---------- Background Monitoring ----------

        public static void StartMonitoring()
        {
            lock (_monitorLock)
            {
                if (_isMonitoring) return;
                _isMonitoring = true;

                EnsureBleWatcherRunning();
                _monitorTimer = new Timer(_ => MonitorTick(), null,
                    TimeSpan.Zero, TimeSpan.FromMilliseconds(MonitorIntervalMs));
            }
        }

        public static void StopMonitoring()
        {
            lock (_monitorLock)
            {
                if (!_isMonitoring) return;

                try { _monitorTimer?.Dispose(); } catch { }
                _monitorTimer = null;

                _monitorLastSeen.Clear();
                _monitorLastRssi.Clear();
                _monitorPresent.Clear();
                _monitorConsecutiveMisses.Clear();
                _isMonitoring = false;
                StopBleWatcherIfUnused();
            }
        }

        public static bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Registers a device address for active monitoring. The device is
        /// assumed present until proven otherwise (miss tolerance).
        /// </summary>
        public static void RegisterMonitoredDevice(ulong bluetoothAddress)
        {
            if (bluetoothAddress == 0 || !_isMonitoring) return;
            _monitorPresent[bluetoothAddress] = 1;
            _monitorConsecutiveMisses.TryRemove(bluetoothAddress, out _);
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

        private static void MonitorTick()
        {
            if (!_isMonitoring) return;

            try
            {
                var foundDevices = Perform32FeetClassicScan();

                var inquiredDevices = PerformInquiryScan(
                    issueInquiry: true,
                    returnAuthenticated: true,
                    returnRemembered: false,
                    returnUnknown: true,
                    returnConnected: true,
                    timeoutMultiplier: MonitorInquiryTimeoutMultiplier);
                foundDevices.AddRange(inquiredDevices);

                if (!_isMonitoring) return;

                DateTime now = DateTime.Now;
                var foundAddresses = new HashSet<ulong>();
                foreach (var dev in foundDevices)
                {
                    foundAddresses.Add(dev.BluetoothAddress);
                }

                // Mark found devices as present.
                foreach (ulong address in foundAddresses)
                {
                    _monitorPresent[address] = 1;
                    _monitorConsecutiveMisses.TryRemove(address, out _);
                    _monitorLastSeen[address] = now;
                }

                foreach (ulong address in _monitorPresent.Keys)
                {
                    if (_monitorLastSeen.TryGetValue(address, out DateTime lastSeen) &&
                        (now - lastSeen).TotalSeconds <= BleFreshSeenToleranceSeconds)
                    {
                        foundAddresses.Add(address);
                    }
                }

                // For registered devices NOT found in this scan, increment miss count.
                foreach (ulong address in _monitorPresent.Keys)
                {
                    if (!foundAddresses.Contains(address))
                    {
                        int misses = _monitorConsecutiveMisses.AddOrUpdate(
                            address, 1, (_, existing) => existing + 1);

                        if (misses >= MonitorMissTolerance)
                        {
                            _monitorPresent.TryRemove(address, out _);
                            _monitorConsecutiveMisses.TryRemove(address, out _);
                        }
                        else
                        {
                            // Still within tolerance, keep last seen alive.
                            _monitorLastSeen[address] = now;
                        }
                    }
                }
            }
            catch
            {
                // Scan failed, keep current state unchanged.
            }
        }

        // ---------- Win32 Inquiry Scan ----------

        private static List<BleDeviceInfo> PerformInquiryScan(
            bool issueInquiry,
            bool returnAuthenticated,
            bool returnRemembered,
            bool returnUnknown,
            bool returnConnected,
            byte timeoutMultiplier)
        {
            var results = new List<BleDeviceInfo>();

            var searchParams = new BLUETOOTH_DEVICE_SEARCH_PARAMS
            {
                dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_SEARCH_PARAMS>(),
                fReturnAuthenticated = returnAuthenticated,
                fReturnRemembered = returnRemembered,
                fReturnUnknown = returnUnknown,
                fReturnConnected = returnConnected,
                fIssueInquiry = issueInquiry,
                cTimeoutMultiplier = timeoutMultiplier,
                hRadio = IntPtr.Zero
            };

            var deviceInfo = new BLUETOOTH_DEVICE_INFO
            {
                dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>()
            };

            IntPtr hFind = BluetoothFindFirstDevice(ref searchParams, ref deviceInfo);
            if (hFind == IntPtr.Zero)
            {
                return results;
            }

            try
            {
                do
                {
                    if (deviceInfo.Address == 0) continue;

                    string name = deviceInfo.szName;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = FormatMacAddress(deviceInfo.Address);
                    }

                    results.Add(new BleDeviceInfo
                    {
                        BluetoothAddress = deviceInfo.Address,
                        MacAddress = FormatMacAddress(deviceInfo.Address),
                        LocalName = name,
                        RssiDbm = 0,
                        LastSeen = DateTime.Now
                    });

                    deviceInfo = new BLUETOOTH_DEVICE_INFO
                    {
                        dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>()
                    };

                } while (BluetoothFindNextDevice(hFind, ref deviceInfo));
            }
            finally
            {
                BluetoothFindDeviceClose(hFind);
            }

            return results;
        }

        private static List<BleDeviceInfo> Perform32FeetClassicScan()
        {
            var results = new List<BleDeviceInfo>();

            try
            {
                using var client = new BluetoothClient();
                var devices = client.DiscoverDevices(255);
                DateTime now = DateTime.Now;

                foreach (var device in devices)
                {
                    if (device == null) continue;

                    ulong address = ParseBluetoothAddressFromText(device.DeviceAddress?.ToString());
                    if (address == 0) continue;

                    string name = device.DeviceName;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = FormatMacAddress(address);
                    }

                    results.Add(new BleDeviceInfo
                    {
                        BluetoothAddress = address,
                        MacAddress = FormatMacAddress(address),
                        LocalName = name,
                        RssiDbm = 0,
                        LastSeen = now
                    });
                }
            }
            catch
            {
                // 32feet discovery is optional; keep other providers active.
            }

            return results;
        }

        private static void EnsureBleWatcherRunning()
        {
            lock (_bleLock)
            {
                if (_isBleWatcherRunning)
                {
                    return;
                }

                try
                {
                    _bleWatcher ??= new BluetoothLEAdvertisementWatcher
                    {
                        ScanningMode = BluetoothLEScanningMode.Active
                    };

                    _bleWatcher.Received -= BleWatcherOnReceived;
                    _bleWatcher.Received += BleWatcherOnReceived;
                    _bleWatcher.Start();
                    _isBleWatcherRunning = true;
                }
                catch
                {
                    _isBleWatcherRunning = false;
                }
            }
        }

        private static void StopBleWatcherIfUnused()
        {
            lock (_bleLock)
            {
                if (_isDiscovering || _isMonitoring)
                {
                    return;
                }

                if (_bleWatcher == null)
                {
                    _isBleWatcherRunning = false;
                    return;
                }

                try
                {
                    _bleWatcher.Received -= BleWatcherOnReceived;
                    if (_bleWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started ||
                        _bleWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Created ||
                        _bleWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Stopping)
                    {
                        _bleWatcher.Stop();
                    }
                }
                catch
                {
                    // Ignore shutdown errors.
                }

                _isBleWatcherRunning = false;
            }
        }

        private static void BleWatcherOnReceived(
            BluetoothLEAdvertisementWatcher sender,
            BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                if (args == null || args.BluetoothAddress == 0)
                {
                    return;
                }

                ulong address = args.BluetoothAddress;
                string localName = args.Advertisement?.LocalName;
                short rssi = (short)args.RawSignalStrengthInDBm;
                DateTime now = DateTime.Now;

                string fallbackMac = FormatMacAddress(address);
                if (string.IsNullOrWhiteSpace(localName))
                {
                    localName = fallbackMac;
                }

                bool changed = false;
                if (_discoveredDevices.TryGetValue(address, out BleDeviceInfo existing) &&
                    existing != null)
                {
                    if (!string.Equals(existing.LocalName, localName, StringComparison.Ordinal) ||
                        existing.RssiDbm != rssi)
                    {
                        changed = true;
                    }

                    existing.LocalName = localName;
                    existing.RssiDbm = rssi;
                    existing.LastSeen = now;
                }
                else
                {
                    var device = new BleDeviceInfo
                    {
                        BluetoothAddress = address,
                        MacAddress = fallbackMac,
                        LocalName = localName,
                        RssiDbm = rssi,
                        LastSeen = now
                    };

                    _discoveredDevices[address] = device;
                    changed = true;
                    existing = device;
                }

                _monitorPresent[address] = 1;
                _monitorConsecutiveMisses.TryRemove(address, out _);
                _monitorLastSeen[address] = now;
                _monitorLastRssi[address] = rssi;

                if (changed)
                {
                    Interlocked.Increment(ref _discoveryVersion);
                }

                DeviceDiscovered?.Invoke(existing);
            }
            catch
            {
                // Ignore BLE event processing failures.
            }
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

        private static ulong ParseBluetoothAddressFromText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return 0;
            }

            var sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (Uri.IsHexDigit(c))
                {
                    sb.Append(c);
                }
            }

            string hex = sb.ToString();
            if (hex.Length == 0)
            {
                return 0;
            }

            if (hex.Length > 12)
            {
                hex = hex.Substring(hex.Length - 12);
            }

            try
            {
                return Convert.ToUInt64(hex, 16);
            }
            catch
            {
                return 0;
            }
        }
    }
}
