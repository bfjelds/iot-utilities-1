#include "stdafx.h"
#include "Onboarding_h.h"
#include "Wifi.h"

HRESULT Wifi::GetSSID(BSTR *ssid)
{
    if (!ssid)
    {
        return E_POINTER;
    }

    // Going to managed code the marshaler will copy the content of this BSTR to a
    // managed string and free the memory allocated by SysAllocString
    // Note that is not right pass the member m_Ssid directly since it would be
    // deallocated by the managed side
    *ssid = SysAllocString(m_Ssid);

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
