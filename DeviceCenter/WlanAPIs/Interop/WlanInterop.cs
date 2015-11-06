// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WlanAPIs
{
    public class WlanInterop
    {
        #region p/invoke
        [DllImport("wlanapi.dll")]
        public static extern uint WlanOpenHandle(
            [In] UInt32 clientVersion,
            [In, Out] IntPtr pReserved,
            [Out] out UInt32 negotiatedVersion,
            [Out] out IntPtr clientHandle);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanEnumInterfaces(
            [In] IntPtr clientHandle,
            [In, Out] IntPtr pReserved,
            [Out] out IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanScan(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] IntPtr pDot11Ssid,
            [In] IntPtr pIeData,
            [In, Out] IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanGetAvailableNetworkList(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] WlanGetAvailableNetworkFlags flags,
            [In, Out] IntPtr reservedPtr,
            [Out] out IntPtr availableNetworkListPtr);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanConnect(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] ref WlanConnectionParameters connectionParameters,
            IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern void WlanFreeMemory(IntPtr pMemory);

        public delegate void WlanNotificationCallbackDelegate(ref WlanNotificationData notificationData, IntPtr context);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanRegisterNotification(
            [In] IntPtr clientHandle,
            [In] WlanNotificationSource notifSource,
            [In] bool ignoreDuplicate,
            [In] WlanNotificationCallbackDelegate funcCallback,
            [In] IntPtr callbackContext,
            [In] IntPtr reserved,
            [Out] out WlanNotificationSource prevNotifSource);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanReasonCodeToString(
            [In] WlanReasonCode reasonCode,
            [In] int bufferSize,
            [In, Out] StringBuilder stringBuffer,
            IntPtr pReserved
        );

        [DllImport("Wlanapi.dll")]
        public static extern uint WlanDisconnect(
            [In] IntPtr hClientHandle,
            [In] ref Guid pInterfaceGuid,
            [In, Out] IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanQueryInterface(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] uint opCode,
            [In, Out] IntPtr pReserved,
            [Out] out int dataSize,
            [Out] out IntPtr ppData,
            [Out] out uint wlanOpcodeValueType);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanCloseHandle(
            [In] IntPtr clientHandle,
            [In, Out] IntPtr pReserved);
        #endregion

        #region Enum
        public static uint WlanApiVersion2 = 0x00000002;

        public enum WlanInterfaceState
        {
            NotReady = 0,
            Connected = 1,
            AdHocNetworkFormed = 2,
            Disconnecting = 3,
            Disconnected = 4,
            Associating = 5,
            Discovering = 6,
            Authenticating = 7
        }

        [Flags]
        public enum WlanGetAvailableNetworkFlags
        {
            IncludeAllAdhocProfiles = 0x00000001,
            IncludeAllManualHiddenProfiles = 0x00000002
        }
        #endregion

        #region Structures
        [StructLayout(LayoutKind.Sequential)]
        internal struct WlanInterfaceInfoList
        {
            public uint numberOfItems;
            public uint index;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanInterfaceInfo
        {
            public Guid interfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string interfaceDescription;
            public WlanInterfaceState isState;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WlanAvailableNetworkList
        {
            public uint numberOfItems;
            public uint index;
        }

        [Flags]
        public enum WlanNotificationSource
        {
            None = 0,
            All = 0X0000FFFF,
            ACM = 0X00000008,
            MSM = 0X00000010,
            Security = 0X00000020,
            IHV = 0X00000040
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WlanNotificationData
        {
            public WlanNotificationSource notificationSource;
            public int notificationCode;
            public Guid interfaceGuid;
            public int dataSize;
            public IntPtr dataPtr;

            public object NotificationCode
            {
                get
                {
                    if (notificationSource == WlanNotificationSource.MSM)
                        return (WlanNotificationCodeMsm)notificationCode;
                    else if (notificationSource == WlanNotificationSource.ACM)
                        return (WlanNotificationCodeAcm)notificationCode;
                    else
                        return notificationCode;
                }

            }
        }

        public enum WlanNotificationCodeMsm
        {
            Associating = 1,
            Associated,
            Authenticating,
            Connected,
            RoamingStart,
            RoamingEnd,
            RadioStateChange,
            SignalQualityChange,
            Disassociating,
            Disconnected,
            PeerJoin,
            PeerLeave,
            AdapterRemoval,
            AdapterOperationModeChange
        }

        public enum WlanNotificationCodeAcm
        {
            AutoconfEnabled = 1,
            AutoconfDisabled,
            BackgroundScanEnabled,
            BackgroundScanDisabled,
            BssTypeChange,
            PowerSettingChange,
            ScanComplete,
            ScanFail,
            ConnectionStart,
            ConnectionComplete,
            ConnectionAttemptFail,
            FilterListChange,
            InterfaceArrival,
            InterfaceRemoval,
            ProfileChange,
            ProfileNameChange,
            ProfilesExhausted,
            NetworkNotAvailable,
            NetworkAvailable,
            Disconnecting,
            Disconnected,
            AdhocNetworkStateChange
        }

        public struct Dot11Ssid
        {
            public uint SsidLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Ssid;
        }

        public enum Dot11BssType
        {
            Infrastructure = 1,
            Independent = 2,
            Any = 3
        }

        public enum WlanReasonCode : uint
        {
            Success = 0
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanAvailableNetwork
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string profileName;
            public Dot11Ssid dot11Ssid;
            public Dot11BssType dot11BssType;
            public uint numberOfBssids;
            public bool networkConnectable;
            public WlanReasonCode wlanNotConnectableReason;
            private readonly uint numberOfPhyTypes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private readonly uint[] dot11PhyTypes;
            public bool morePhyTypes;
            public uint wlanSignalQuality;
            public bool securityEnabled;
            public uint dot11DefaultAuthAlgorithm;
            public uint dot11DefaultCipherAlgorithm;
            public uint flags;
            readonly uint reserved;

            public string SsidString => Util.SsidToString(dot11Ssid);

            public override string ToString()
            {
                return $"[{SsidString}] [connectable? - {networkConnectable}] [Quality - {wlanSignalQuality}] [Auth - {dot11DefaultAuthAlgorithm}] [Ciph - {dot11DefaultCipherAlgorithm}]";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WlanConnectionParameters
        {
            public WlanConnectionMode wlanConnectionMode;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string profile;
            public IntPtr dot11SsidPtr;
            public IntPtr desiredBssidListPtr;
            public Dot11BssType dot11BssType;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanConnectionNotificationData
        {
            public WlanConnectionMode wlanConnectionMode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string profileName;
            public Dot11Ssid dot11Ssid;
            public Dot11BssType dot11BssType;
            public bool securityEnabled;
            public WlanReasonCode wlanReasonCode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
            public string profileXml;
        }

        public enum WlanConnectionMode
        {
            Profile = 0,
            TemporaryProfile,
            DiscoverySecure,
            DiscoveryUnsecure,
            Auto,
            Invalid
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanConnectionAttributes
        {
            public WlanInterfaceState isState;
            public WlanConnectionMode wlanConnectionMode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string profileName;
            public WlanAssociationAttributes wlanAssociationAttributes;

            public override string ToString()
            {
                return $"[{wlanAssociationAttributes.SsidString}] [state - {isState}] [mode - {wlanConnectionMode}] [{profileName}]";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WlanAssociationAttributes
        {
            public Dot11Ssid dot11Ssid;
            public Dot11BssType dot11BssType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] dot11Bssid;
            public uint dot11PhyType;
            public uint dot11PhyIndex;
            public uint wlanSignalQuality;
            public uint rxRate;
            public uint txRate;

            public string SsidString => Util.SsidToString(dot11Ssid);
        }
        #endregion
    }
}
