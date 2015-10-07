#include "stdafx.h"
#include "callbacks.h"
#include "Onboarding_h.h"
#include "OnboardingManager.h"
#include "OnboardingConsumer.h"
#include "WifiList.h"
#include "util.h"

alljoyn_sessionlistener_callbacks session_callbacks = { on_session_lost, NULL, NULL };

HRESULT CreateAJConsumer(PCSTR uniqueName, PCSTR displayName, PCSTR objectPath, alljoyn_sessionport port, alljoyn_busattachment bus, IOnboardingConsumer **ppObj)
{
    ComPtr<IOnboardingConsumer> object = Make<OnboardingConsumer>(uniqueName, displayName, objectPath, port, bus);

    if (!object)
    {
        return E_OUTOFMEMORY;
    }

    *ppObj = object.Detach();

    return S_OK;
}

OnboardingConsumer::OnboardingConsumer(PCSTR uniqueName, PCSTR displayName, PCSTR objectPath, alljoyn_sessionport port, alljoyn_busattachment bus)
{
    assert(bus != NULL);

    m_Bus = bus;
    m_SessionJoined = false;
    m_ServiceObjectPath.assign(objectPath);
    m_UniqueName.assign(uniqueName);
    m_DisplayName.assign(displayName);
    m_Port = port;

    m_ProxyBusObject = NULL;
    m_SessionListener = NULL;

    TRACE(L"Consumer created\n");
}

OnboardingConsumer::~OnboardingConsumer()
{
    // TBD: Destroying the ProxyBusObject is enough to destroy the session, if there is one?
    if (m_ProxyBusObject)
    {
        alljoyn_proxybusobject_destroy(m_ProxyBusObject);
        m_ProxyBusObject = NULL;
    }

    if (m_SessionListener)
    {
        alljoyn_sessionlistener_destroy(m_SessionListener);
        m_SessionListener = NULL;
    }

    TRACE(L"Consumer destroyed\n");
}

HRESULT STDMETHODCALLTYPE OnboardingConsumer::GetDisplayName(BSTR *name)
{
    if (!name)
    {
        return E_INVALIDARG;
    }

    PWSTR wName = ConvertCStrToWStr(m_DisplayName.c_str());

    BSTR bstrName = SysAllocString(wName);

    // if bstrName is NULL should we throw an expection in managed code? (return an HR other then S_OK)
    *name = bstrName;

    SAFE_FREE(wName);

    return S_OK;
}

HRESULT STDMETHODCALLTYPE OnboardingConsumer::JoinSession()
{
    auto lock = m_SessionLock.Lock();

    QStatus status;

    m_SessionListener = alljoyn_sessionlistener_create(&session_callbacks, this);

    alljoyn_sessionopts opts = alljoyn_sessionopts_create(ALLJOYN_TRAFFIC_TYPE_MESSAGES, QCC_FALSE, ALLJOYN_PROXIMITY_ANY, ALLJOYN_TRANSPORT_ANY);

    alljoyn_busattachment_enableconcurrentcallbacks(m_Bus);

    CHECK_STATUS(alljoyn_busattachment_joinsession(m_Bus, m_UniqueName.c_str(), m_Port, m_SessionListener, &m_SessionId, opts));

    alljoyn_interfacedescription description = alljoyn_busattachment_getinterface(m_Bus, "org.alljoyn.Onboarding");
    
    if (alljoyn_interfacedescription_issecure(description))
    {
        m_ProxyBusObject = alljoyn_proxybusobject_create_secure(m_Bus, m_UniqueName.c_str(), m_ServiceObjectPath.c_str(), m_SessionId);

        CHECK_STATUS(alljoyn_proxybusobject_secureconnection(m_ProxyBusObject, QCC_FALSE));
    }
    else
    {
        m_ProxyBusObject = alljoyn_proxybusobject_create(m_Bus, m_UniqueName.c_str(), m_ServiceObjectPath.c_str(), m_SessionId);
    }
    

    CHECK_STATUS(alljoyn_proxybusobject_addinterface(m_ProxyBusObject, description));

    m_SessionJoined = true;

Cleanup:
    alljoyn_sessionopts_destroy(opts);

    if (status != ER_OK)
    {
        if (m_SessionListener)
        {
            alljoyn_sessionlistener_destroy(m_SessionListener);
            m_SessionListener = NULL;
        }

        if (m_ProxyBusObject)
        {
            alljoyn_proxybusobject_destroy(m_ProxyBusObject);
            m_ProxyBusObject = NULL;
        }
    }

    ReturnHRESULTFromQStatus(status);
}

