#pragma once

class WifiList : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IWifiList>
{
    //
    // IWifiList
    //
    HRESULT STDMETHODCALLTYPE Size(unsigned int *size) override;
    HRESULT STDMETHODCALLTYPE AddItem(BSTR ssid, INT16 security) override;
    HRESULT STDMETHODCALLTYPE AddItemEx(BSTR Ssid, BOOL securityEnabled, INT32 authAlg, INT32 cipherAlg) override;
    HRESULT STDMETHODCALLTYPE GetItem(unsigned int index, IWifi **item) override;

public:
    WifiList()
    {
        TRACE(L"Wifi list created\n");
    }
    ~WifiList()
    {
        m_InternalList.clear();

        TRACE(L"Wifi list destroyed\n");
    }
private:
    vector<ComPtr<IWifi>> m_InternalList;
    CriticalSection       m_Lock;
};

CoCreatableClass(WifiList);
