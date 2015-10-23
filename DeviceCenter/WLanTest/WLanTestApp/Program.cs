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

namespace WLanTestApp
{
    class Program
    {
        static int GetUserInput(int maxCount)
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

        static async void Run()
        {
            // scan
            var softAP = SoftAPHelper.Instance;
            IList<WlanInterop.WlanAvailableNetwork> list;
            while (true)
            {
                list = softAP.GetAvailableNetworkList();
                if (list.Count == 0)
                {
                    Console.WriteLine("No AJ network");
                    await Task.Delay(4000);
                    continue;
                }

                break;
            }

            foreach(var n in list)
            {
                Console.WriteLine(string.Format("[{0}] - [{1}]", n.SSIDString, n.wlanSignalQuality));
            }

            Console.WriteLine("select a network, press any key for the first one");
            int index = GetUserInput(list.Count);

            // connect
            var network = list[index];
            string ssid = network.SSIDString;
            Console.WriteLine("Start to connect to " + ssid);
            bool success = await softAP.ConnectAsync(network, "password");
            Console.WriteLine(string.Format("Current IP [{0}]", softAP.IPV4));
            Console.WriteLine("Connect " + (success ? "succeeded":"failed"));

            // disconnect
            Console.WriteLine("Start to disconnect");
            softAP.Disconnect();
        }

        static void Main(string[] args)
        {
            try
            {
                Run();
            }
            catch(WLanException wlEx)
            {
                Console.WriteLine(wlEx.ToString());
            }

            Console.ReadLine();
        }
    }
}
