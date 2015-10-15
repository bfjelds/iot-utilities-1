// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files:
#include <windows.h>
#include <wtypes.h>
#include <OleAuto.h>
#include <Wlanapi.h>
#include <strsafe.h>
#include <stdio.h>
#include <assert.h>

#include <initguid.h>

// alljoyn
#include <alljoyn_c\AjAPI.h>
#include <alljoyn_c\Init.h>
#include <alljoyn_c\BusAttachment.h>
#include <alljoyn_c\AboutData.h>
#include <alljoyn_c\AboutDataListener.h>
#include <alljoyn_c\AboutListener.h>
#include <alljoyn_c\AboutObj.h>
#include <alljoyn_c\MsgArg.h>
#include <alljoyn_c\AboutObjectDescription.h>

// COM
#include <wrl.h>
#include <wrl\wrappers\corewrappers.h>

#include <string>
#include <vector>
#include <set>
#include <mutex>
#include <map>
#include <memory>

#define SAFE_FREE(x) { if(x != NULL) { free(x); x = NULL; }}

#define TRACEERR(x) {char msg[256]; sprintf_s(msg, _countof(msg), "Error in function [%s], hr=0x%x\n", __FUNCTION__, x); OutputDebugStringA(msg);}
#define CHECK_STATUS(x) { status = x; if (status != ER_OK) { TRACEERR(status); goto Cleanup; } }
#define CHKHR(x) { hr = x; if(FAILED(hr)) { TRACEERR(hr); goto Cleanup; } }

#define CHECK_ERROR(x) { dError = x; if(dError != ERROR_SUCCESS) { hr = HRESULT_FROM_WIN32(x); CHKHR(hr); } }
#define ReturnHRESULTFromQStatus(x) { if(x == ER_OK) { return S_OK; } else {return E_FAIL; } }

#define METHOD_CALL_TIMEOUT 10000

#define ONBOARDING_OBJECT_PATH "/Onboarding"
#define ONBOARDING_INTERFACE_NAME "org.alljoyn.Onboarding"

#define TRACE(x) OutputDebugString(x)

// Error code
#define E_WLAN_INTERFACE_NOT_AVALIABLE 0x80077001
#define E_WLAN_INTERFACE_NOT_CONNECTED 0x80077002

using namespace std;
using namespace Microsoft::WRL;
using namespace Microsoft::WRL::Wrappers;
