#pragma once

// alljoyn callbacks
// TBD: Not using so far, marking for removal
void AJ_CALL found_advertised_name(const void* context, const char* name, alljoyn_transportmask transport, const char* namePrefix);
void AJ_CALL name_owner_changed(const void* context, const char* busName, const char* previousOwner, const char* newOwner);

void AJ_CALL on_announce(_In_ const void* context, _In_ PCSTR name, _In_ uint16_t version, _In_ alljoyn_sessionport port, _In_ alljoyn_msgarg objectDescriptionArg, _In_ const alljoyn_msgarg aboutDataArg);
void AJ_CALL on_authentication_complete(const void* context, const char* authMechanism, const char* peerName, QCC_BOOL success);
void AJ_CALL on_session_lost(_In_ const void* context, _In_ alljoyn_sessionid sessionId, _In_ alljoyn_sessionlostreason reason);
QCC_BOOL AJ_CALL on_request_credentials(const void* context, const char* authMechanism, const char* authPeer, uint16_t authCount, const char* userName, uint16_t credMask, alljoyn_credentials credentials);

// wifi callbacks
VOID WINAPI wlan_notification_callback(PWLAN_NOTIFICATION_DATA data, PVOID context);