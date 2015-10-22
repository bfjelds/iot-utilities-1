using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WLanTest;

namespace WLanTest
{
    public class WlanClient
    {
        public WlanClient()
        {
            _interfaces = new List<WlanInterface>();

            WlanInterop.WlanOpenHandle(WlanInterop.WLAN_API_VERSION_2_0, IntPtr.Zero, out _negotiatedVersion, out _nativeHandle);

            WlanInterop.WlanNotificationSource prevSrc;
            _wlanNotificationCallback = new WlanInterop.WlanNotificationCallbackDelegate(OnWlanNotification);

            WlanInterop.WlanRegisterNotification(
                _nativeHandle,
                WlanInterop.WlanNotificationSource.All, 
                false,
                _wlanNotificationCallback, 
                IntPtr.Zero, 
                IntPtr.Zero, 
                out prevSrc);

            IntPtr ifaceList;
            WlanInterop.WlanEnumInterfaces(_nativeHandle, IntPtr.Zero, out ifaceList);
            try
            {
                WlanInterop.WlanInterfaceInfoList header =
                    (WlanInterop.WlanInterfaceInfoList)Marshal.PtrToStructure(ifaceList, typeof(WlanInterop.WlanInterfaceInfoList));
                Int64 listIterator = ifaceList.ToInt64() + Marshal.SizeOf(header);

                List<Guid> currentIfaceGuids = new List<Guid>();
                for (int i = 0; i < header.numberOfItems; ++i)
                {
                    WlanInterop.WlanInterfaceInfo info =
                        (WlanInterop.WlanInterfaceInfo)Marshal.PtrToStructure(new IntPtr(listIterator), typeof(WlanInterop.WlanInterfaceInfo));
                    listIterator += Marshal.SizeOf(info);
                    _interfaces.Add(new WlanInterface(this, info));
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                WlanInterop.WlanFreeMemory(ifaceList);
            }
        }

        ~WlanClient()
        {
            // WlanInterop.WlanCloseHandle(_nativeHandle, IntPtr.Zero);
        }

        public List<WlanInterface> Interfaces
        {
            get { return _interfaces; }
        }

        private WlanInterop.WlanConnectionNotificationData? ParseWlanConnectionNotification(ref WlanInterop.WlanNotificationData notifyData)
        {
            int expectedSize = Marshal.SizeOf(typeof(WlanInterop.WlanConnectionNotificationData));
            if (notifyData.dataSize < expectedSize)
                return null;

            var connNotifyData =
                (WlanInterop.WlanConnectionNotificationData)
                Marshal.PtrToStructure(notifyData.dataPtr, typeof(WlanInterop.WlanConnectionNotificationData));
            if (connNotifyData.wlanReasonCode == WlanInterop.WlanReasonCode.Success)
            {
                IntPtr profileXmlPtr = new IntPtr(
                    notifyData.dataPtr.ToInt64() +
                    Marshal.OffsetOf(typeof(WlanInterop.WlanConnectionNotificationData), "profileXml").ToInt64());
                connNotifyData.profileXml = Marshal.PtrToStringUni(profileXmlPtr);
            }
            return connNotifyData;
        }

        private void OnWlanNotification(ref WlanInterop.WlanNotificationData notifyData, IntPtr context)
        {
            // WlanInterface wlanIface = _interfaces.ContainsKey(notifyData.interfaceGuid) ? _interfaces[notifyData.interfaceGuid] : null;

            switch (notifyData.notificationSource)
            {
                case WlanInterop.WlanNotificationSource.ACM:
                    switch ((WlanInterop.WlanNotificationCodeAcm)notifyData.notificationCode)
                    {
                        case WlanInterop.WlanNotificationCodeAcm.ConnectionStart:
                        case WlanInterop.WlanNotificationCodeAcm.ConnectionComplete:
                        case WlanInterop.WlanNotificationCodeAcm.ConnectionAttemptFail:
                        case WlanInterop.WlanNotificationCodeAcm.Disconnecting:
                        case WlanInterop.WlanNotificationCodeAcm.Disconnected:
                            {
                                if((WlanInterop.WlanNotificationCodeAcm)notifyData.notificationCode == WlanInterop.WlanNotificationCodeAcm.ConnectionComplete)
                                {
                                    _connectDoneEvent.Set();
                                    Console.WriteLine("Connected");
                                }
                                WlanInterop.WlanConnectionNotificationData? connNotifyData = ParseWlanConnectionNotification(ref notifyData);

                                string notificationCode = Enum.GetName(typeof(WlanInterop.WlanNotificationCodeAcm), notifyData.notificationCode);
                                string rcStr = Enum.GetName(typeof(WlanInterop.WlanReasonCode), connNotifyData.Value.wlanReasonCode);
                                Console.WriteLine("ACM [{0}] [{1}]", notificationCode, rcStr);
                                /*if (connNotifyData.HasValue)
                                    if (wlanIface != null)
                                        wlanIface.OnWlanConnection(notifyData, connNotifyData.Value);
                                        */
                            }
                            break;
                        case WlanInterop.WlanNotificationCodeAcm.ScanFail:
                            {
                                Console.WriteLine("ACM [{0}] [{1}]", "ScanFail", "");
                                
                                int expectedSize = Marshal.SizeOf(typeof(uint));
                                if (notifyData.dataSize >= expectedSize)
                                {
                                    WlanInterop.WlanReasonCode reasonCode = (WlanInterop.WlanReasonCode)Marshal.ReadInt32(notifyData.dataPtr);
                                /*
                                if (wlanIface != null)
                                    wlanIface.OnWlanReason(notifyData, reasonCode);
                                    */
                                }
                            }
                            break;
                    }
                    break;
                case WlanInterop.WlanNotificationSource.MSM:
                    switch ((WlanInterop.WlanNotificationCodeMsm)notifyData.notificationCode)
                    {
                        case WlanInterop.WlanNotificationCodeMsm.Associating:
                        case WlanInterop.WlanNotificationCodeMsm.Associated:
                        case WlanInterop.WlanNotificationCodeMsm.Authenticating:
                        case WlanInterop.WlanNotificationCodeMsm.Connected:
                        case WlanInterop.WlanNotificationCodeMsm.RoamingStart:
                        case WlanInterop.WlanNotificationCodeMsm.RoamingEnd:
                        case WlanInterop.WlanNotificationCodeMsm.Disassociating:
                        case WlanInterop.WlanNotificationCodeMsm.Disconnected:
                        case WlanInterop.WlanNotificationCodeMsm.PeerJoin:
                        case WlanInterop.WlanNotificationCodeMsm.PeerLeave:
                        case WlanInterop.WlanNotificationCodeMsm.AdapterRemoval:
                            WlanInterop.WlanConnectionNotificationData? connNotifyData = ParseWlanConnectionNotification(ref notifyData);

                            string notificationCode = Enum.GetName(typeof(WlanInterop.WlanNotificationCodeAcm), notifyData.notificationCode);
                            string rcStr = Enum.GetName(typeof(WlanInterop.WlanReasonCode), connNotifyData.Value.wlanReasonCode);
                            Console.WriteLine("MSM [{0}] [{1}]", notificationCode, rcStr);

                            /*if (connNotifyData.HasValue)
                                if (wlanIface != null)
                                    wlanIface.OnWlanConnection(notifyData, connNotifyData.Value);*/
                            break;
                    }
                    break;
            }
        }

        public void WaitConnectComplete()
        {
            _connectDoneEvent.WaitOne();
        }

        internal IntPtr _nativeHandle;
        private uint _negotiatedVersion;
        private List<WlanInterface> _interfaces;
        private WlanInterop.WlanNotificationCallbackDelegate _wlanNotificationCallback;
        private AutoResetEvent _connectDoneEvent = new AutoResetEvent(false);
    }
}
