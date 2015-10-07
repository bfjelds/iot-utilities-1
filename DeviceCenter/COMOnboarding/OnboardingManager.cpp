#include "stdafx.h"
#include "callbacks.h"
#include "Onboarding_h.h"
#include "OnboardingManager.h"
#include "OnboardingConsumer.h"
#include "util.h"
#include "WifiList.h"
#include "Wifi.h"

// Onboarding interface introspection XML
PCSTR c_OnboardingIntrospectionXml = "<interface name=\"org.alljoyn.Onboarding\">"
"  <method name=\"ConfigureWifi\">"
"    <arg name=\"SSID\" type=\"s\" direction=\"in\" />"
"    <arg name=\"passphrase\" type=\"s\" direction=\"in\" />"
"    <arg name=\"authType\" type=\"n\" direction=\"in\" />"
"    <arg name=\"status\" type=\"n\" direction=\"out\" />"
"  </method>"
"  <method name=\"Connect\">"
"    <annotation name=\"org.freedesktop.DBus.Method.NoReply\" value=\"true\" />"
"  </method>"
"  <signal name=\"ConnectionResult\">"
"    <arg name=\"result\" type=\"(ns)\" direction=\"out\" />"
"  </signal>"
"  <method name=\"GetScanInfo\">"
"    <arg name=\"age\" type=\"q\" direction=\"out\" />"
"    <arg name=\"scanList\" type=\"a(sn)\" direction=\"out\" />"
"  </method>"
"  <method name=\"Offboard\">"
"    <annotation name=\"org.freedesktop.DBus.Method.NoReply\" value=\"true\" />"
"  </method>"
"  <property name=\"LastError\" type=\"(ns)\" access=\"read\" />"
"  <property name=\"State\" type=\"n\" access=\"read\" />"
"  <property name=\"Version\" type=\"q\" access=\"read\" />"
"</interface>";

// Alljoyn callbacks
alljoyn_buslistener_callbacks callbacks = { NULL, NULL, &found_advertised_name, NULL, &name_owner_changed, NULL, NULL, NULL };
alljoyn_aboutlistener_callback about_callback = { on_announce };
alljoyn_authlistener_callbacks auth_callbacks = { on_request_credentials, NULL, NULL, on_authentication_complete };

HRESULT OnboardingManager::Init()    
{
    HRESULT hr = S_OK;
    
    CHKHR(_InitAllJoyn());
    CHKHR(_InitWifi());

Cleanup:
    return hr;
}

HRESULT OnboardingManager::_InitWifi()
{
    HRESULT hr = S_OK;
    DWORD dError = 0;

    // Native wifi init
    m_hWlan = INVALID_HANDLE_VALUE;
    PWLAN_INTERFACE_INFO_LIST pWlanInterfaceInfoList = nullptr;

    DWORD dwWlanVersion = 0;
    CHKHR(WlanOpenHandle(WLAN_API_VERSION_2_0, nullptr, &dwWlanVersion, &m_hWlan));

    CHECK_ERROR(WlanEnumInterfaces(m_hWlan, nullptr, &pWlanInterfaceInfoList));

    if (pWlanInterfaceInfoList->dwNumberOfItems < 1)
    {
        CHKHR(E_FAIL);
    }

    CHECK_ERROR(WlanScan(m_hWlan, &pWlanInterfaceInfoList->InterfaceInfo[0].InterfaceGuid, NULL, NULL, NULL));

    // Just pick the first adapter for now
    m_InterfaceGuid = pWlanInterfaceInfoList->InterfaceInfo[0].InterfaceGuid;

    // Register notification callback
    WlanRegisterNotification(m_hWlan, WLAN_NOTIFICATION_SOURCE_ALL, TRUE, wlan_notification_callback, this, NULL, NULL);
Cleanup:

    if (FAILED(hr))
    {
        WlanCloseHandle(m_hWlan, NULL);
    }

    WlanFreeMemory(pWlanInterfaceInfoList);

    return hr;
}

