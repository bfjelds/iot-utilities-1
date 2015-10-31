// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WlanAPIs
{
    public class WlanInterface
    {
        internal WlanInterface(WlanClient client, WlanInterop.WlanInterfaceInfo interfaceInfo)
        {
            _client = client;
            _nativeInterfaceInfo = interfaceInfo;
        }

        public void Scan()
        {
            Util.ThrowIfFail(
                WlanInterop.WlanScan(
                    _client.NativeHandle,
                    _nativeInterfaceInfo.interfaceGuid,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero),
                "WlanScan"
                );
        }

        public List<WlanInterop.WlanAvailableNetwork> GetAvailableNetworkList()
        {
            var nativeNetworkList = IntPtr.Zero;
            var networkList = new List<WlanInterop.WlanAvailableNetwork>();

            try
            {
                Util.ThrowIfFail(
                    WlanInterop.WlanGetAvailableNetworkList(
                        _client.NativeHandle,
                        _nativeInterfaceInfo.interfaceGuid,
                        WlanInterop.WlanGetAvailableNetworkFlags.IncludeAllAdhocProfiles,
                        IntPtr.Zero,
                        out nativeNetworkList),
                    "WlanGetAvailableNetworkList"
                    );

                networkList = ParseNativeWlanAvaliableNetwork(nativeNetworkList);
            }
            finally
            {
                WlanInterop.WlanFreeMemory(nativeNetworkList);
            }

            return networkList;
        }

        public void Connect(
            WlanInterop.WlanConnectionMode connectionMode,
            WlanInterop.Dot11BssType bssType,
            WlanInterop.WlanAvailableNetwork network,
            string password)
        {
            this._client._isConnectAttemptSuccess = true;

            var connectionParams = new WlanInterop.WlanConnectionParameters { wlanConnectionMode = connectionMode };
            var ssid = network.dot11Ssid;
            connectionParams.dot11SsidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ssid));
            Marshal.StructureToPtr(ssid, connectionParams.dot11SsidPtr, false);
            connectionParams.dot11BssType = bssType;
            connectionParams.flags = 0;
            connectionParams.profile = Util.MakeProfileString(
                Util.GetStringForSsid(ssid),
                network.dot11DefaultAuthAlgorithm,
                network.dot11DefaultCipherAlgorithm,
                password
                );

            Connect(connectionParams);

            Marshal.DestroyStructure(connectionParams.dot11SsidPtr, ssid.GetType());
            Marshal.FreeHGlobal(connectionParams.dot11SsidPtr);
        }

        public void Disconnect()
        {
            Util.ThrowIfFail(
                WlanInterop.WlanDisconnect(
                    _client.NativeHandle,
                    ref _nativeInterfaceInfo.interfaceGuid,
                    IntPtr.Zero),
                "Disconnect"
                );
        }

        public override string ToString()
        {
            return $"Wlan interface [{_nativeInterfaceInfo.interfaceDescription}] - [{_nativeInterfaceInfo.isState}]";
        }

        public WlanInterop.WlanConnectionAttributes CurrentConnection
        {
            get
            {
                const uint WlanIntfOpcode_CurrentConnection = 7;
                int valueSize;
                IntPtr valuePtr;
                uint opcodeValueType;

                Util.ThrowIfFail(
                    WlanInterop.WlanQueryInterface(
                        _client.NativeHandle, 
                        _nativeInterfaceInfo.interfaceGuid,
                        WlanIntfOpcode_CurrentConnection, 
                        IntPtr.Zero, 
                        out valueSize, 
                        out valuePtr, 
                        out opcodeValueType), 
                    "CurrentConnection");
                try
                {
                    return (WlanInterop.WlanConnectionAttributes)Marshal.PtrToStructure(valuePtr, 
                        typeof(WlanInterop.WlanConnectionAttributes));
                }
                finally
                {
                    WlanInterop.WlanFreeMemory(valuePtr);
                }
            }
        }

        protected void Connect(WlanInterop.WlanConnectionParameters connectionParams)
        {
            Util.ThrowIfFail(
                WlanInterop.WlanConnect(_client.NativeHandle, _nativeInterfaceInfo.interfaceGuid, ref connectionParams, IntPtr.Zero),
                "WlanConnect"
                );
        }

        private static List<WlanInterop.WlanAvailableNetwork> ParseNativeWlanAvaliableNetwork(IntPtr nativeNetworkList)
        {
            var networkList = new List<WlanInterop.WlanAvailableNetwork>();
            var avaliableNLHeader = (WlanInterop.WlanAvailableNetworkList)Marshal.PtrToStructure(
                                nativeNetworkList,
                                typeof(WlanInterop.WlanAvailableNetworkList));

            var availNetListIt = nativeNetworkList.ToInt64() + Marshal.SizeOf(typeof(WlanInterop.WlanAvailableNetworkList));

            for (int i = 0; i < avaliableNLHeader.numberOfItems; ++i)
            {
                var network = (WlanInterop.WlanAvailableNetwork)Marshal.PtrToStructure(
                    new IntPtr(availNetListIt),
                    typeof(WlanInterop.WlanAvailableNetwork));

                networkList.Add(network);
                availNetListIt += Marshal.SizeOf(typeof(WlanInterop.WlanAvailableNetwork));
            }

            return networkList;
        }

        public Guid Guid => _nativeInterfaceInfo.interfaceGuid;

        private readonly WlanClient _client;
        private WlanInterop.WlanInterfaceInfo _nativeInterfaceInfo;
    }
}
