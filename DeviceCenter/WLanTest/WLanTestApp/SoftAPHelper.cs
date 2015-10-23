using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceCenter.WlanAPIs;
using System.Diagnostics;

namespace DeviceCenter
{
    public class SoftAPHelper
    {
        const string SOFT_AP_HOST_IP = "192.168.173.1";
        const string SOFT_AP_CLIENT_IP = "192.168.173.2";
        const string SOFT_AP_SUBNET_ADDR = "255.255.0.0";
        const string SOFT_AP_NAME_PREFIX = "AJ_";
        const int PING_RETRY_NUMBER = 10;
        const int PING_DELAY = 500;

        public void Scan()
        {
            if (_wlanInterface == null)
            {
                Debug.WriteLine("Scan: No Wlan interface");
                return ;
            }

            _wlanInterface.Scan();
        }

        public IList<WlanInterop.WlanAvailableNetwork> GetAvailableNetworkList()
        {
            var networkList = new List<WlanInterop.WlanAvailableNetwork>();
            if (_wlanInterface == null)
            {
                Debug.WriteLine("GetAvailableNetworkList: No Wlan interface");
                return networkList;
            }

            Scan();
            // Sleep(4000);

            var list = _wlanInterface.GetAvailableNetworkList();
            var sortedList = new SortedList<uint, WlanInterop.WlanAvailableNetwork>();
            foreach (var network in list)
            {
                string ssid = network.SSIDString;
                if (ssid.StartsWith(SOFT_AP_NAME_PREFIX))
                {
                    Debug.WriteLine(string.Format("{0} {1}", ssid, network.wlanSignalQuality));
                    sortedList.Add(network.wlanSignalQuality, network);
                }
            }
            IList<WlanInterop.WlanAvailableNetwork> descList = sortedList.Values.Reverse().ToList();
            return descList;
        }

        public async Task<bool> ConnectAsync(WlanInterop.WlanAvailableNetwork network, string password)
        {
            if (_wlanInterface == null)
            {
                Debug.WriteLine("Connect: No Wlan interface");
                return false;
            }

            _ssidToConnect = network.SSIDString;

            _wlanInterface.Connect(
                WlanInterop.WlanConnectionMode.TemporaryProfile,
                WlanInterop.Dot11BssType.Any,
                network,
                password
                );

            bool isSuccess = _wlanClient.WaitConnectComplete();
            if(!isSuccess)
            {
                return false;
            }

            var wmi = WMIHelper.CreateByNICGuid(_wlanInterface.GUID);
            // wmi.DebugPrint();
            var ipv4 = wmi.GetIPV4();
            Debug.WriteLine(string.Format("Curernt IP [{0}]", ipv4));


            bool isDHCP = Util.IsDHCPIPAddress(ipv4);
            Debug.WriteLine(string.Format("Is DHCP IP [{0}]", isDHCP ? "yes" : "no"));

            if (!isDHCP)
            {
                Debug.WriteLine(string.Format("Switch to IP address {0}", SOFT_AP_CLIENT_IP));
                wmi.SetIP(SOFT_AP_CLIENT_IP, SOFT_AP_SUBNET_ADDR);
            }

            bool isReachable = false;
            for (int i = 0; i < PING_RETRY_NUMBER; i++)
            {
                isReachable = Util.Ping(SOFT_AP_HOST_IP);
                Debug.WriteLine(string.Format("Reachable [{0}]", isReachable ? "yes" : "no"));
                if (isReachable)
                {
                    return true;
                }

                await Task.Delay(PING_DELAY);
            }

            return false;
        }

        public void Disconnect()
        {
            if (_wlanInterface == null)
            {
                Debug.WriteLine("Disconnect: No Wlan interface");
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
            }
        }

        private static SoftAPHelper _instance;
        private WlanClient _wlanClient;
        private WlanInterface _wlanInterface;
        private string _ssidToConnect;
        private bool _isConnected;
    }
}
