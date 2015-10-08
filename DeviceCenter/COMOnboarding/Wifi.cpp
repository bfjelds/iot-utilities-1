#include "stdafx.h"
#include "Onboarding_h.h"
#include "Wifi.h"

HRESULT Wifi::GetSSID(BSTR *ssid)
{
    if (!ssid)
    {
        return E_POINTER;
    }

    *ssid = m_Ssid;

    return S_OK;
}

HRESULT Wifi::GetSecurity(INT16 *security)
{
    if (!security)
    {
        return E_POINTER;
    }

    // GetSecurity is only valid for a remote wifi
    if (m_IsLocal)
    {
        return E_ILLEGAL_METHOD_CALL;
    }

    *security = m_Security;

    return S_OK;
}

HRESULT Wifi::IsLocalWifi(BOOL *ret)
{
    if (!ret)
    {
        return E_POINTER;
    }

    *ret = m_IsLocal;

    return S_OK;
}

HRESULT Wifi::IsSecurityEnabled(BOOL *ret)
{
    if (!ret)
    {
        return E_POINTER;
    }

    *ret = m_SecurityEnabled;

    return S_OK;
}

HRESULT Wifi::GetAuthAlg(INT32 *auth)
{
    if (!auth)
    {
        return E_POINTER;
    }

    // This method call is not legal if this COM object is not 
    // representing a native wifi
    if (!m_IsLocal)
    {
        return E_ILLEGAL_METHOD_CALL;
    }

    *auth = m_AuthAlg;

    return S_OK;
}

HRESULT Wifi::GetCipherAlg(INT32 *cipher)
{
    if (!cipher)
    {
        return E_POINTER;
    }

    // This method call is not legal if this COM object is not 
    // representing a secure native wifi
    if (!m_IsLocal)
    {
        return E_ILLEGAL_METHOD_CALL;
    }

    *cipher = m_CipherAlg;

    return S_OK;
}
