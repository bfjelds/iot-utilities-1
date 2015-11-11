using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using WlanAPIs;

namespace DeviceCenter.Helper
{
    public class DiscoveryHelper : IDisposable
    {
        /// <summary>
        /// Bind the listview to this to show all devices
        /// </summary>
        public ObservableCollection<DiscoveredDevice> AllDevices { get; }

        /// <summary>
        /// Bind the listview to this to show only AllJoyn devices
        /// </summary>
        public ObservableCollection<DiscoveredDevice> NewDevices { get; }

        /// <summary>
        /// Bind the listview to this to show only devices that are connected to the network
        /// and are configurable
        /// </summary>
        public ObservableCollection<DiscoveredDevice> ConfiguredDevices { get; }

        // Broadcast helper classes
        readonly NativeMethods.AddDeviceCallbackDelegate _addDeviceCallbackDel;
        private BroadcastWatcher _broadCastWatcher = new BroadcastWatcher();

        /// <summary>
        /// Timer for scanning new devices
        /// </summary>
        private readonly DispatcherTimer _scanNewDevicesTimer = new DispatcherTimer();

        /// <summary>
        /// Timer for stopping the above scan
        /// </summary>
        private readonly DispatcherTimer _stopScanNewDevicesTimer = new DispatcherTimer();

        /// <summary>
        /// Amount of time to wait before rescanning for new devices
        /// </summary>
        private readonly TimeSpan constStartScanNewDevicesDelay = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Amount of time to wait before stopping the above scanning
        /// </summary>
        private readonly TimeSpan constStopScanNewDevicesDelay = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Amount of time a device can be remembered before we can cut it 
        /// </summary>
        private readonly TimeSpan constMaxAgeDevice = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Cache lookup for AllJoyn discovered devices.  This helps locate the device instance by name
        /// </summary>
        private readonly ConcurrentDictionary<string, DiscoveredDevice> _adhocNetworks = new ConcurrentDictionary<string, DiscoveredDevice>();

        /// <summary>
        /// Cache lookup for mDNS and ebootpinger devices.  This helps locate the device instance by name
        /// </summary>
        private readonly ConcurrentDictionary<string, DiscoveredDevice> _foundDevices = new ConcurrentDictionary<string, DiscoveredDevice>();

        /// <summary>
        /// Call this to get an instance of this class.  This is reference counted to maintain
        /// a single instance of this class.  Call Release when done
        /// </summary>
        public static DiscoveryHelper Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DiscoveryHelper();

                Interlocked.Increment(ref _refCount);

