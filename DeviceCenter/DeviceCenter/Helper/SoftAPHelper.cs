using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WlanAPIs;

namespace DeviceCenter.Helper
{
    public delegate void SoftApDisconnectedHandler(object sender, EventArgs e);
    public delegate void WlanScanCompleteHandler(object sender, WlanScanCompleteArgs e);

    public class WlanScanCompleteArgs : EventArgs
    {
        public IList<WlanInterop.WlanAvailableNetwork> AvaliableNetworks { get; set; }
    }

    enum WlanConnectResult
    {
        Success = 0,
        FailWlan,
        FailUpdateIPRouting,
        FailPing
    }

    public class SoftApHelper
    {
        #region consts
        public const string SoftApHostIp = "192.168.173.1";
        public const string SoftApClientIp = "192.168.173.2";
        public const string SoftApSubnetAddr = "255.255.255.0";
        public const string SoftApPassword = "password";
        public const string SoftApNamePrefix = "AJ_";
        public const int PingRetryNumber = 30;
        public const int PingDelay = 500;
        public const int PollDelay = 5;
        #endregion

        #region public
        public event SoftApDisconnectedHandler OnSoftApDisconnected;
        public event WlanScanCompleteHandler OnWlanScanComplete;

        public void GetAvailableNetworkList()
        {
            Util.Info("========== start GetAvailableNetworkList =========");

            if (_wlanInterface == null)
            {
                Util.Error("GetAvailableNetworkList: No Wlan interface");
                return;
            }

            try
            {
                _wlanInterface.Scan();
            }
            catch (WLanException)
            {
                // error occurred at calling wlan WINAPI
            }
        }

        public async Task<bool> ConnectAsync(WlanInterop.WlanAvailableNetwork network, string password)
        {
            if (_wlanInterface == null)
            {
                Util.Error("Connect: No Wlan interface");
                return false;
            }

            var connectResult = await Task.Run(
                async () =>
                {
                    Util.Info("--------- connecting to soft AP ----------");
                    if (ConnectToSoftAp(network, password) == false)
                    {
                        Util.Error("Failed to connect to soft AP");
                        return WlanConnectResult.FailWlan;
                    }

                    Util.Info("--------- Checking IP and subnet ----------");

                    if (_ipRoutingHelper.AddLocalEntryIfNeeded(SoftApHostIp) == false)
                    {
                        Util.Error("Failed to Check Ip And Subnet");
                        return WlanConnectResult.FailUpdateIPRouting;
                    }

                    Util.Info("--------- Testing connection ----------");
                    if(await TestConnection() == false)
                    {
                        return WlanConnectResult.FailPing;
                    }

                    return WlanConnectResult.Success;
                }
            );

            App.TelemetryClient.TrackEvent("SoftAPConnectResult", new Dictionary<string, string>()
            {
                { "ConnectResult", connectResult.ToString()}
            });

            return connectResult == WlanConnectResult.Success;
        }

        public void DisconnectIfNeeded()
        {
            if (_wlanInterface == null)
            {
                Util.Info("Disconnect: No Wlan interface");
                return;
            }

            lock(_disconnectLockObj)
            {
                if (_isDisconnecting)
                {
                    Util.Info("Disconnecting, exit");
                    return;
                }

                _isDisconnecting = true;
            }

            try
            {
                if (_isConnectedToSoftAp)
                {
                    _wlanInterface.Disconnect();
                }
                else
                {
                    Util.Info("Disconnect: Not connected to any softAP");
                }
            }
            catch (WLanException)
            {
                return;
            }
            finally
            {
                _isDisconnecting = false;
            }
        }

        public void RemoveIPRoutingEntryIfNeeded()
        {
            try
            {
                if (_ipRoutingHelper != null && !_ipRoutingHelper.DeleteEntryIfNeeded(SoftApHostIp))
                {
                    Util.Error("User selects not to delete IP routing entry");
                }
            }
            catch (WLanException wlanEx)
            {
                if (wlanEx.ErrorCode == WLanException.ERROR_IPROUTINGTABLE_REMOVE_FAILED)
                {
                    App.TelemetryClient.TrackEvent("IPRoutingRemoveFailure", new Dictionary<string, string>()
                    {
                    });
                }
            }
        }

