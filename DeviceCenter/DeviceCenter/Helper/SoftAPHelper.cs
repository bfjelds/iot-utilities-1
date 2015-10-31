using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WlanAPIs;

namespace DeviceCenter.Helper
{
    public delegate void SoftApDisconnectedHandler();

    public class SoftApHelper
    {
        #region consts
        public const string SoftApHostIp = "192.168.173.1";
        public const string SoftApClientIp = "192.168.173.2";
        public const string SoftApSubnetAddr = "255.255.255.0";
        public const string SoftApNamePrefix = "AJ_";
        public const int PingRetryNumber = 10;
        public const int PingDelay = 500;
        #endregion

        #region public
        public event SoftApDisconnectedHandler OnSoftApDisconnected;

        public IList<WlanInterop.WlanAvailableNetwork> GetAvailableNetworkList()
        {
            var networkList = new List<WlanInterop.WlanAvailableNetwork>();
            if (_wlanInterface == null)
            {
                Util.Error("GetAvailableNetworkList: No Wlan interface");
                return networkList;
            }

            List<WlanInterop.WlanAvailableNetwork> nativeNetworkList = null;
            try
            {
                _wlanInterface.Scan();

                nativeNetworkList = _wlanInterface.GetAvailableNetworkList();
            }
            catch (WLanException)
            {
                // error occurred at calling wlan WINAPI
                return networkList;
            }

            var sortedWLanNetworks = new SortedList<uint, WlanInterop.WlanAvailableNetwork>();
            var uniqueWlanNetworks = new HashSet<string>();

            uint index = 0;
            foreach (var network in nativeNetworkList)
            {
                var ssid = network.SsidString;

                if (!ssid.StartsWith(SoftApNamePrefix) || uniqueWlanNetworks.Contains(ssid)) continue;

                Debug.WriteLine($"{ssid} {network.wlanSignalQuality}");

                // dup keys is not allowed
                var key = network.wlanSignalQuality * 10 + index;
                sortedWLanNetworks.Add(key, network);
                uniqueWlanNetworks.Add(ssid);
                index++;
            }

            return sortedWLanNetworks.Values.Reverse().ToList();
        }

        public async Task<bool> ConnectAsync(WlanInterop.WlanAvailableNetwork network, string password)
        {
            if (_wlanInterface == null)
            {
                Util.Error("Connect: No Wlan interface");
                return false;
            }

            return await Task.Run(
                async () =>
                {
                    Util.Info("--------- connecting to soft AP ----------");
                    if (ConnectToSoftAp(network, password) == false)
                    {
                        Util.Error("Failed to connect to soft AP");
                        return false;
                    }

                    Util.Info("--------- Checking IP and subnet ----------");

                    if (CheckIpAndSubnet() == false)
                    {
                        Util.Error("Failed to Check Ip And Subnet");
                        return false;
                    }

                    Util.Info("--------- Testing connection ----------");
                    return await TestConnection();
                }
            );
        }

        public void Disconnect()
        {
            // tbd this code need refactor to cleanly handle when Wi-Fi doesn't exist in the system.

            if (_wlanInterface == null)
            {
                Util.Error("Disconnect: No Wlan interface");
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

            if(_subnetHelper != null && !_subnetHelper.EnableDhcp())
            {
                Debug.WriteLine("User selects not to enable DHCP");
            }

            try
            {
                if (_isConnectedToSoftAP && _wlanInterface != null)
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

        public IPAddress IPV4
        {
            get
            {
                return _subnetHelper.GetIpv4();
            }
        }

        private SoftApHelper()
        {
            try
            {
                _wlanClient = new WlanClient();
            }
            catch (WLanException)
            {
                _wlanClient = null;
                _wlanInterface = null;
            }

            var interfaces = _wlanClient?.Interfaces;

            if (interfaces != null && interfaces.Count != 0)
            {
                // TBD - to support multiple wlan interfaces
                _wlanInterface = interfaces[0];
                _wlanClient.OnAcmNotification += OnACMNotification;
                _subnetHelper = SubnetHelper.CreateByNicGuid(_wlanInterface.Guid);
            }
        }

        public static SoftApHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SoftApHelper();
                }

                return _instance;
            }
        }
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

				_isConnectedToSoftAP = _wlanClient.WaitConnectComplete();
            }
            catch (WLanException)
            {
                _isConnectedToSoftAP = false;
            }

            return _isConnectedToSoftAP;
        }

        private bool CheckIpAndSubnet()
        {
            var ipv4 = _subnetHelper.GetIpv4();
            Util.Info("Curernt IP [{0}]", ipv4);


            bool isDhcp = Util.IsDhcpipAddress(ipv4.ToString());
            Util.Info("Is DHCP IP [{0}]", isDhcp ? "yes" : "no");

            if (!isDhcp)
            {
                Util.Info("Switch to IP address {0}", SoftApClientIp);
                return _subnetHelper.SetIp(SoftApClientIp, SoftApSubnetAddr);
            }

            return true;
        }

        private async Task<bool> TestConnection()
        {
            for (var i = 0; i < PingRetryNumber; i++)
            {
                var isReachable = await Util.Ping(SoftApHostIp);
                Util.Info("Reachable [{0}]", isReachable ? "yes" : "no");
                if (isReachable)
                {
                    return true;
                }

                await Task.Delay(PingDelay);
            }

            Util.Error("Ping failed after retries");
            return false;
        }

        private void OnACMNotification(string profileName, int notificationCode, WlanInterop.WlanReasonCode reasonCode)
        {
            switch ((WlanInterop.WlanNotificationCodeAcm)notificationCode)
            {
                case WlanInterop.WlanNotificationCodeAcm.Disconnected:
                    {
                        Util.Info("Disconnected from [{0}]", profileName);
                        if (_isConnectedToSoftAP && profileName == Util.WlanProfileName)
                        {
                            _isConnectedToSoftAP = false;
                            OnSoftApDisconnected?.Invoke();
                        }
                    }
                    break;
            }
        }

        private readonly WlanClient _wlanClient;
        private readonly WlanInterface _wlanInterface;
        private bool _isConnectedToSoftAP;
        private SubnetHelper _subnetHelper;
        private static SoftApHelper _instance;
        private bool _isDisconnecting;
        private Object _disconnectLockObj = new Object();
        #endregion
    }
}