HRESULT OnboardingManager::_InitAllJoyn()
{
    HRESULT hr = S_OK;
    QStatus status = ER_OK;

    CHECK_STATUS(alljoyn_init());

    m_Bus = alljoyn_busattachment_create("myApp", QCC_TRUE);
    // check g_bus for NULL?

    // Add interface to the bus
    CHECK_STATUS(alljoyn_busattachment_createinterfacesfromxml(m_Bus, c_OnboardingIntrospectionXml));

    // start bus
    CHECK_STATUS(alljoyn_busattachment_start(m_Bus));

    m_AuthListener = alljoyn_authlistener_create(&auth_callbacks, NULL);

    CHECK_STATUS(alljoyn_busattachment_enablepeersecurity(m_Bus, "ALLJOYN_ECDHE_PSK", m_AuthListener, NULL, QCC_FALSE));

    // connect
    CHECK_STATUS(alljoyn_busattachment_connect(m_Bus, NULL));

    // check return?
    m_AboutListener = alljoyn_aboutlistener_create(&about_callback, this);
    alljoyn_busattachment_registeraboutlistener(m_Bus, m_AboutListener);

    PCSTR interfaces[] = { "org.alljoyn.Onboarding" };

    CHECK_STATUS(alljoyn_busattachment_whoimplements_interfaces(
        m_Bus,
        interfaces,
        _countof(interfaces)));

Cleanup:
    if (status != ER_OK)
    {
        if (m_Bus)
        {
            alljoyn_busattachment_destroy(m_Bus);
        }

        if (m_AuthListener)
        {
            alljoyn_authlistener_destroy(m_AuthListener);
        }
    }

    ReturnHRESULTFromQStatus(status);
}

HRESULT OnboardingManager::Shutdown()
{
    auto lock = m_Lock.Lock();
    QStatus status = ER_OK;
    DWORD dError = 0;
    HRESULT hr = S_OK;

    m_ConsumerMap.clear();

    alljoyn_authlistener_destroy(m_AuthListener);
    alljoyn_aboutlistener_destroy(m_AboutListener);
    alljoyn_busattachment_destroy(m_Bus);

    CHECK_ERROR(WlanCloseHandle(m_hWlan, NULL));

    CHECK_STATUS(alljoyn_shutdown());

    if (m_OnboardeeAdded)
    {
        m_OnboardeeAdded = nullptr;
    }

    if (m_WifiConnectionResult)
    {
        m_WifiConnectionResult = nullptr;
    }

Cleanup:
    // TBD: This is confusing, I have two sources of errors (QStatus and win32 error)
    ReturnHRESULTFromQStatus(status);
}

HRESULT OnboardingManager::SetOnboardeeAddedHandler(IOnboardeeAdded *handler)
{
    m_OnboardeeAdded = handler;

    return S_OK;
}

HRESULT OnboardingManager::SetWifiConnectionResultHandler(IWifiConnectionResult *handler)
{
    m_WifiConnectionResult = handler;

    return S_OK;
}

HRESULT OnboardingManager::Announce(IOnboardingConsumer *consumer)
{
    if (m_OnboardeeAdded)
    {
        m_OnboardeeAdded->Added(consumer);
    }

    return S_OK;
}

HRESULT OnboardingManager::WiFiCallbackHandler(int reasonCode, BSTR reasonStr)
{
    if (m_WifiConnectionResult)
    {
        m_WifiConnectionResult->ConnectionResult(reasonCode, reasonStr);
    }
    return S_OK;
}

HRESULT OnboardingManager::AddConsumer(PCWSTR UniqueName, IOnboardingConsumer *consumer)
{
    auto lock = m_Lock.Lock();

    auto result = m_ConsumerMap.insert(std::pair<wstring, ComPtr<IOnboardingConsumer>>(wstring(UniqueName), consumer));

    if (result.second)
    {
        return S_OK;
    }
    else
    {
        return E_FAIL;
    }
}

HRESULT OnboardingManager::RemoveConsumer(PCWSTR UniqueName)
{
    auto lock = m_Lock.Lock();

    m_ConsumerMap.erase(wstring(UniqueName));

    return S_OK;
}

HRESULT OnboardingManager::GetOnboardingNetworks(IWifiList **list)
{
    HRESULT hr = S_OK;
    DWORD dError = 0;
    PWLAN_AVAILABLE_NETWORK_LIST availableList = NULL;
    DWORD i, j;
    set<string> SSIDs;
    ComPtr<IWifiList> pList;

    if (!list)
    {
        return E_POINTER;
    }

    // Throw exception?
    if (m_InterfaceGuid == GUID_NULL)
    {
        *list = nullptr;

        return S_OK;
    }

    CHECK_ERROR(WlanGetAvailableNetworkList(m_hWlan, &m_InterfaceGuid, 0, NULL, &availableList));

    pList = Make<WifiList>();

    j = 0;
    for (i = 0;i < availableList->dwNumberOfItems; i++)
    {
        PUCHAR ssid = availableList->Network[i].dot11Ssid.ucSSID;
        string strSSID((PSTR)ssid);

        if (StartsWithAJ_((PSTR)ssid) && availableList->Network[i].bNetworkConnectable && SSIDs.find(strSSID) == SSIDs.end())
        {
            PWSTR uSsid = ConvertCStrToWStr((PSTR)ssid);

            BSTR bstrSsid = SysAllocString(uSsid);

            SAFE_FREE(uSsid);

            pList->AddItemEx(bstrSsid, availableList->Network[i].bSecurityEnabled, availableList->Network[i].dot11DefaultAuthAlgorithm, availableList->Network[i].dot11DefaultCipherAlgorithm);
            
            // Avoid duplicated SSIDs in the results
            SSIDs.insert(strSSID);

            j++;
        }
    }
Cleanup:

    WlanFreeMemory(availableList);

    if (FAILED(hr))
    {
        pList = nullptr;
    }
    else
    {
        *list = pList.Detach();
    }

    return hr;
}

