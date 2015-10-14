using System;
using Onboarding;

namespace DeviceCenter.Handlers
{
    class WifiConnectionResultHandler : IWifiConnectionResult
    {
        Action<int, string> m_Action;

        public WifiConnectionResultHandler(Action<int, string> action)
        {
            m_Action = action;
        }

        public void ConnectionResult(int reasonCode, string reasonStr)
        {
            if (m_Action != null)
            {
                m_Action(reasonCode, reasonStr);
            }
        }
    }
}
