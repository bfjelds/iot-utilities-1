// WlanHelper.h

#pragma once

using namespace System;

namespace WlanHelper {

	public ref class WlanClient
	{
    public:
        WlanClient();

    private:
        IntPtr _nativeHandle;
        UInt32 _negotiatedVersion;
	};
}
