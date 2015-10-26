using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public const string SoftApSubnetAddr = "255.255.0.0";
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
            catch(WLanException)
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
            IList<WlanInterop.WlanAvailableNetwork> descList = sortedWLanNetworks.Values.Reverse().ToList();
            return descList;
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
                    CheckIpAndSubnet();

                    Util.Info("--------- Testing connection ----------");
                    return await TestConnection();
                }
            );
        }

        public void Disconnect()
        {
            if (_wlanInterface == null)
            {
                Util.Error("Disconnect: No Wlan interface");
            }

            var wmi = WmiHelper.CreateByNicGuid(_wlanInterface.Guid);
            Util.Info("Enable DHCP");
            wmi.EnableDhcp();

            try
            {
                _wlanInterface.Disconnect();
            }
            catch(WLanException)
            {
                return;
            }
        }

        public string IPV4
        {
            get
            {
                var wmi = WmiHelper.CreateByNicGuid(_wlanInterface.Guid);
                return wmi.GetIpv4();
            }
        }

        public SoftApHelper()
        {
            _wlanClient = new WlanClient();
            var interfaces = _wlanClient.Interfaces;
            if (interfaces != null && interfaces.Count != 0)
            {
                // TBD - to support multiple wlan interfaces
                _wlanInterface = interfaces[0];
                _wlanClient.OnAcmNotification += OnACMNotification;
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
            }
            catch(WLanException)
            {
                return false;
            }

            var isSuccess = _wlanClient.WaitConnectComplete();
            if (!isSuccess)
            {
                return false;
            }
            _isConnected = true;

            return true;
        }

        private void CheckIpAndSubnet()
        {
            var wmi = WmiHelper.CreateByNicGuid(_wlanInterface.Guid);
            // wmi.DebugPrint();
            var ipv4 = wmi.GetIpv4();
            Util.Info("Curernt IP [{0}]", ipv4);


            bool isDhcp = Util.IsDhcpipAddress(ipv4);
            Util.Info("Is DHCP IP [{0}]", isDhcp ? "yes" : "no");

            if (!isDhcp)
            {
                Util.Info("Switch to IP address {0}", SoftApClientIp);
                wmi.SetIp(SoftApClientIp, SoftApSubnetAddr);
            }
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
                        if (_isConnected && profileName == Util.WlanProfileName)
                        {
                            _isConnected = false;
                            OnSoftApDisconnected?.Invoke();
                        }
                    }
                    break;
            }
        }

        private readonly WlanClient _wlanClient;
        private readonly WlanInterface _wlanInterface;
        private bool _isConnected;
        #endregion
    }
}