                return _instance;
            }
        }

        /// <summary>
        /// Releases an instance of this class, deletes itself if no longer needed
        /// </summary>
        public static void Release()
        {
            if (Interlocked.Decrement(ref _refCount) == 0)
            {
                _instance.Dispose();
                _instance = null;
            }
        }

        private static int _refCount = 0;
        private static DiscoveryHelper _instance;

        private bool _disposedValue = false; // To detect redundant calls

        private DiscoveryHelper()
        {
            AllDevices = new ObservableCollection<DiscoveredDevice>();
            NewDevices = new ObservableCollection<DiscoveredDevice>();
            ConfiguredDevices = new ObservableCollection<DiscoveredDevice>();

            _addDeviceCallbackDel = new NativeMethods.AddDeviceCallbackDelegate(AddDeviceCallback);

            // Initialize delay (but not enable) for completing discovery
            _stopScanNewDevicesTimer.Interval = constStopScanNewDevicesDelay;
            _stopScanNewDevicesTimer.Tick += StopScanNewDevicesTimerTick;
            _broadCastWatcher.OnPing += new BroadcastWatcher.PingHandler(BroadcastWatcher_Ping);

            // Initialize delay (but not enable) for starting discovery
            _scanNewDevicesTimer.Interval = constStartScanNewDevicesDelay;
            _scanNewDevicesTimer.Tick += ScanNewDevicesTimerTick;

            StartDiscovery();
        }

        /// <summary>
        /// Performs cleanup of self
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    StopDiscovery();

                    _scanNewDevicesTimer.Tick -= ScanNewDevicesTimerTick;
                    _stopScanNewDevicesTimer.Tick -= StopScanNewDevicesTimerTick;
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Called to start destruction of this instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Stops the ebootpinter and mDNS discovery process as well as all timers.  Callers
        /// will have to reenable timers if needed
        /// </summary>
        private void StopDiscovery()
        {
            // Stop all timers
            _scanNewDevicesTimer.Stop();
            _stopScanNewDevicesTimer.Stop();

            // Stop all listeners
            _broadCastWatcher.RemoveListeners();
            NativeMethods.StopDiscovery();
        }

        /// <summary>
        /// Starts the ebootpinger and mDNS discovery process for a limited amount of time
        /// </summary>
        private void StartDiscovery()
        {
            // Stop everything first 
            StopDiscovery();

            // Start mDNS based discovery 

            // 1. Register the callback
            NativeMethods.RegisterCallback(_addDeviceCallbackDel);

            // 2. Start device discovery using DNS-SD
            NativeMethods.StartDiscovery();

            // 3. Start broadcast watcher
            _broadCastWatcher.AddListeners();

            // Start timer that will end scanning
            _stopScanNewDevicesTimer.Start();
        }

        /// <summary>
        /// Called externally by the ViewDevicesPage to display AllJoyn network
        /// </summary>
        /// <param name="accessPoints">The AdHoc device instance list, 
        /// 1) a new device instance will be created if the SSID doesn't exist. 
        /// 2) the device instance will be remvoed if it doesn't exist in the list passed in</param>
        public void RefreshAdhocDevices(IList<WlanInterop.WlanAvailableNetwork> accessPoints)
        {
            // get a list of ssid strings from the passed in list
            var newNetworksSsid = new HashSet<string>();
            foreach (var item in accessPoints)
            {
                newNetworksSsid.Add(item.SsidString);
            }

            // compare with the cached adhoc network to find out the ssids to be removed
            var devicesToRemove = new List<string>();
            foreach (var item in _adhocNetworks)
            {
                if (!newNetworksSsid.Contains(item.Key))
                {
                    devicesToRemove.Add(item.Key);
                }
            }

            // add new devices
            foreach (var item in accessPoints)
            {
                AddAdhocDevice(item);
            }

            // remove devices
            foreach (var item in devicesToRemove)
            {
                RemoveAdhocDevice(item);
            }
        }

        /// <summary>
        /// Add a device into internal new device and all device list
        /// </summary>
        /// <param name="accessPoint">The AdHoc device instance, a new device instance will be
        /// created if the SSID doesn't exist.</param>
        private void AddAdhocDevice(WlanInterop.WlanAvailableNetwork accessPoint)
        {
            DiscoveredDevice device = _adhocNetworks.GetOrAdd(accessPoint.SsidString, (key) =>
            {
                System.Diagnostics.Debug.WriteLine("---------- new device {0}", accessPoint.SsidString);
                var newDevice = new DiscoveredDevice(accessPoint)
                {
                    DeviceName = key
                };

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    AllDevices.Add(newDevice);
                    NewDevices.Add(newDevice);
                }));

                return newDevice;
            });
        }

        /// <summary>
        /// Remove a device from internal new device and all device list
        /// </summary>
        /// <param name="ssidToRemove">The ssid to be removed from the list</param>
        private void RemoveAdhocDevice(string ssidToRemove)
        {
            DiscoveredDevice device;
            if (_adhocNetworks.TryRemove(ssidToRemove, out device))
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    if (AllDevices.Contains(device))
                    {
                        AllDevices.Remove(device);
                    }

                    if (NewDevices.Contains(device))
                    {
                        NewDevices.Remove(device);
                    }
                }));
            };
        }

        /// <summary>
        /// Adds a device to the ConfiguredDevices and AllDevices list.  If it already exists,
        /// it's timeout is reset
        /// </summary>
        /// <param name="Name">The device name</param>
        /// <param name="IP">The device's IP address</param>
        /// <param name="Mac">The device's Ethernet address</param>
        private void AddDeviceCallback(string deviceName, string ipV4Address, string txtParameters)
        {
            if (String.IsNullOrEmpty(deviceName) || String.IsNullOrEmpty(ipV4Address))
            {
                return;
            }

            string parsedDeviceName = "";
            string deviceModel = "";
            string osVersion = "";
            Guid deviceGuid = Guid.Empty;
            string arch = "";
            IPAddressSortable ipAddress = IPAddressSortable.Parse(ipV4Address);

            // mDNS Discovered devices are in format "devicename.local". Remove the suffix
            if (deviceName.IndexOf('.') > 0)
            {
                parsedDeviceName = deviceName.Substring(0, deviceName.IndexOf('.'));
            }
            else
            {
                parsedDeviceName = deviceName;
            }

            if (!String.IsNullOrEmpty(txtParameters))
            {
                //The txt parameter are in following format
                // txtParameters = "guid=79F50796-F59B-D97A-A00F-63D798C6C144,model=Virtual,architecture=x86,osversion=10.0.10557,"
                // Split them with ',' and '=' and get the odd values 
                var deviceDetails = txtParameters.Split(',', '=');
                var index = 0;
                while (index < deviceDetails.Length)
                {
                    switch (deviceDetails[index])
                    {
                        case "guid":
                            deviceGuid = new Guid(deviceDetails[index + 1]);
                            break;
                        case "model":
                            deviceModel = deviceDetails[index + 1];
                            break;
                        case "osversion":
                            osVersion = deviceDetails[index + 1];
                            break;
                        case "architecture":
                            arch = deviceDetails[index + 1];
                            break;
                    }
                    index += 2;
                }
            }

            string keyString = ipAddress != null ? ipAddress.ToString() : parsedDeviceName;

            DiscoveredDevice device = _foundDevices.GetOrAdd(keyString, (key) =>
            {
                var newDevice = new DiscoveredDevice()
                {
                    DeviceName = parsedDeviceName,
                    DeviceModel = deviceModel,
                    Architecture = arch,
                    OsVersion = osVersion,
                    IpAddress = ipAddress,
                    UniqueId = deviceGuid,
                    Manage = new Uri($"http://administrator@{ipV4Address}/"),
                    Authentication = DialogAuthenticate.GetSavedPassword(deviceName)
                };

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    bool found = false;
                    foreach (DiscoveredDevice d in ConfiguredDevices)
                    {
                        if ((d.IpAddress != null) && d.IpAddress.Equals(newDevice.IpAddress))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        ConfiguredDevices.Add(newDevice);
                        AllDevices.Add(newDevice);
                    }
                }));

                return newDevice;
            });

            device.Seen();
        }

        /// <summary>
        /// Called by the DeviceDiscovery DLL when a device announces itself
        /// </summary>
        /// <param name="Name">The device name</param>
        /// <param name="IP">The device's IP address</param>
        /// <param name="Mac">The device's Ethernet address</param>
        private void BroadcastWatcher_Ping(string Name, string IP, string Mac)
        {
            AddDeviceCallback(Name, IP, "");
        }

        /// <summary>
        /// Starts the process of scanning for new devices.  Any devices that we are already
        /// aware of will have their timeouts reset 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScanNewDevicesTimerTick(object sender, EventArgs e)
        {
            StartDiscovery();
        }

        /// <summary>
        /// This function ends listening for new devices and scans/deletes any devices that haven't
        /// answered in the specified amount of time
        /// </summary>
        /// <param name="sender">Set from timer event</param>
        /// <param name="e">Set from timer event</param>
        private void StopScanNewDevicesTimerTick(object sender, EventArgs e)
        {
            // Always be sure to stop listening for new devices and timers so they
            // don't reenter this or add functions
            StopDiscovery();

            // scan known devices for any that haven't been found in a while
            DateTime devicesTooOld = DateTime.Now - constMaxAgeDevice;

            // build a list of devices we want to expire, these will be removed from observable collections
            List<DiscoveredDevice> removeList = new List<DiscoveredDevice>();

            // Scan for devices in the mDNS and ebootpinger lists.  Any that haven't responded
            // in the specified time can be added to the removeList above
            foreach (var cur in _foundDevices)
            {
                if (cur.Value.LastSeen < devicesTooOld)
                {
                    removeList.Add(cur.Value);
                }
            }

            // If there are any items to be removed,
            if (removeList.Count > 0)
            {
                // From the UI thread,
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    // remove them from the observable collections
                    foreach (var cur in removeList)
                    {
                        DiscoveredDevice device;

                        string key = (cur.IpAddress == null) ? cur.DeviceName : cur.IpAddress.ToString();

                        // A device may exist in more than one list, scan each one and remove
                        if (_foundDevices.TryRemove(key, out device))
                            if (this.ConfiguredDevices.Contains(device))
                                this.ConfiguredDevices.Remove(device);

                        if (_adhocNetworks.TryRemove(cur.DeviceName, out device))
                            if (this.NewDevices.Contains(device))
                                this.NewDevices.Remove(device);

                        if (this.AllDevices.Contains(cur))
                            this.AllDevices.Remove(cur);
                    }
                }));
            }

            // This resets the time to scan for old devices again later
            _scanNewDevicesTimer.Start();
        }
    }
}