        private SoftApHelper()
        {
            try
            {
                _wlanClient = new WlanClient();

                var interfaces = _wlanClient?.Interfaces;

                if (interfaces != null && interfaces.Count != 0)
                {
                    Util.Info($"Found {interfaces.Count} wlan interfaces");
                    // TBD - to support multiple wlan interfaces
                    _wlanInterface = interfaces[0];
                    Util.Info(_wlanInterface.ToString());
                    Util.Info("Connected to " + _wlanInterface.CurrentConnection.ToString());
                    _wlanClient.OnAcmNotification += OnAcmNotification;
                    _ipRoutingHelper = IPRoutingTableHelper.CreateByNicGuid(_wlanInterface.Guid);
                }
                else
                {
                    Util.Info("No Wlan interface found");
                }
            }
            catch (WLanException)
            {
                _wlanClient = null;
                _wlanInterface = null;
                Util.Info("No Wlan interface found");
            }
        }

        public static SoftApHelper Instance => _instance ?? (_instance = new SoftApHelper());

        #endregion

        #region private
        private bool ConnectToSoftAp(WlanInterop.WlanAvailableNetwork network, string password)
        {
            try
            {
                _wlanInterface.Connect(
                            WlanInterop.WlanConnectionMode.TemporaryProfile,
                            WlanInterop.Dot11BssType.Any,
                            network,
                            password
                            );

				_isConnectedToSoftAp = _wlanClient.WaitConnectComplete();
            }
            catch (WLanException)
            {
                _isConnectedToSoftAp = false;
            }

            return _isConnectedToSoftAp;
        }

        private async Task<bool> TestConnection()
        {
            bool isReachable = false;
            int pingRetries = 0;

            for (pingRetries = 0; pingRetries < PingRetryNumber; pingRetries++)
            {
                isReachable = await Util.Ping(SoftApHostIp);
                Util.Info("([{0}]) - Reachable [{1}]", pingRetries, isReachable ? "yes" : "no");
                if (isReachable)
                {
                    break;
                }

                await Task.Delay(PingDelay);
            }

            App.TelemetryClient.TrackEvent("TestConnection", new Dictionary<string, string>()
            {
                { "Reachable", isReachable.ToString()},
                { "RetryNumber",  pingRetries.ToString()},
                { "WaitTime", (pingRetries * PingDelay / 1000).ToString()}
            });

            Util.Info("Is reachable? [{0}]", isReachable);

            return isReachable;
        }

        private void OnAcmNotification(string profileName, int notificationCode, WlanInterop.WlanReasonCode reasonCode)
        {
            switch ((WlanInterop.WlanNotificationCodeAcm)notificationCode)
            {
                case WlanInterop.WlanNotificationCodeAcm.Disconnected:
                    {
                        Util.Info("Disconnected from [{0}]", profileName);
                        if (_isConnectedToSoftAp)
                        {
                            _isConnectedToSoftAp = false;
                            OnSoftApDisconnected?.Invoke(this, new EventArgs());
                        }
                    }
                    break;
                case WlanInterop.WlanNotificationCodeAcm.ScanComplete:
                    {
                        HandleScanComplete();
                    }
                    break;
            }
        }

        private void HandleScanComplete()
        {
            var networkList = new List<WlanInterop.WlanAvailableNetwork>();
            try
            {
                networkList = _wlanInterface.GetAvailableNetworkList();
            }
            catch (WLanException)
            {
            }

            var sortedWLanNetworks = new SortedList<uint, WlanInterop.WlanAvailableNetwork>();
            var uniqueWlanNetworks = new HashSet<string>();

            uint index = 0;
            foreach (var network in networkList)
            {
                var ssid = network.SsidString;

                if (!ssid.StartsWith(SoftApNamePrefix) || uniqueWlanNetworks.Contains(ssid)) continue;

                Util.Info(network.ToString());

                // dup keys is not allowed
                var key = network.wlanSignalQuality * 10 + index;
                sortedWLanNetworks.Add(key, network);
                uniqueWlanNetworks.Add(ssid);
                index++;
            }

            var args = new WlanScanCompleteArgs();
            args.AvaliableNetworks = sortedWLanNetworks.Values.Reverse().ToList();

            OnWlanScanComplete?.Invoke(this, args);

            Util.Info("======== WlanScanComplete ============");
        }

        private readonly WlanClient _wlanClient;
        private readonly WlanInterface _wlanInterface;
        private bool _isConnectedToSoftAp;
        private readonly IPRoutingTableHelper _ipRoutingHelper;
        private static SoftApHelper _instance;
        private bool _isDisconnecting;
        private readonly object _disconnectLockObj = new object();
        #endregion
    }
}
