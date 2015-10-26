// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;

namespace WlanAPIs
{
    /// <summary>
    /// Helper class for setting static IP, enabling DHCP
    /// </summary>
    public class SubnetHelper
    {
        private const string NetshSetStaticIpArgument = "interface ip set address \"{0}\" static 192.168.173.2 255.255.0.0";
        private const string NetshEnableDhcpArgument = "interface ip set address \"{0}\" dhcp";

        static public SubnetHelper CreateByNicGuid(Guid interfaceGuid)
        {
            if(interfaceGuid == Guid.Empty)
            {
                return null;
            }

            var newInstance = new SubnetHelper();

            // netsh interface ip show addresses
            // netsh interface ip show addresses "Wi-Fi"
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var interf in interfaces.Where(interf => Guid.Parse(interf.Id) == interfaceGuid))
            {
                Util.Info("Find name [{0}] for guid [{1}]", interf.Name, interfaceGuid.ToString());
                newInstance._networkInterface = interf;
            }

            if (newInstance._networkInterface == null)
            {
                Util.Error("Can't Find name for guid [{1}]", interfaceGuid.ToString());
                return null;
            }

            return newInstance;
        }

        public IPAddress GetIpv4()
        {
            Debug.Assert(_networkInterface != null);
            if (_networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                _networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            {
                foreach (var ip in _networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.Address;
                    }
                }
            }

            return IPAddress.None;
        }

        public bool SetIp(string ipAddresses, string subnetMask)
        {
            Debug.Assert(_networkInterface != null);

            lock (_dhcpLockObj)
            {
                if (_isStaticIPSet) return true;
                _isStaticIPSet = true;
            }

            Util.Info("WMIHelper: Seting static IP to [{0}] [{1}]", ipAddresses, subnetMask);
            string argument = string.Format(NetshSetStaticIpArgument, _networkInterface.Name);
            _isStaticIPSet = Util.RunNetshElevated(argument);
            return _isStaticIPSet;
        }

        public bool EnableDhcp()
        {
            Debug.Assert(_networkInterface != null);

            lock(_dhcpLockObj)
            {
                if (!_isStaticIPSet) return true;
                _isStaticIPSet = false;
            }

            // netsh interface ip set address "Wi-Fi" dhcp
            Util.Info("WMIHelper: Enabling DHCP");

            var argument = string.Format(NetshEnableDhcpArgument, _networkInterface.Name);
            bool isDHCPEnabled = Util.RunNetshElevated(argument);
            _isStaticIPSet = !isDHCPEnabled;
            return isDHCPEnabled;
        }

        public void DebugPrint()
        {
            Util.Info("----------------");
            Util.Info("----------------");
        }

        private SubnetHelper()
        {
        }

        private NetworkInterface _networkInterface;
        private bool _isStaticIPSet;
        private Object _dhcpLockObj = new Object();
    }
}
