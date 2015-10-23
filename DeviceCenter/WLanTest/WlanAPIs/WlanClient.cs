using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceCenter.WlanAPIs
{
    public class WlanClient
    {
        public WlanClient()
        {
            _interfaces = new List<WlanInterface>();

            // open handle
            Util.ThrowIfFail(
                WlanInterop.WlanOpenHandle(WlanInterop.WLAN_API_VERSION_2_0, IntPtr.Zero, out _negotiatedVersion, out _nativeHandle),
                "WlanOpenHandle"
            );

            WlanInterop.WlanNotificationSource prevSrc;
            _wlanNotificationCallback = new WlanInterop.WlanNotificationCallbackDelegate(OnWlanNotification);

            // register notification
            Util.ThrowIfFail(
                WlanInterop.WlanRegisterNotification(
                    _nativeHandle,
                    WlanInterop.WlanNotificationSource.All,
                    false,
                    _wlanNotificationCallback,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    out prevSrc),
                "WlanRegisterNotification"
                );

            // enum interfaces
            IntPtr ifaceList;
            Util.ThrowIfFail(
                WlanInterop.WlanEnumInterfaces(_nativeHandle, IntPtr.Zero, out ifaceList),
                "WlanEnumInterfaces"
                );

            try
            {
                var header = (WlanInterop.WlanInterfaceInfoList)Marshal.PtrToStructure(ifaceList, typeof(WlanInterop.WlanInterfaceInfoList));
                Int64 listIterator = ifaceList.ToInt64() + Marshal.SizeOf(header);

                var currentIfaceGuids = new List<Guid>();
                for (int i = 0; i < header.numberOfItems; ++i)
                {
                    WlanInterop.WlanInterfaceInfo info =
                        (WlanInterop.WlanInterfaceInfo)Marshal.PtrToStructure(new IntPtr(listIterator), typeof(WlanInterop.WlanInterfaceInfo));
                    listIterator += Marshal.SizeOf(info);
                    _interfaces.Add(new WlanInterface(this, info));
                }
            }
            finally
            {
                WlanInterop.WlanFreeMemory(ifaceList);
            }
        }

        ~WlanClient()
        {
            WlanInterop.WlanCloseHandle(_nativeHandle, IntPtr.Zero);
        }

        public List<WlanInterface> Interfaces
        {
            get { return _interfaces; }
        }

        private WlanInterop.WlanConnectionNotificationData? ParseWlanConnectionNotification(ref WlanInterop.WlanNotificationData notifyData)
        {
            int expectedSize = Marshal.SizeOf(typeof(WlanInterop.WlanConnectionNotificationData));
            if (notifyData.dataSize < expectedSize)
            {
                return null;
            }

            var connNotifyData =
                (WlanInterop.WlanConnectionNotificationData)
                Marshal.PtrToStructure(notifyData.dataPtr, typeof(WlanInterop.WlanConnectionNotificationData));
            if (connNotifyData.wlanReasonCode == WlanInterop.WlanReasonCode.Success)
            {
                var profileXmlPtr = new IntPtr(
                    notifyData.dataPtr.ToInt64() +
                    Marshal.OffsetOf(typeof(WlanInterop.WlanConnectionNotificationData), "profileXml").ToInt64());
                connNotifyData.profileXml = Marshal.PtrToStringUni(profileXmlPtr);
            }
            return connNotifyData;
        }

        private void OnWlanNotification(ref WlanInterop.WlanNotificationData notifyData, IntPtr context)
        {
            var connNotifyData = ParseWlanConnectionNotification(ref notifyData);
            string source = Enum.GetName(typeof(WlanInterop.WlanNotificationSource), notifyData.notificationSource);
            string notification = Enum.GetName(typeof(WlanInterop.WlanNotificationCodeAcm), notifyData.notificationCode);
            string reason = string.Empty;
            uint reasonCode = 0;
            if (connNotifyData != null)
            {
                reasonCode = (uint)connNotifyData.Value.wlanReasonCode;
                reason = Enum.GetName(typeof(WlanInterop.WlanReasonCode), connNotifyData.Value.wlanReasonCode);
            }

            Debug.WriteLine(string.Format("{0} [{1}] [{2}]({3},{4})", source, notification, reason, notifyData.notificationCode, reasonCode));

            if (notifyData.notificationSource == WlanInterop.WlanNotificationSource.ACM)
            {
                HandleACMNotification(notifyData, connNotifyData);
            }
        }

        private void HandleACMNotification(WlanInterop.WlanNotificationData notifyData, WlanInterop.WlanConnectionNotificationData? connNotifyData)
        {
            switch ((WlanInterop.WlanNotificationCodeAcm)notifyData.notificationCode)
            {
                case WlanInterop.WlanNotificationCodeAcm.ConnectionComplete:
                    {
                        if (connNotifyData.Value.wlanReasonCode == WlanInterop.WlanReasonCode.Success)
                        {
                            _connectDoneEvent.Set();
                            Debug.WriteLine(string.Format("ACM Connected [{0}]", _isConnectAttemptSuccess));
                        }
                        else
                        {
                            // never get here
                            _isConnectAttemptSuccess = false;
                            Debug.Fail("debug");
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

        internal IntPtr _nativeHandle;
        private uint _negotiatedVersion;
        private List<WlanInterface> _interfaces;
        private WlanInterop.WlanNotificationCallbackDelegate _wlanNotificationCallback;
        private AutoResetEvent _connectDoneEvent = new AutoResetEvent(false);
        private bool _isConnectAttemptSuccess = true;
    }
}
