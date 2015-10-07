#pragma once

enum AJ_ONBOARDING_STATE
{
    AJ_ONBOARDING_STATE_NOT_CONFIGURED = 0,
    AJ_ONBOARDING_STATE_CONFIGURED_NOT_VALIDATED,
    AJ_ONBOARDING_STATE_CONFIGURED_VALIDATING,
    AJ_ONBOARDING_STATE_CONFIGURED_VALIDATED,
    AJ_ONBOARDING_STATE_CONFIGURED_ERROR,
    AJ_ONBOARDING_STATE_CONFIGURED_RETRY
};

class OnboardingConsumer : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IOnboardingConsumer>
{
    friend void AJ_CALL on_session_lost(_In_ const void* context, _In_ alljoyn_sessionid sessionId, _In_ alljoyn_sessionlostreason reason);
    
    //
    // IOnboardingConsumer
    //
    virtual HRESULT STDMETHODCALLTYPE GetDisplayName(BSTR *name) override;
    // Alljoyn method mapping
    virtual HRESULT STDMETHODCALLTYPE JoinSession() override;
    virtual HRESULT STDMETHODCALLTYPE GetState(INT16 *value) override;
    virtual HRESULT STDMETHODCALLTYPE GetLastError(INT16 *value1, BSTR *value2) override;
    virtual HRESULT STDMETHODCALLTYPE GetScanInfo(IWifiList **list) override;
    virtual HRESULT STDMETHODCALLTYPE ConfigWifi( BSTR ssid, BSTR password, INT16 authType, INT16 *ret) override;
    virtual HRESULT STDMETHODCALLTYPE Connect() override;
    virtual HRESULT STDMETHODCALLTYPE Offboard() override;

public:
    OnboardingConsumer(PCSTR uniqueName, PCSTR displayName, PCSTR objectPath, alljoyn_sessionport port, alljoyn_busattachment bus);
    OnboardingConsumer() { TRACE(L"Consumer created\n"); }
    ~OnboardingConsumer();
    
private:
    HRESULT SetSessionState(bool state)
    {
        auto lock = m_SessionLock.Lock();
        m_SessionJoined = state;

        return S_OK;
    }

private:

    bool   m_SessionJoined;
    string m_ServiceObjectPath;
    string m_UniqueName;
    string m_DisplayName;

    alljoyn_proxybusobject  m_ProxyBusObject;
    alljoyn_sessionlistener m_SessionListener;
    alljoyn_sessionport     m_Port;
    alljoyn_sessionid       m_SessionId;
    alljoyn_busattachment   m_Bus;

    CriticalSection         m_SessionLock;
};

// Helper to create an OnboardingConsumer COM Object
HRESULT CreateAJConsumer(PCSTR uniqueName, PCSTR displayName, PCSTR objectPath, alljoyn_sessionport port, alljoyn_busattachment bus, IOnboardingConsumer **ppObj);

// This does the magic of actually creating the coclass for us
CoCreatableClass(OnboardingConsumer);
