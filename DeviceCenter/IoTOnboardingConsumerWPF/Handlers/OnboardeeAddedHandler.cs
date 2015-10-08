﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Onboarding;

namespace IoTOnboardingConsumerWPF.Handlers
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
