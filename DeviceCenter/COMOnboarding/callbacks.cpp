#include "stdafx.h"

#include "callbacks.h"
#include "util.h"
#include "Onboarding_h.h"
#include "OnboardingConsumer.h"
#include "OnboardingManager.h"

void AJ_CALL found_advertised_name(const void* context, const char* name, alljoyn_transportmask transport, const char* namePrefix)
{
    UNREFERENCED_PARAMETER(context);
    UNREFERENCED_PARAMETER(name);
    UNREFERENCED_PARAMETER(transport);
    UNREFERENCED_PARAMETER(namePrefix);
}

void AJ_CALL name_owner_changed(const void* context, const char* busName, const char* previousOwner, const char* newOwner)
{
    UNREFERENCED_PARAMETER(context);
    UNREFERENCED_PARAMETER(busName);
    UNREFERENCED_PARAMETER(previousOwner);
    UNREFERENCED_PARAMETER(newOwner);
}

//
// Authentication callback
// When we try to connect to a secure remote object
// this callback is executed and we can set the password
// for the connection.
// Hardcoding this for now.
// 
QCC_BOOL AJ_CALL on_request_credentials(const void* context, const char* authMechanism, const char* authPeer, uint16_t authCount,
    const char* userName, uint16_t credMask, alljoyn_credentials credentials)
{
    UNREFERENCED_PARAMETER(context);
    UNREFERENCED_PARAMETER(authMechanism);
    UNREFERENCED_PARAMETER(authPeer);
    UNREFERENCED_PARAMETER(authCount);
    UNREFERENCED_PARAMETER(userName);
    UNREFERENCED_PARAMETER(credMask);

    // TBD: Hardcoding this for now
    alljoyn_credentials_setpassword(credentials, "testing123");

    return QCC_TRUE;
}

//
// Authentication complete callback
// Called with the authentication status.
// Not used now but can be useful to notify 
// a potential error to the UI.
//
void AJ_CALL on_authentication_complete(const void* context, const char* authMechanism, const char* peerName, QCC_BOOL success)
{
    UNREFERENCED_PARAMETER(context);
    UNREFERENCED_PARAMETER(authMechanism);
    UNREFERENCED_PARAMETER(peerName);
    UNREFERENCED_PARAMETER(success);
}

//
// About announcement callback
// Called when alljoyn finds a new about producer in the network
// Context is the OnboardingManager instance that started the
// about listener responsible for this callback
//
void AJ_CALL on_announce(
    _In_ const void* context,
    _In_ PCSTR name,
    _In_ uint16_t version,
    _In_ alljoyn_sessionport port,
    _In_ alljoyn_msgarg objectDescriptionArg,
    _In_ const alljoyn_msgarg aboutDataArg)
{
    UNREFERENCED_PARAMETER(version);

    if (!context)
    {
        return; 
    }

    alljoyn_aboutobjectdescription objectDescription = alljoyn_aboutobjectdescription_create_full(objectDescriptionArg);

    if (alljoyn_aboutobjectdescription_hasinterface(objectDescription, ONBOARDING_INTERFACE_NAME) && alljoyn_aboutobjectdescription_haspath(objectDescription, ONBOARDING_OBJECT_PATH))
    {
        QStatus status = ER_OK;
        HRESULT hr = S_OK;
        INT16 state;
        PCSTR path = NULL;
        PWSTR wName = NULL;
        PWSTR wPath = NULL;
        PSTR appName = NULL;
        alljoyn_aboutdata aboutData = NULL;
        string displayName;

        alljoyn_busattachment bus;
        ComPtr<IOnboardingConsumer> consumer;

        aboutData = alljoyn_aboutdata_create(NULL);

        // Creating display name from about app name and alljoyn unique name
        CHECK_STATUS(alljoyn_aboutdata_createfrommsgarg(aboutData, aboutDataArg, NULL));

        CHECK_STATUS(alljoyn_aboutdata_getappname(aboutData, &appName, NULL));

        // Display name is composed of the appName (from about data) and the alljoyn
        // bus object unique name.
        // Example: IoTCoreDefaultApp(:Ots1r-4Q3)
        displayName.assign(appName);
        displayName.append("(");
        displayName.append(name);
        displayName.append(")");

        // TBD: Is there an cleaner way to do this?
        OnboardingManager *manager = const_cast<OnboardingManager *>(static_cast<const OnboardingManager *>(context));

        alljoyn_aboutobjectdescription_getinterfacepaths(objectDescription, "org.alljoyn.Onboarding", &path, 1);

        wName = ConvertCStrToWStr(name);
        wPath = ConvertCStrToWStr(path);

        if (!wName || !wPath)
        {
            CHECK_STATUS(ER_OUT_OF_MEMORY);
        }

        //// Create consumer
        bus = manager->GetBus();

        assert(bus != NULL);

        CreateAJConsumer(name, displayName.c_str(), path, port, bus, consumer.GetAddressOf());

        if (!consumer)
        {
            CHECK_STATUS(ER_FAIL);
        }

        hr = consumer->JoinSession();

        if (SUCCEEDED(hr))
        {
            hr = consumer->GetState(&state);
        }

        //Only report consumers that were not onboarded yet
        if (SUCCEEDED(hr) && state == AJ_ONBOARDING_STATE_NOT_CONFIGURED)
        {
            manager->AddConsumer(wName, consumer.Get());
            manager->Announce(consumer.Get());
        }

    Cleanup:
        SAFE_FREE(wName);
        SAFE_FREE(wPath);

        alljoyn_aboutdata_destroy(aboutData);
    }

    alljoyn_aboutobjectdescription_destroy(objectDescription);
}

//
// Session lost callback
// Called when a consumer lost its alljoyn session,
// the context is the AJOnboardingConsumer that lost
// the session
//
void AJ_CALL on_session_lost(_In_ const void* context, _In_ alljoyn_sessionid sessionId, _In_ alljoyn_sessionlostreason reason)
{
    // TBD: Is there an cleaner way to do this?
    OnboardingConsumer *consumer = const_cast<OnboardingConsumer *>(static_cast<const OnboardingConsumer *>(context));

    consumer->SetSessionState(false);
}

// Don't know what would be a good size for this
#define WLAN_REASON_STR_BUFFER_SIZE 2048

//
// Native WiFi notification callback
// Called when a notification is issued by the wifi api.
// This could be a succesfull connection, connection failed,
// enumeration completed, etc
// TBD: This needs to be more complete (cover more cases)
//
VOID WINAPI wlan_notification_callback(PWLAN_NOTIFICATION_DATA data, PVOID context)
{
    assert(context != NULL);

    OnboardingManager *manager = static_cast<OnboardingManager *>(context);

    if (data->NotificationCode == wlan_notification_acm_connection_complete)
    {
        PWLAN_CONNECTION_NOTIFICATION_DATA info = (PWLAN_CONNECTION_NOTIFICATION_DATA)data->pData;

        BSTR bstrReasonString;
        WCHAR reason[WLAN_REASON_STR_BUFFER_SIZE];

        if (ERROR_SUCCESS == WlanReasonCodeToString(info->wlanReasonCode, WLAN_REASON_STR_BUFFER_SIZE, reason, NULL))
        {
            TRACE(reason);

            bstrReasonString = SysAllocString(reason);
        }
        else
        {
            TRACE(L"Unable to get Wlan reason string.\n");

            bstrReasonString = SysAllocString(L"Undefined");
        }

        manager->WiFiCallbackHandler(info->wlanReasonCode, bstrReasonString);
    }
}
