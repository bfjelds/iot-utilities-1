// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Management;

namespace WlanAPIs
{
    /// <summary>
    /// Access to WMI apis.
    /// </summary>
    public class WmiHelper
    {
        private const string NetshSetStaticIpArgument = "interface ip set address \"{0}\" static 192.168.173.2 255.255.0.0";
        private const string NetshEnableDhcpArgument = "interface ip set address \"{0}\" dhcp";

        static public WmiHelper CreateByNicGuid(Guid interfaceGuid)
        {
            if(interfaceGuid == Guid.Empty)
            {
                return null;
            }

            var newInstance = new WmiHelper();

            // netsh interface ip show addresses
            // netsh interface ip show addresses "Wi-Fi"
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var moc = mc.GetInstances();
            foreach (var o in moc)
            {
                var mo = (ManagementObject) o;
                var settingId = mo["SettingID"].ToString();
                try
                {
                    var guid = Guid.Parse(settingId);

                    if(guid == interfaceGuid)
                    {
                        newInstance._networkAdapterMo = mo;
                        newInstance._interfaceName = Util.GetNameByGuid(guid);
                    }
                }
                catch(Exception ex)
                {
                    Util.Error("Error occurred parsing SettingID - [{0}]", settingId);
                    Util.Error("  {0}", ex.Message);
                }
            }

            Debug.Assert(newInstance._networkAdapterMo != null);

            return newInstance;
        }

        public string GetIpv4()
        {
            Debug.Assert(_networkAdapterMo != null);
            var ip = string.Empty;
            var ips = (string[])_networkAdapterMo["IPAddress"];
            if(ips != null && ips.Length > 0)
            {
                ip = ips[0];
            }
            return ip;
        }

        public void SetIp(string ipAddresses, string subnetMask)
        {
            Debug.Assert(_networkAdapterMo != null);

            Util.Info("WMIHelper: Seting static IP to [{0}] [{1}]", ipAddresses, subnetMask);
            string argument = string.Format(NetshSetStaticIpArgument, _interfaceName);
            Util.RunNetshElevated(argument);
        }

        public void EnableDhcp()
        {
            Debug.Assert(_networkAdapterMo != null);

            // netsh interface ip set address "Wi-Fi" dhcp
            Util.Info("WMIHelper: Enabling DHCP");

            var argument = string.Format(NetshEnableDhcpArgument, _interfaceName);
            Util.RunNetshElevated(argument);
        }

        public void DebugPrint()
        {
            Util.Info("----------------");
            foreach (var prop in _networkAdapterMo.Properties)
            {
                Util.Info("{0}: {1}", prop.Name, prop.Value);
            }
            Util.Info("----------------");
        }

        private void TraceMoResult(ManagementBaseObject mo, string methodName)
        {
            Util.Info("===== MO method [{0}] returns - [{1}]", methodName, mo["returnValue"]);
        }

        private WmiHelper()
        {

        }

        private ManagementObject _networkAdapterMo;
        private string _interfaceName;
    }
}
