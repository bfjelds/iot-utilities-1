using System;
using Onboarding;

namespace DeviceCenter.Handlers
{
    class OnboardeeAddedHandler : IOnboardeeAdded
    {
        Action<OnboardingConsumer> m_Action;
        public OnboardeeAddedHandler(Action<OnboardingConsumer> action)
        {
            m_Action = action;
        }

        public void Added(OnboardingConsumer consumer)
        {
            if (m_Action != null)
            {
                m_Action(consumer);
            }
        }
    }
}