HRESULT STDMETHODCALLTYPE OnboardingConsumer::GetState(INT16 *value)
{
    QStatus status = ER_OK;
    alljoyn_msgarg propertyValue = NULL;

    propertyValue = alljoyn_msgarg_create();

    if (m_SessionJoined)
    {
        CHECK_STATUS(alljoyn_proxybusobject_getproperty(m_ProxyBusObject, ONBOARDING_INTERFACE_NAME, "State", propertyValue));

        CHECK_STATUS(alljoyn_msgarg_get(propertyValue, "n", value));
    }
    // Else throw exception?
Cleanup:

    if (propertyValue != NULL)
    {
        alljoyn_msgarg_destroy(propertyValue);
    }

    ReturnHRESULTFromQStatus(status);
}

HRESULT STDMETHODCALLTYPE OnboardingConsumer::GetLastError(INT16 *value1, BSTR *value2)
{
    QStatus status = ER_OK;
    alljoyn_msgarg propertyValue = NULL;

    propertyValue = alljoyn_msgarg_create();

    if (m_SessionJoined)
    {
        PSTR aErrorMessage = NULL;
        PWSTR wErrorMessage = NULL;

        alljoyn_msgarg arg1;
        alljoyn_msgarg arg2;

        CHECK_STATUS(alljoyn_proxybusobject_getproperty(m_ProxyBusObject, ONBOARDING_INTERFACE_NAME, "LastError", propertyValue));

        CHECK_STATUS(alljoyn_msgarg_get(propertyValue, "(**)", &arg1, &arg2));

        CHECK_STATUS(alljoyn_msgarg_get(arg1, "n", value1));

        CHECK_STATUS(alljoyn_msgarg_get(arg2, "s", &aErrorMessage));

        wErrorMessage = ConvertCStrToWStr(aErrorMessage);

        if (!wErrorMessage)
        {
            CHECK_STATUS(ER_OUT_OF_MEMORY);
        }

        *value2 = SysAllocString(wErrorMessage);

        SAFE_FREE(wErrorMessage);
    }
    // Else throw exception?
Cleanup:

    if (propertyValue != NULL)
    {
        alljoyn_msgarg_destroy(propertyValue);
    }

    ReturnHRESULTFromQStatus(status);
}

HRESULT STDMETHODCALLTYPE OnboardingConsumer::GetScanInfo(IWifiList **ppList)
{
    QStatus status = ER_OK;
    HRESULT hr = S_OK;
    alljoyn_message reply = NULL;
    ComPtr<IWifiList> pList = NULL;
    set<string> SsidMap;

    if (m_SessionJoined)
    {
        reply = alljoyn_message_create(m_Bus);

        CHECK_STATUS(alljoyn_proxybusobject_methodcall(m_ProxyBusObject, "org.alljoyn.Onboarding", "GetScanInfo", NULL, 0, reply, METHOD_CALL_TIMEOUT, 0));

        UINT16 age;
        alljoyn_msgarg arg0 = alljoyn_message_getarg(reply, 0);
        CHECK_STATUS(alljoyn_msgarg_get_uint16(arg0, &age));

        size_t elementCount = 0;
        alljoyn_msgarg arrayContents = NULL;
        CHECK_STATUS(alljoyn_msgarg_get(alljoyn_message_getarg(reply, 1), "a(sn)", &elementCount, &arrayContents));

        if (arrayContents != nullptr)
        {
            pList = Make<WifiList>();

            for (size_t i = 0; i < elementCount; i++)
            {
                INT16 security;
                // alljoyn works with ANSI strings
                char *aSsid;
                PWSTR wSsid;
                BSTR bstrSsid;

                alljoyn_msgarg argument1;
                alljoyn_msgarg argument2;
                CHECK_STATUS(alljoyn_msgarg_get(alljoyn_msgarg_array_element(arrayContents, i), "(**)", &argument1, &argument2));

                CHECK_STATUS(alljoyn_msgarg_get(argument1, "s", &aSsid));
                CHECK_STATUS(alljoyn_msgarg_get(argument2, "n", &security));

                wSsid = ConvertCStrToWStr(aSsid);

                if (!wSsid)
                {
                    CHECK_STATUS(ER_OUT_OF_MEMORY);
                }

                if (SsidMap.find(string(aSsid)) == SsidMap.end())
                {
                    bstrSsid = SysAllocString(wSsid);

                    pList->AddItem(bstrSsid, security);

                    // Avoid duplicated Ssids in the list
                    SsidMap.insert(string(aSsid));
                }
                SAFE_FREE(wSsid);
            }
        }
    }
    else
    {
        // TBD: If the session is not joined should we throw an exception (return a HResult other then S_OK) or just set the pointer to NULL?
        *ppList = NULL;
    }
Cleanup:
    // As far as I know this should free memory for everything related to it (message arguments, etc)
    alljoyn_message_destroy(reply);
    
    if (status == ER_OK)
    {
        *ppList = pList.Detach();
    }
    else
    {
        // This will decrease the ref count for this COM object,
        // and because we hold the only reference it will be destroyed
        pList = nullptr;
    }

    ReturnHRESULTFromQStatus(status);
}

