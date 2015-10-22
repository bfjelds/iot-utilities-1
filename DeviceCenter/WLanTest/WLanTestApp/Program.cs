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
        static async void Run()
        {
            var softAP = SoftAPHelper.Instance;
            softAP.Scan();
            var list = softAP.GetAvailableNetworkList();
            if(list.Count == 0)
            {
                Console.WriteLine("No AJ network");
                return;
            }

            bool success = await softAP.ConnectAsync(list[0], "password");
            Console.WriteLine("Connect " + (success ? "succeeded":"failed"));
        }

        static void Main(string[] args)
        {
            Run();
            Console.ReadLine();
        }
    }
}
