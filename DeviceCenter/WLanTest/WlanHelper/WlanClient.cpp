// This is the main DLL file.

#include "stdafx.h"

#include "WlanClient.h"
using namespace WlanHelper;

WlanClient::WlanClient() :
    _nativeHandle(nullptr),
    _negotiatedVersion(0)
{
    HANDLE handle = (HANDLE)(_nativeHandle.ToPointer());
    WlanOpenHandle(WLAN_API_VERSION_2_0, nullptr, &_negotiatedVersion, &handle);
}