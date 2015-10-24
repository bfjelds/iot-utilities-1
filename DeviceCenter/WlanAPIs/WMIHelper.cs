using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.WlanAPIs
{
    public class WMIHelper
    {
        static public WMIHelper CreateByNICGuid(Guid interfaceGuid)
        {
            if(interfaceGuid == Guid.Empty)
            {
                return null;
            }

            var newInstance = new WMIHelper();

            // netsh interface ip show addresses
            // netsh interface ip show addresses "Wi-Fi"
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                string settingID = mo["SettingID"].ToString();
                try
                {
                    var guid = Guid.Parse(settingID);

                    if(guid == interfaceGuid)
                    {
                        newInstance._networkAdapterMO = mo;
                    }
                }
                catch(Exception ex)
                {
                    Util.Error("Error occurred parsing SettingID - [{0}]", settingID);
                    Util.Error("  {0}", ex.Message);
                }
            }

            Debug.Assert(newInstance._networkAdapterMO != null);

            return newInstance;
        }

        public string GetIPV4()
        {
            Debug.Assert(_networkAdapterMO != null);
            string ip = string.Empty;
            var ips = (string[])_networkAdapterMO["IPAddress"];
            if(ips != null && ips.Length > 0)
            {
                ip = ips[0];
            }
            return ip;
        }

        public void SetIP(string ipAddresses, string subnetMask)
        {
            Debug.Assert(_networkAdapterMO != null);

            Util.Info("WMIHelper: set IP to [{0}] subnet [{1}]", ipAddresses, subnetMask);
            ManagementBaseObject newIP = _networkAdapterMO.GetMethodParameters("EnableStatic");
            ManagementBaseObject newDNS = _networkAdapterMO.GetMethodParameters("SetDNSServerSearchOrder");

            newIP["IPAddress"] = ipAddresses.Split(',');
            newIP["SubnetMask"] = new string[] { subnetMask };

            TraceMOResult(_networkAdapterMO.InvokeMethod("EnableStatic", newIP, null), "EnableStatic");
        }

        public void EnableDHCP()
        {
            Debug.Assert(_networkAdapterMO != null);

            // netsh interface ip set address "Wi-Fi" dhcp
            Util.Info("WMIHelper: Enabling DHCP");

            var newDNS = _networkAdapterMO.GetMethodParameters("SetDNSServerSearchOrder");
            newDNS["DNSServerSearchOrder"] = null;
            TraceMOResult(_networkAdapterMO.InvokeMethod("EnableDHCP", null, null), "EnableDHCP");
            TraceMOResult(_networkAdapterMO.InvokeMethod("SetDNSServerSearchOrder", newDNS, null), "SetDNSServerSearchOrder");
        }

        public void DebugPrint()
        {
            Util.Info("----------------");
            foreach (PropertyData prop in _networkAdapterMO.Properties)
            {
                Util.Info("{0}: {1}", prop.Name, prop.Value);
            }
            Util.Info("----------------");
        }

        private void TraceMOResult(ManagementBaseObject mo, string methodName)
        {
            Util.Info("===== MO method [{0}] returns - [{1}]", methodName, mo["returnValue"]);
        }

        private WMIHelper()
        {

        }

        private ManagementObject _networkAdapterMO;
    }
}
