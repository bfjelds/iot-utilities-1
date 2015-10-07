#include "stdafx.h"
#include "Onboarding_h.h"
#include "WifiList.h"
#include "Wifi.h"

HRESULT WifiList::Size(unsigned int *size)
{
    if (!size)
    {
        return E_POINTER;
    }

    auto lock = m_Lock.Lock();

    *size = m_InternalList.size();

    return S_OK;
}

HRESULT WifiList::AddItem(BSTR ssid, INT16 security)
{
    auto lock = m_Lock.Lock();

    m_InternalList.push_back(Make<Wifi>(ssid, security));

    return S_OK;
}

HRESULT WifiList::AddItemEx(BSTR ssid, BOOL securityEnabled, INT32 authAlg, INT32 cipherAlg)
{
    auto lock = m_Lock.Lock();

    m_InternalList.push_back(Make<Wifi>(ssid, securityEnabled, authAlg, cipherAlg));

    return S_OK;
}

HRESULT WifiList::GetItem(unsigned int index, IWifi **item)
{
    if (!item)
    {
        return E_POINTER;
    }

    auto lock = m_Lock.Lock();

    if (index >= m_InternalList.size())
    {
        return E_BOUNDS;
    }

    ComPtr<IWifi> value = m_InternalList.at(index);
    *item = value.Detach();

    return S_OK;
}
