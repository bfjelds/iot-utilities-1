// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace WlanAPIs
{
    /// <summary>
    /// Helper class for setting static IP, enabling DHCP
    /// </summary>
    public class SubnetHelper
    {
        private const string NetshSetStaticIpArgument = "interface ip set address \"$inferfaceName\" static $ip $subnetMask";
        private const string NetshEnableDhcpArgument = "interface ip set address \"$interfaceName\" dhcp";

        static public SubnetHelper CreateByNicGuid(Guid interfaceGuid)
        {
            if (interfaceGuid == Guid.Empty)
            {
                return null;
            }

            var newInstance = new SubnetHelper();
            newInstance._networkInterfaceGuid = interfaceGuid;

            return newInstance;
        }

        public bool DisableDhcpIfNeeded(string ipAddress, string subnetMask)
        {
            var networkInterface = GetNetworkInterface();
            var ipv4 = Util.GetIpv4(networkInterface);

            if (ipv4 == IPAddress.None || !Util.IsDhcpipAddress(ipv4))
            {
                return true;
            }

            // netsh interface ip set address "Wi-Fi" 192.168.173.2 255.255.255.0
            Util.Info("SubnetHelper: Seting static IP to [{0}] [{1}]", ipAddress, subnetMask);
            var argument = NetshSetStaticIpArgument;
            argument = argument.Replace("$inferfaceName", networkInterface.Name);
            argument = argument.Replace("$ip", ipAddress);
            argument = argument.Replace("$subnetMask", subnetMask);
            return Util.RunNetshElevated(argument);
        }

        public bool EnableDhcpIfNeeded()
        {
            var networkInterface = GetNetworkInterface();
            var ipv4 = Util.GetIpv4(networkInterface);
            
            if (ipv4 == IPAddress.None || Util.IsDhcpipAddress(ipv4))
            {
                return true;
            }

            // netsh interface ip set address "Wi-Fi" dhcp
            Util.Info("SubnetHelper: Enabling DHCP");

            var argument = NetshEnableDhcpArgument.Replace("$interfaceName", networkInterface.Name);
            return Util.RunNetshElevated(argument);
        }

        private NetworkInterface GetNetworkInterface()
        {
            // netsh interface ip show addresses
            // netsh interface ip show addresses "Wi-Fi"
            NetworkInterface networkInterface = null;

            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var interf in interfaces.Where(interf => Guid.Parse(interf.Id) == _networkInterfaceGuid))
            {
                Util.Info("Find name [{0}] for guid [{1}]", interf.Name, _networkInterfaceGuid.ToString());
                networkInterface = interf;
            }

            if (networkInterface == null)
            {
                Util.Error("Can't Find name for guid [{0}]", _networkInterfaceGuid.ToString());
            }

            return networkInterface;
        }

        private SubnetHelper()
        {
        }

        private Guid _networkInterfaceGuid;
    }
}
