// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
            var availNetListPtr = IntPtr.Zero;
            var networkList = new List<WlanInterop.WlanAvailableNetwork>();

            try
            {
                Util.ThrowIfFail(
                    WlanInterop.WlanGetAvailableNetworkList(
                        _client.NativeHandle,
                        _nativeInterfaceInfo.interfaceGuid,
                        WlanInterop.WlanGetAvailableNetworkFlags.IncludeAllAdhocProfiles,
                        IntPtr.Zero,
                        out availNetListPtr),
                    "WlanGetAvailableNetworkList"
                    );

                var availNetListHeader = (WlanInterop.WlanAvailableNetworkList)Marshal.PtrToStructure(
                    availNetListPtr, 
                    typeof(WlanInterop.WlanAvailableNetworkList));

                var availNetListIt = availNetListPtr.ToInt64() + Marshal.SizeOf(typeof(WlanInterop.WlanAvailableNetworkList));
                
                for (int i = 0; i < availNetListHeader.numberOfItems; ++i)
                {
                    var network = (WlanInterop.WlanAvailableNetwork)Marshal.PtrToStructure(
                        new IntPtr(availNetListIt), 
                        typeof(WlanInterop.WlanAvailableNetwork));
                    networkList.Add(network);
                    availNetListIt += Marshal.SizeOf(typeof(WlanInterop.WlanAvailableNetwork));
                }
            }
            finally
            {
                WlanInterop.WlanFreeMemory(availNetListPtr);
            }

            return networkList;
        }

        public void Connect(
            WlanInterop.WlanConnectionMode connectionMode,
            WlanInterop.Dot11BssType bssType,
            WlanInterop.WlanAvailableNetwork network,
            string password)
        {
            var connectionParams = new WlanInterop.WlanConnectionParameters {wlanConnectionMode = connectionMode};
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

        protected void Connect(WlanInterop.WlanConnectionParameters connectionParams)
        {
            Util.ThrowIfFail(
                WlanInterop.WlanConnect(_client.NativeHandle, _nativeInterfaceInfo.interfaceGuid, ref connectionParams, IntPtr.Zero),
                "WlanConnect"
                );
        }

        public Guid Guid => _nativeInterfaceInfo.interfaceGuid;

        private readonly WlanClient _client;
        private WlanInterop.WlanInterfaceInfo _nativeInterfaceInfo;
    }
}
