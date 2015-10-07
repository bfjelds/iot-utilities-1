#pragma once

class OnboardingManager : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IOnboardingManager>
{
    friend void AJ_CALL on_announce(_In_ const void* context, _In_ PCSTR name, _In_ uint16_t version, _In_ alljoyn_sessionport port, _In_ alljoyn_msgarg objectDescriptionArg, _In_ const alljoyn_msgarg aboutDataArg);
    friend VOID WINAPI wlan_notification_callback(PWLAN_NOTIFICATION_DATA data, PVOID context);
public: 
    OnboardingManager() :
        m_InterfaceGuid(GUID_NULL),
        m_OnboardeeAdded(nullptr) 
    {
        TRACE(L"Manager created\n");
    }
    ~OnboardingManager()
    {
        //Shutdown();
        TRACE(L"Manager destroyed\n");
    }

    //
    // IOnboardingManager
    //
    virtual HRESULT STDMETHODCALLTYPE Init() override;
    virtual HRESULT STDMETHODCALLTYPE Shutdown() override;
    virtual HRESULT STDMETHODCALLTYPE SetOnboardeeAddedHandler(IOnboardeeAdded *handler) override;
    virtual HRESULT STDMETHODCALLTYPE SetWifiConnectionResultHandler(IWifiConnectionResult *handler) override;
    virtual HRESULT STDMETHODCALLTYPE GetOnboardingNetworks(IWifiList **list) override;
    virtual HRESULT STDMETHODCALLTYPE ConnectToOnboardingNetwork(IWifi *wifi, BSTR password) override;
    virtual HRESULT STDMETHODCALLTYPE RestoreWifi() override;

private:
    // Internal methods
    alljoyn_busattachment OnboardingManager::GetBus(void) { return m_Bus; }
    HRESULT STDMETHODCALLTYPE Announce(IOnboardingConsumer *consumer);
    HRESULT STDMETHODCALLTYPE WiFiCallbackHandler(int reasonCode, BSTR reasonStr);
    HRESULT STDMETHODCALLTYPE AddConsumer(PCWSTR UniqueName, IOnboardingConsumer *consumer);
    HRESULT STDMETHODCALLTYPE RemoveConsumer(PCWSTR UniqueName);

    HRESULT _InitWifi();
    HRESULT _InitAllJoyn();

    alljoyn_busattachment m_Bus;
    alljoyn_aboutlistener m_AboutListener;
    alljoyn_authlistener  m_AuthListener;

    HANDLE m_hWlan;
    GUID m_InterfaceGuid;

    map<wstring, ComPtr<IOnboardingConsumer>> m_ConsumerMap;
    CriticalSection                           m_Lock;
    ComPtr<IOnboardeeAdded>                   m_OnboardeeAdded;
    ComPtr<IWifiConnectionResult>             m_WifiConnectionResult;
};

CoCreatableClass(OnboardingManager);