using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WLanTest
{
    class Program
    {
        static async void Run()
        {
            WlanClient client = new WlanClient();
            var interfaces = client.Interfaces;
            var interf = interfaces[0];
            interf.Scan();
            var list = interf.GetAvailableNetworkList();

            var sl = new SortedList<uint, WlanInterop.WlanAvailableNetwork>();
            foreach (var l in list)
            {
                string ssid = l.GetStringForSSID();
                if (ssid.StartsWith("AJ_"))
                {
                    Console.WriteLine("{0} {1}", ssid, l.wlanSignalQuality);
                    sl.Add(l.wlanSignalQuality, l);

                    break;
                }
            }

            if (sl.Values.Count == 0)
            {
                Console.WriteLine("no avaliable AJ_ network");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Connecting to " + sl.Values[0].GetStringForSSID());

            interf.Connect(
                WlanInterop.WlanConnectionMode.TemporaryProfile,
                WlanInterop.Dot11BssType.Any,
                sl.Values[0],
                WlanInterop.WlanConnectionFlags.AdhocJoinOnly,
                "password"
                );
            client.WaitConnectComplete();
            Console.WriteLine("End connect");

            var wmi = WMIHelper.CreateByNICGuid(interf.GUID);
            wmi.DebugPrint();
            var ipv4 = wmi.GetIPV4();
            Console.WriteLine("Curernt IP [{0}]", ipv4);
            bool isDHCP = Util.IsDHCPIPAddress(ipv4);
            Console.WriteLine(string.Format("Is DHCP IP [{0}]", isDHCP ? "yes" : "no"));

            if (!isDHCP)
            {
                Console.WriteLine("Switch to IP address 192.168.173.3");
                wmi.SetIP("192.168.173.3", "255.255.0.0");
            }

            bool isReachable = false;
            for(int i=0;i<10;i++)
            {
                isReachable = Util.Ping("192.168.173.1");
                Console.WriteLine("Reachable [{0}]", isReachable ? "yes" : "no");
                if (isReachable) break;
                await Task.Delay(500);
            }
        }

        static void Main(string[] args)
        {
            Run();
            Console.ReadLine();
        }
    }
}
