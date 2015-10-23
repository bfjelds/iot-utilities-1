﻿using System;
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
                    Debug.WriteLine(string.Format("Error occurred parsing SettingID - [{0}]", settingID));
                    Debug.WriteLine(string.Format("  {0}", ex.Message));
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

        public void SetIP(string IpAddresses, string SubnetMask)
        {
            Debug.Assert(_networkAdapterMO != null);

            ManagementBaseObject newIP = _networkAdapterMO.GetMethodParameters("EnableStatic");
            ManagementBaseObject newGate = _networkAdapterMO.GetMethodParameters("SetGateways");
            ManagementBaseObject newDNS = _networkAdapterMO.GetMethodParameters("SetDNSServerSearchOrder");

            newGate["GatewayCostMetric"] = new int[] { 1 };

            newIP["IPAddress"] = IpAddresses.Split(',');
            newIP["SubnetMask"] = new string[] { SubnetMask };

            _networkAdapterMO.InvokeMethod("EnableStatic", newIP, null);
            _networkAdapterMO.InvokeMethod("SetGateways", newGate, null);
        }

        public void DebugPrint()
        {
            Debug.WriteLine("----------------");
            foreach (PropertyData prop in _networkAdapterMO.Properties)
            {
                Debug.WriteLine(string.Format("{0}: {1}", prop.Name, prop.Value));
            }
            Debug.WriteLine("----------------");
        }

        private WMIHelper()
        {

        }

        private ManagementObject _networkAdapterMO;
    }
}
