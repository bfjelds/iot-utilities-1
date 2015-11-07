// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace WlanAPIs
{
    public delegate void AcmNotificationHandler(string profileName, int notificationCode, WlanInterop.WlanReasonCode reasonCode);

    public class WlanClient
    {
        public event AcmNotificationHandler OnAcmNotification;

        public WlanClient()
        {
            uint negotiatedVersion;
            Interfaces = new List<WlanInterface>();

            // open handle
            Util.ThrowIfFail(
                WlanInterop.WlanOpenHandle(WlanInterop.WlanApiVersion2, IntPtr.Zero, out negotiatedVersion, out NativeHandle),
                "WlanOpenHandle"
            );

            WlanInterop.WlanNotificationSource prevSrc;
            _wlanNotificationCallback = new WlanInterop.WlanNotificationCallbackDelegate(OnWlanNotification);

            // register notification
            Util.ThrowIfFail(
                WlanInterop.WlanRegisterNotification(
                    NativeHandle,
                    WlanInterop.WlanNotificationSource.All,
                    false,
                    _wlanNotificationCallback,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    out prevSrc),
                "WlanRegisterNotification"
                );

            IntPtr nativeInterfaceList = IntPtr.Zero;
            try
            {
                // enum interfaces
                Util.ThrowIfFail(
                    WlanInterop.WlanEnumInterfaces(NativeHandle, IntPtr.Zero, out nativeInterfaceList),
                    "WlanEnumInterfaces"
                    );

                this.Interfaces = ParseNativeWlanInterfaceList(nativeInterfaceList);
            }
            finally
            {
                WlanInterop.WlanFreeMemory(nativeInterfaceList);
            }
        }

        public List<WlanInterface> Interfaces { get; }

        ~WlanClient()
        {
            WlanInterop.WlanCloseHandle(NativeHandle, IntPtr.Zero);
        }

        private List<WlanInterface> ParseNativeWlanInterfaceList(IntPtr nativeInterfaceList)
        {
            var wlanInterfaces = new List<WlanInterface>();
            var header = (WlanInterop.WlanInterfaceInfoList)Marshal.PtrToStructure(
                nativeInterfaceList,
                typeof(WlanInterop.WlanInterfaceInfoList));

            var listIterator = nativeInterfaceList.ToInt64() + Marshal.SizeOf(header);

            var currentIfaceGuids = new List<Guid>();

            for (var i = 0; i < header.numberOfItems; ++i)
            {
                var nativeInterfaceInfo = (WlanInterop.WlanInterfaceInfo)Marshal.PtrToStructure(
                    new IntPtr(listIterator),
                    typeof(WlanInterop.WlanInterfaceInfo)
                    );

                listIterator += Marshal.SizeOf(nativeInterfaceInfo);
                wlanInterfaces.Add(new WlanInterface(this, nativeInterfaceInfo));
            }

            return wlanInterfaces;
        }

        private WlanInterop.WlanConnectionNotificationData? ParseWlanConnectionNotification(ref WlanInterop.WlanNotificationData notifyData)
        {
            var expectedSize = Marshal.SizeOf(typeof(WlanInterop.WlanConnectionNotificationData));

            if (notifyData.dataSize < expectedSize)
            {
                return null;
            }

            var connNotifyData =
                (WlanInterop.WlanConnectionNotificationData)
                Marshal.PtrToStructure(notifyData.dataPtr, typeof(WlanInterop.WlanConnectionNotificationData));

            return connNotifyData;
        }

        private void OnWlanNotification(ref WlanInterop.WlanNotificationData notifyData, IntPtr context)
        {
            var connNotifyData = ParseWlanConnectionNotification(ref notifyData);
            var reason = string.Empty;
            var reasonCode = uint.MaxValue;
            var profileName = string.Empty;

            if (connNotifyData != null)
            {
                reasonCode = (uint)connNotifyData.Value.wlanReasonCode;
                reason = Enum.GetName(typeof(WlanInterop.WlanReasonCode), connNotifyData.Value.wlanReasonCode);
                profileName = connNotifyData.Value.profileName;
            }

            if (notifyData.notificationSource == WlanInterop.WlanNotificationSource.ACM)
            {
                if(!string.IsNullOrEmpty(profileName))
                {
                    var source = Enum.GetName(typeof(WlanInterop.WlanNotificationSource), notifyData.notificationSource);
                    var notification = Enum.GetName(typeof(WlanInterop.WlanNotificationCodeAcm), notifyData.notificationCode);

                    Util.Info("*** {0} notification [{1}] [{2}] [{3}]({4},{5})",
                        source,
                        profileName,
                        notification,
                        reason,
                        notifyData.notificationCode,
                        reasonCode);
                }

                if (profileName == Util.AthensWlanProfileName)
                {
                    HandleAcmNotification(notifyData, connNotifyData);
                }

                OnAcmNotification?.Invoke(profileName, notifyData.notificationCode, (WlanInterop.WlanReasonCode)reasonCode);
            }
        }

        private void HandleAcmNotification(WlanInterop.WlanNotificationData notifyData, WlanInterop.WlanConnectionNotificationData? connNotifyData)
        {
            switch ((WlanInterop.WlanNotificationCodeAcm)notifyData.notificationCode)
            {
                case WlanInterop.WlanNotificationCodeAcm.ConnectionComplete:
                    {
                        if (connNotifyData != null && connNotifyData.Value.wlanReasonCode == WlanInterop.WlanReasonCode.Success)
                        {
                            _connectDoneEvent.Set();
                            Util.Info("Connection Complete [{0}]", _isConnectAttemptSuccess);
                        }
                        else
                        {
                            // never get here
                            _isConnectAttemptSuccess = false;
                            _connectDoneEvent.Set();
                        }
                    }
                    break;
                case WlanInterop.WlanNotificationCodeAcm.ConnectionAttemptFail:
                    {
                        _isConnectAttemptSuccess = false;
                    }
                    break;
            }
        }

        public bool WaitConnectComplete()
        {
            _connectDoneEvent.WaitOne();
            return _isConnectAttemptSuccess;
        }

        internal IntPtr NativeHandle;
        private readonly WlanInterop.WlanNotificationCallbackDelegate _wlanNotificationCallback;
        private readonly AutoResetEvent _connectDoneEvent = new AutoResetEvent(false);
        internal bool _isConnectAttemptSuccess;
    }
}