HRESULT OnboardingManager::ConnectToOnboardingNetwork(IWifi *wifi, BSTR password)
{
    // TBD: This method needs major refactoring, right now it only connects to
    // secure wifi. I will do that as soon as possible
    HRESULT hr = E_FAIL;
    DWORD dError;
    WLAN_CONNECTION_PARAMETERS parameters;
    string output;
    LPCWSTR tempStr = L"<?xml version=\"1.0\" encoding=\"US-ASCII\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>SampleWPAPSK</name><SSIDConfig><SSID><name>%s</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>auto</connectionMode><autoSwitch>false</autoSwitch><MSM><security><authEncryption><authentication>%s</authentication><encryption>%s</encryption><useOneX>false</useOneX></authEncryption>%s</security></MSM></WLANProfile>";
    LPWSTR tempSecuritySect = L"<sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>%s</keyMaterial></sharedKey>";
    LPWSTR profileStr = NULL;
    LPWSTR securitySection = NULL;
    size_t securitySection_size;
    size_t profileStr_size;
    DOT11_CIPHER_ALGORITHM cipherAlg;
    DOT11_AUTH_ALGORITHM authAlg;
    BOOL securityEnabled;
    BOOL isLocalWifi;

    BSTR bstrSsid;

    if (!wifi)
    {
        CHKHR(E_INVALIDARG);
    }

    CHKHR(wifi->GetSSID(&bstrSsid));
    CHKHR(wifi->IsLocalWifi(&isLocalWifi));
    CHKHR(wifi->IsSecurityEnabled(&securityEnabled));

    if (!isLocalWifi)
    {
        CHKHR(E_INVALIDARG);
    }

    if (securityEnabled && !password)
    {
        CHKHR(E_INVALIDARG);
    }

    CHKHR(wifi->GetAuthAlg((PINT32)&authAlg));
    CHKHR(wifi->GetCipherAlg((PINT32)&cipherAlg));

    securitySection_size = 1 + wcslen(tempSecuritySect) + (password ? wcslen(password) : 0);
    profileStr_size = 1 + wcslen(tempStr) + SysStringLen(bstrSsid) + CIPHER_STRING_MAX_LENGTH + AUTH_STRING_MAX_LENGTH + securitySection_size;

    securitySection = (LPWSTR)malloc(securitySection_size*sizeof(WCHAR));
    profileStr = (PWSTR)malloc(profileStr_size*sizeof(WCHAR));

    if (!securitySection || !profileStr)
    {
        CHKHR(E_OUTOFMEMORY);
    }

    CHKHR(StringCchPrintf(securitySection, securitySection_size, tempSecuritySect, password));
    CHKHR(StringCchPrintf(profileStr, profileStr_size, tempStr, bstrSsid, AuthToString(authAlg), CipherToString(cipherAlg), securitySection));

    parameters.wlanConnectionMode = wlan_connection_mode_temporary_profile;
    parameters.strProfile = profileStr;
    parameters.pDot11Ssid = (PDOT11_SSID)malloc(sizeof(DOT11_SSID));

    strcpy_s((PSTR)parameters.pDot11Ssid->ucSSID, DOT11_SSID_MAX_LENGTH, ConvertWStrToCStr(bstrSsid));
    parameters.pDot11Ssid->uSSIDLength = strlen((PSTR)parameters.pDot11Ssid->ucSSID);
    parameters.pDesiredBssidList = NULL;
    parameters.dot11BssType = dot11_BSS_type_any;
    parameters.dwFlags = 0;

    //LPWSTR profile[20000];
    //CHECK_ERROR(WlanGetProfile(hWlan, &interf, L"AJ_Test", NULL, profile, 0, NULL));

    CHECK_ERROR(WlanConnect(m_hWlan, &m_InterfaceGuid, &parameters, NULL));

Cleanup:
    SAFE_FREE(securitySection);
    SAFE_FREE(profileStr);

    return hr;
}

HRESULT OnboardingManager::RestoreWifi()
{
    HRESULT hr = S_OK;
    DWORD dError = 0;

    // This will make windows connect to the top priority wifi
    // TBD: Need to think if this is the best approach
    CHECK_ERROR(WlanDisconnect(m_hWlan, &m_InterfaceGuid, NULL));

Cleanup:
    return hr;
}
