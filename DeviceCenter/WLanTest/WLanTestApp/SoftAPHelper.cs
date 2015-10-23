using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceCenter.WlanAPIs;
using System.Diagnostics;

namespace DeviceCenter
{
    public delegate void SoftAPDisconnectedHandler();

    public class SoftAPHelper
    {
        const string SOFT_AP_HOST_IP = "192.168.173.1";
        const string SOFT_AP_CLIENT_IP = "192.168.173.2";
        const string SOFT_AP_SUBNET_ADDR = "255.255.0.0";
        const string SOFT_AP_NAME_PREFIX = "AJ_";
        const int PING_RETRY_NUMBER = 10;
        const int PING_DELAY = 500;

        public event SoftAPDisconnectedHandler OnSoftAPDisconnected;

        public void Scan()
        {
            if (_wlanInterface == null)
            {
                Util.Error("Scan: No Wlan interface");
                return ;
            }

            _wlanInterface.Scan();
        }

        public IList<WlanInterop.WlanAvailableNetwork> GetAvailableNetworkList()
        {
            var networkList = new List<WlanInterop.WlanAvailableNetwork>();
            if (_wlanInterface == null)
            {
                Util.Error("GetAvailableNetworkList: No Wlan interface");
                return networkList;
            }

            Scan();
            // Sleep(4000);

            var list = _wlanInterface.GetAvailableNetworkList();
            var sortedList = new SortedList<uint, WlanInterop.WlanAvailableNetwork>();
            uint index = 0;
            foreach (var network in list)
            {
                string ssid = network.SSIDString;
                if (ssid.StartsWith(SOFT_AP_NAME_PREFIX))
                {
                    Debug.WriteLine(string.Format("{0} {1}", ssid, network.wlanSignalQuality));
                    // dup keys is not allowed
                    uint key = network.wlanSignalQuality * 10 + index;
                    sortedList.Add(key, network);
                    index++;
                }
            }
            IList<WlanInterop.WlanAvailableNetwork> descList = sortedList.Values.Reverse().ToList();
            return descList;
        }

        public async Task<bool> ConnectAsync(WlanInterop.WlanAvailableNetwork network, string password)
        {
            if (_wlanInterface == null)
            {
                Util.Error("Connect: No Wlan interface");
                return false;
            }

            return await Task<bool>.Run(
                async () =>
                {
                    Util.Info("--------- connecting to soft AP ----------");
                    if (ConnectToSoftAP(network, password) == false)
                    {
                        Util.Error("Failed to connect to soft AP");
                        return false;
                    }

                    Util.Info("--------- Checking IP and subnet ----------");
                    CheckIPAndSubnet();

                    Util.Info("--------- Testing connection ----------");
                    return await TestConnection();
                }
            );
        }

        private bool ConnectToSoftAP(WlanInterop.WlanAvailableNetwork network, string password)
        {
            _wlanInterface.Connect(
                            WlanInterop.WlanConnectionMode.TemporaryProfile,
                            WlanInterop.Dot11BssType.Any,
                            network,
                            password
                            );

            bool isSuccess = _wlanClient.WaitConnectComplete();
            if (!isSuccess)
            {
                return false;
            }
            _isConnected = true;

            return true;
        }

        private void CheckIPAndSubnet()
        {
            var wmi = WMIHelper.CreateByNICGuid(_wlanInterface.GUID);
            // wmi.DebugPrint();
            var ipv4 = wmi.GetIPV4();
            Util.Info("Curernt IP [{0}]", ipv4);


            bool isDHCP = Util.IsDHCPIPAddress(ipv4);
            Util.Info("Is DHCP IP [{0}]", isDHCP ? "yes" : "no");

            if (!isDHCP)
            {
                Util.Info("Switch to IP address {0}", SOFT_AP_CLIENT_IP);
                wmi.SetIP(SOFT_AP_CLIENT_IP, SOFT_AP_SUBNET_ADDR);
            }
        }

        private async Task<bool> TestConnection()
        {
            bool isReachable = false;
            for (int i = 0; i < PING_RETRY_NUMBER; i++)
            {
                isReachable = await Util.Ping(SOFT_AP_HOST_IP);
                Util.Info("Reachable [{0}]", isReachable ? "yes" : "no");
                if (isReachable)
                {
                    return true;
                }

                await Task.Delay(PING_DELAY);
            }

            Util.Error("Ping failed after retries");
            return false;
        }

        public void Disconnect()
        {
            if (_wlanInterface == null)
            {
                Util.Error("Disconnect: No Wlan interface");
            }

            _wlanInterface.Disconnect();
        }

        public static SoftAPHelper Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new SoftAPHelper();
                }

                return _instance;
            }
        }

        public string IPV4
        {
            get
            {
                var wmi = WMIHelper.CreateByNICGuid(_wlanInterface.GUID);
                return wmi.GetIPV4();
            }
        }

        private SoftAPHelper()
        {
            _wlanClient = new WlanClient();
            var interfaces = _wlanClient.Interfaces;
            if (interfaces != null && interfaces.Count != 0)
            {
                // TBD - to support multiple wlan interfaces
                _wlanInterface = interfaces[0];
                _wlanClient.OnACMNotification += OnACMNotification;
            }
        }

        private void OnACMNotification(string profileName, int notificationCode, WlanInterop.WlanReasonCode reasonCode)
        {
            switch ((WlanInterop.WlanNotificationCodeAcm)notificationCode)
            {
                case WlanInterop.WlanNotificationCodeAcm.Disconnected:
                    {
                        Util.Info("Disconnected from [{0}]", profileName);
                        if (_isConnected && profileName == Util.WLAN_PROFILE_NAME)
                        {
                            _isConnected = false;
                            if(OnSoftAPDisconnected != null)
                            {
                                OnSoftAPDisconnected();
                            }
                        }
                    }
                    break;
            }
        }

        private static SoftAPHelper _instance;
        private WlanClient _wlanClient;
        private WlanInterface _wlanInterface;
        private bool _isConnected;
    }
}