HRESULT STDMETHODCALLTYPE OnboardingConsumer::ConfigWifi(BSTR ssid, BSTR password, INT16 authType, INT16 *ret)
{
    QStatus status = ER_OK;
    PSTR aSsid = NULL;
    PSTR aPassword = NULL;
    alljoyn_msgarg inputs = NULL;
    alljoyn_message reply = NULL;

    if (m_SessionJoined)
    {
        aSsid = ConvertWStrToCStr(ssid);
        aPassword = ConvertWStrToCStr(password);

        if (!aSsid)
        {
            CHECK_STATUS(ER_OUT_OF_MEMORY);
        }

        // If authType is not zero there should be
        // a password string
        if (authType != 0 && !aPassword)
        {
            CHECK_STATUS(ER_INVALID_ADDRESS);
        }

        if (!aPassword)
        {
            aPassword = "";
        }

        size_t numArgs = 3;
        inputs = alljoyn_msgarg_array_create(numArgs);
        reply = alljoyn_message_create(m_Bus);

        CHECK_STATUS(alljoyn_msgarg_array_set(inputs, &numArgs, "ssn", aSsid, aPassword, authType));
        CHECK_STATUS(alljoyn_proxybusobject_methodcall(m_ProxyBusObject, "org.alljoyn.Onboarding", "ConfigureWifi", inputs, numArgs, reply, METHOD_CALL_TIMEOUT, 0));

        alljoyn_msgarg arg0 = alljoyn_message_getarg(reply, 0);

        CHECK_STATUS(alljoyn_msgarg_get_int16(arg0, ret));
    }
    // Else throw exception?
Cleanup:
    SAFE_FREE(aSsid);
    SAFE_FREE(aPassword);

    if (inputs != NULL)
    {
        alljoyn_msgarg_destroy(inputs);
    }

    if (reply != NULL)
    {
        alljoyn_message_destroy(reply);
    }

    ReturnHRESULTFromQStatus(status);
}

HRESULT STDMETHODCALLTYPE OnboardingConsumer::Connect()
{
    QStatus status = ER_OK;
    alljoyn_msgarg inputs = NULL;
    alljoyn_message reply = NULL;
    if (m_SessionJoined)
    {
        reply = alljoyn_message_create(m_Bus);
        inputs = alljoyn_msgarg_create();

        // TBD: Maybe I should not forward an error from this method to the UI
        // Because the remote device is changing Wifi networks the session might be destroyed
        // before this method call can return
        CHECK_STATUS(alljoyn_proxybusobject_methodcall(m_ProxyBusObject, "org.alljoyn.Onboarding", "Connect", inputs, 0, reply, METHOD_CALL_TIMEOUT, 0));
    }
    // Else throw exception?

Cleanup:
    alljoyn_msgarg_destroy(inputs);
    alljoyn_message_destroy(reply);

    ReturnHRESULTFromQStatus(status);
}

HRESULT STDMETHODCALLTYPE OnboardingConsumer::Offboard()
{
    return E_NOTIMPL;
}
