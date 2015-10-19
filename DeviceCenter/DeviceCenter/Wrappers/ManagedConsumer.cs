using Onboarding;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DeviceCenter.Wrappers
{
    public class ManagedConsumer
    {
        private Onboarding.IOnboardingConsumer m_NativeConsumer;
        private string m_Name;

        public ManagedConsumer(Onboarding.IOnboardingConsumer nativeConsumer)
        {
            m_NativeConsumer = nativeConsumer;
            string bstr = m_NativeConsumer.GetDisplayName();

            m_Name = new string(bstr.ToCharArray());

            try
            {
                var comWifiList = m_NativeConsumer.GetScanInfo();
                if (comWifiList != null)
                {
                    for (uint i = 0, n = comWifiList.Size(); i < n; i++)
                    {
                        m_WifiList.Add(comWifiList.GetItem(i));
                    }
                }
            }
            catch (COMException error)
            {
                var x = error.HResult;
                if (x != 0)
                {

                }
            }
        }

        public List<IWifi> WifiList 
        {
            get { return m_WifiList; }
        }

        public string Name
        {
            get { return m_Name; }
        }

        public IOnboardingConsumer NativeConsumer
        {
            get { return m_NativeConsumer; }
        }

        private List<IWifi> m_WifiList = new List<IWifi>();
    }
}
