using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DeviceCenter.WlanAPIs;
using DeviceCenter;
using System.Net.NetworkInformation;

namespace WLanTestApp
{
    class Program
    {
        int GetUserInput(int maxCount)
        {
            string read = Console.ReadLine();
            int index = 0;
            try
            {
                index = Convert.ToInt32(read);
            }
            catch
            { }

            if (index < 0 || index >= maxCount)
            {
                index = 0;
            }

            return index;
        }

        async void PrintSoftAPList()
        {
            while (true)
            {
                if (_isSoftAPSelected) break;

                _softAPNetworkList = _softAP.GetAvailableNetworkList();
                if (_softAPNetworkList.Count == 0)
                {
                    Console.WriteLine("No AJ network");
                }
                else
                {
                    Console.WriteLine("{0,16}   {1}", "SSID", "Signal Quality");
                    int index = 0;
                    foreach (var n in _softAPNetworkList)
                    {
                        Console.WriteLine(string.Format("{0}) [{1}] - [{2}]", index++, n.SSIDString, n.wlanSignalQuality));
                    }
                    Console.WriteLine("Enter [{0}-{1}], any key for [0]", 0, _softAPNetworkList.Count - 1);
                }
                Console.WriteLine();
                await Task.Delay(4000);
            }
        }

        async void Run()
        {
            PrintSoftAPList();
            int index = GetUserInput(_softAPNetworkList.Count);
            _isSoftAPSelected = true;

            // connect
            var network = _softAPNetworkList[index];
            string ssid = network.SSIDString;
            Console.WriteLine("Start to connect to " + ssid);
            bool success = await _softAP.ConnectAsync(network, "password");
            _isConnectFailed = !success;
            Console.WriteLine(string.Format("Current IP [{0}]", _softAP.IPV4));
            Console.WriteLine("Connect " + (success ? "succeeded" : "failed"));
            
            // disconnect
            Console.WriteLine("Press enter to disconnect");
            Console.ReadLine();

            _softAP.Disconnect();
        }

        Program()
        {
            _softAP = SoftAPHelper.Instance;
            _softAP.OnSoftAPDisconnected += OnSoftAPDisconnected;
        }

        private void OnSoftAPDisconnected()
        {
            // _isConnectFailed = true;
            Console.WriteLine("********* Info: disconnected from soft AP");
        }
        
        static void Main(string[] args)
        {
            try
            {
                new Program().Run();
            }
            catch(WLanException wlEx)
            {
                Console.WriteLine(wlEx.ToString());
            }

            Console.ReadLine();
            Console.ReadLine();
        }

        private SoftAPHelper _softAP;
        private IList<WlanInterop.WlanAvailableNetwork> _softAPNetworkList;
        private bool _isSoftAPSelected;
        private bool _isConnectFailed;
    }
}
