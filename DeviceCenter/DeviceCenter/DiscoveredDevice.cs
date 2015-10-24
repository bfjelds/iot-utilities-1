using DeviceCenter.WlanAPIs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DeviceCenter
{
    public class DiscoveredDevice
    {
        public enum NetworkType { ethernet, adhoc };
        public DiscoveredDevice()
        {
            this.Network = NetworkType.ethernet;

            this.ManageVisible = Visibility.Visible;
            this.ConnectVisible = Visibility.Collapsed;
        }

        public DiscoveredDevice(WlanInterop.WlanAvailableNetwork wifi)
        {
            this.Network = NetworkType.adhoc;

            this.ManageVisible = Visibility.Collapsed;
            this.ConnectVisible = Visibility.Visible;
            this.Authentication = null;

            this.WifiInstance = wifi;
        }

        public override string ToString()
        {
            return this.DeviceName;
        }

        public WlanInterop.WlanAvailableNetwork WifiInstance { get; private set; }

        public NetworkType Network { get; private set; }
        public string DeviceName { get; set; }
        public string DeviceModel { get; set; }
        public IPAddress IPAddress { get; set; }
        public string OSVersion { get; set; }
        public string Architecture { get; set; }
        public Guid UniqueId { get; set; }
        public Uri Manage { get; set; }
        public Visibility ManageVisible { get; private set; }
        public Visibility ConnectVisible { get; private set; }
        public UserInfo Authentication { get; set; }
    }
}
