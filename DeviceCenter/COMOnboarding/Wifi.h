
#pragma once

enum AJ_ONBOARDING_AUTH
{
    AJ_ONBOARDING_AUTH_WPA2_AUTO = -3,
    AJ_ONBOARDING_AUTH_WPA_AUTO,
    AJ_ONBOARDING_AUTH_Any,
    AJ_ONBOARDING_AUTH_Open,
    AJ_ONBOARDING_AUTH_WEP,
    AJ_ONBOARDING_AUTH_WPA_TKIP,
    AJ_ONBOARDING_AUTH_WPA_CCMP,
    AJ_ONBOARDING_AUTH_WPA2_TKIP,
    AJ_ONBOARDING_AUTH_WPA2_CCMP,
    AJ_ONBOARDING_AUTH_WPS
};

class Wifi : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IWifi>
{
    //
    // IWifi
    //
    HRESULT STDMETHODCALLTYPE GetSSID(BSTR *ssid) override;
    HRESULT STDMETHODCALLTYPE GetSecurity(INT16 *security) override;
    HRESULT STDMETHODCALLTYPE IsLocalWifi(BOOL *ret) override;
    HRESULT STDMETHODCALLTYPE IsSecurityEnabled(BOOL *ret) override;
    HRESULT STDMETHODCALLTYPE GetAuthAlg(INT32 *auth) override;
    HRESULT STDMETHODCALLTYPE GetCipherAlg(INT32 *cipher) override;

public:
    Wifi() 
    {
        TRACE(L"Wifi created\n");
    }
    Wifi(BSTR ssid, INT16 security)
    {
        m_IsLocal = FALSE;
        m_Ssid = ssid;
        m_Security = security;

        TRACE(L"Wifi created\n");
    }
    Wifi(BSTR ssid, boolean securityEnabled, INT32 authAlg, INT32 cipherAlg)
    {
        m_IsLocal = TRUE;
        m_Ssid = ssid;
        m_SecurityEnabled = securityEnabled;
        m_AuthAlg = (DOT11_AUTH_ALGORITHM)authAlg;
        m_CipherAlg = (DOT11_CIPHER_ALGORITHM)cipherAlg;

        TRACE(L"Wifi created\n");
    }
    ~Wifi() 
    {
        SysFreeString(m_Ssid);

        TRACE(L"Wifi destroyed\n");
    }

private:
    BSTR m_Ssid;
    // This flag indicates if the wifi network
    // is local or remote.
    // Local networks are scanned with Windows native
    // wifi APIs and use the fields m_AuthAlg and m_CipherAlg
    // to remember how to authenticate.
    // Remote networks are scanned by the onboardee and sent to the
    // consumer when GetScanInfo method is called. It uses a member of
    // AJ_ONBOARDING_AUTH enumeration to indicate authentication
    // settings.
    boolean m_IsLocal;

    // Remote wifi settings
    INT16 m_Security;

    // Local wifi settings
    boolean m_SecurityEnabled;
    DOT11_AUTH_ALGORITHM m_AuthAlg;
    DOT11_CIPHER_ALGORITHM m_CipherAlg;
};

CoCreatableClass(Wifi);
