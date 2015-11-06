using System;
using System.Collections.Concurrent;
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
        // Bind to one of these when applying filtering
        public ObservableCollection<DiscoveredDevice> AllDevices { get; }
        public ObservableCollection<DiscoveredDevice> NewDevices { get; }
        public ObservableCollection<DiscoveredDevice> ConfiguredDevices { get; }

        readonly NativeMethods.AddDeviceCallbackDelegate _addCallbackdel;
        private BroadcastWatcher _broadCastWatcher = new BroadcastWatcher();
        private readonly DispatcherTimer _broadCastWatcherStartTimer = new DispatcherTimer();
        private readonly DispatcherTimer _broadCastWatcherStopTimer = new DispatcherTimer();
        private readonly int pollDelayBroadcast = 2;
        private readonly int pollBroadcastListenInterval = 5;
        private readonly ConcurrentDictionary<string, WlanInterop.WlanAvailableNetwork> _adhocNetworks = new ConcurrentDictionary<string, WlanInterop.WlanAvailableNetwork>();

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

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    NativeMethods.StopDiscovery();
                    _broadCastWatcher.RemoveListeners();
                    _broadCastWatcherStartTimer.Stop();
                    _broadCastWatcherStopTimer.Stop();
                    _broadCastWatcherStartTimer.Tick -= StartBroadCastListener;
                    _broadCastWatcherStopTimer.Tick -= StopBroadCastListener;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private DiscoveryHelper()
        {
            AllDevices = new ObservableCollection<DiscoveredDevice>();
            NewDevices = new ObservableCollection<DiscoveredDevice>();
            ConfiguredDevices = new ObservableCollection<DiscoveredDevice>();

            _addCallbackdel = new NativeMethods.AddDeviceCallbackDelegate(AddDeviceCallback);

            StartDiscovery();
        }

        private void StartDiscovery()
        {
            // Stop everything first 
            _broadCastWatcherStartTimer.Stop();
            _broadCastWatcherStopTimer.Stop();
            _broadCastWatcher.RemoveListeners();
            NativeMethods.StopDiscovery();

            // Start mDNS based discovery 

            // 1. Register the callback
            NativeMethods.RegisterCallback(_addCallbackdel);

            // 2. Start device discovery using DNS-SD
            NativeMethods.StartDiscovery();

            // Wait for 2 second and start Broadcast discovery 
            _broadCastWatcherStartTimer.Interval = TimeSpan.FromSeconds(pollDelayBroadcast);
            _broadCastWatcherStartTimer.Tick += StartBroadCastListener;
            _broadCastWatcherStartTimer.Start();
        }

        public void AddAdhocDevice(WlanInterop.WlanAvailableNetwork accessPoint)
        {
            _adhocNetworks.GetOrAdd(accessPoint.SsidString, (key) =>
            {
                var newDevice = new DiscoveredDevice(accessPoint)
                {
                    DeviceName = key
                };

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    AllDevices.Add(newDevice);
                    NewDevices.Add(newDevice);
                }));

                return accessPoint;
            });
        }

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
            IPAddress ipAddress = IPAddress.Parse(ipV4Address);

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

            if (Application.Current == null)
            {
                return;
            }

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
        }
        private void StartBroadCastListener(object sender, EventArgs e)
        {
            // Stop listening after 5 seconds
            _broadCastWatcherStopTimer.Interval = TimeSpan.FromSeconds(pollBroadcastListenInterval);
            _broadCastWatcherStopTimer.Tick += StopBroadCastListener;
            _broadCastWatcher.OnPing += new BroadcastWatcher.PingHandler(BroadcastWatcher_Ping);
            _broadCastWatcher.AddListeners();
            _broadCastWatcherStopTimer.Start();
            _broadCastWatcherStartTimer.Stop();
        }

        private void BroadcastWatcher_Ping(string Name, string IP, string Mac)
        {
            AddDeviceCallback(Name, IP, "");
        }

        private void StopBroadCastListener(object sender, EventArgs e)
        {
            _broadCastWatcher.RemoveListeners();
            _broadCastWatcherStopTimer.Stop();
        }
    }
}
