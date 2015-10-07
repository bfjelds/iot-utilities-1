using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Onboarding;

namespace IoTOnboardingConsumerWPF.Handlers
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
