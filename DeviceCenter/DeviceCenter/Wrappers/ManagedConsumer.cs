using Onboarding;

namespace DeviceCenter.Wrappers
{
    class ManagedConsumer
    {
        private Onboarding.IOnboardingConsumer m_NativeConsumer;
        private string m_Name;

        public ManagedConsumer(Onboarding.IOnboardingConsumer nativeConsumer)
        {
            m_NativeConsumer = nativeConsumer;
            string bstr = m_NativeConsumer.GetDisplayName();

            m_Name = new string(bstr.ToCharArray());
        }

        public string Name
        {
            get
            { return m_Name; }
        }

        public IOnboardingConsumer NativeConsumer
        {
            get { return m_NativeConsumer; }
        }
    }
}
