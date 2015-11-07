using System;
using System.Diagnostics;
using System.Net;
using System.Windows;
using WlanAPIs;

namespace DeviceCenter
{
    public class DiscoveredDevice
    {
        /// <summary>
        /// Icons reflecting signal strength of 802.11 networks around Device Center
        /// </summary>
        const string WiFiSignalStrengthIcons = "";
        
        /// <summary>
        /// Icon bound to xaml
        /// </summary>
        public string WiFiSignalStrengthIconDisplay
        {
            get
            {
                // SignalStrength goes from 0 to 99. 
                switch ((SignalStrength + 1) / 25)
                {
                    case 1 : return WiFiSignalStrengthIcons.Substring(1, 1);
                    case 2: return WiFiSignalStrengthIcons.Substring(2, 1);
                    case 3: return WiFiSignalStrengthIcons.Substring(3, 1);
                    case 4: return WiFiSignalStrengthIcons.Substring(4, 1);
                    default: return WiFiSignalStrengthIcons.Substring(0, 1);
                }
            }
        }
    
        /// <summary>
        /// Signal strength of this network
        /// </summary>
        public uint SignalStrength { get; set; }

        public enum NetworkType { Ethernet, Adhoc };

        public DateTime LastSeen { get; private set; }

        public void Seen()
        {
            this.LastSeen = DateTime.Now;
        }

        public DiscoveredDevice()
        {
            this.Network = NetworkType.Ethernet;

            this.ManageVisible = Visibility.Visible;
            this.ConnectVisible = Visibility.Collapsed;

            Seen();
        }

        public DiscoveredDevice(WlanInterop.WlanAvailableNetwork wifi)
        {
            this.Network = NetworkType.Adhoc;

            this.ManageVisible = Visibility.Collapsed;
            this.ConnectVisible = Visibility.Visible;
            this.Authentication = null;

            this.WifiInstance = wifi;
            this.SignalStrength = wifi.wlanSignalQuality;

            Debug.WriteLine("DiscoveredDevice(): SSID: {0} strength; {1}", wifi.SsidString, wifi.wlanSignalQuality);
        }

        public override string ToString()
        {
            return this.DeviceName;
        }

        public WlanInterop.WlanAvailableNetwork WifiInstance { get; private set; }

        public NetworkType Network { get; private set; }

        public string DeviceName { get; set; }

        public string DeviceModel { get; set; }

        public IPAddress IpAddress { get; set; }

        public string OsVersion { get; set; }

        public string Architecture { get; set; }

        public Guid UniqueId { get; set; }

        public Uri Manage { get; set; }

        public Visibility ManageVisible { get; private set; }

        public Visibility ConnectVisible { get; private set; }

        public UserInfo Authentication { get; set; }
    }
}
