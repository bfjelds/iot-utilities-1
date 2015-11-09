using System;
using Onboarding;

namespace DeviceCenter.Wrappers
{
    public class ManagedWifi
    {
        private string m_Ssid;

        private IWifi m_NativeWifi;

        public ManagedWifi(IWifi nativeWifi)
        {
            m_NativeWifi = nativeWifi;

            m_Ssid = new string(nativeWifi.GetSSID().ToCharArray());
        }

        public IWifi NativeWifi
        {
            get { return m_NativeWifi; }
        }
        public string Ssid
        {
            get { return m_Ssid; }
        }

        public string Security
        {
            get
            {
                if(m_NativeWifi.IsLocalWifi() == 0)
                {
                    return m_NativeWifi.GetSecurity() != 0 ? "Secure" : "Open";
                }
                else
                {
                    return m_NativeWifi.IsSecurityEnabled() == 0 ? "Open" : "Secure";
                }
            }
        }

        public Int16 IntSecurity
        {
            get { return m_NativeWifi.GetSecurity(); }
        }
    }
}
