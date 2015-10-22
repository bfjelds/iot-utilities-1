using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.WlanAPIs
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
                    _client._nativeHandle,
                    _nativeInterfaceInfo.interfaceGuid,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero),
                "WlanScan"
                );
        }

        public List<WlanInterop.WlanAvailableNetwork> GetAvailableNetworkList()
        {
            IntPtr availNetListPtr = IntPtr.Zero;
            var networkList = new List<WlanInterop.WlanAvailableNetwork>();

            try
            {
                Util.ThrowIfFail(
                    WlanInterop.WlanGetAvailableNetworkList(
                        _client._nativeHandle,
                        _nativeInterfaceInfo.interfaceGuid,
                        WlanInterop.WlanGetAvailableNetworkFlags.IncludeAllAdhocProfiles,
                        IntPtr.Zero,
                        out availNetListPtr),
                    "WlanGetAvailableNetworkList"
                    );

                var availNetListHeader = (WlanInterop.WlanAvailableNetworkList)Marshal.PtrToStructure(
                    availNetListPtr, 
                    typeof(WlanInterop.WlanAvailableNetworkList));

                long availNetListIt = availNetListPtr.ToInt64() + Marshal.SizeOf(typeof(WlanInterop.WlanAvailableNetworkList));
                
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
            WlanInterop.WlanConnectionFlags flags,
            string password)
        {
            var connectionParams = new WlanInterop.WlanConnectionParameters();
            connectionParams.wlanConnectionMode = connectionMode;
            var ssid = network.dot11Ssid;
            connectionParams.dot11SsidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ssid));
            Marshal.StructureToPtr(ssid, connectionParams.dot11SsidPtr, false);
            connectionParams.dot11BssType = bssType;
            connectionParams.flags = 0;
            connectionParams.profile = Util.MakeProfileString(
                Util.GetStringForSSID(ssid),
                network.dot11DefaultAuthAlgorithm,
                network.dot11DefaultCipherAlgorithm,
                password
                );
            Connect(connectionParams);
            Marshal.DestroyStructure(connectionParams.dot11SsidPtr, ssid.GetType());
            Marshal.FreeHGlobal(connectionParams.dot11SsidPtr);
        }

        protected void Connect(WlanInterop.WlanConnectionParameters connectionParams)
        {
            Util.ThrowIfFail(
                WlanInterop.WlanConnect(_client._nativeHandle, _nativeInterfaceInfo.interfaceGuid, ref connectionParams, IntPtr.Zero),
                "WlanConnect"
                );
        }

        public Guid GUID
        {
            get
            {
                return _nativeInterfaceInfo.interfaceGuid;
            }
        }

        private WlanClient _client;
        private WlanInterop.WlanInterfaceInfo _nativeInterfaceInfo;
    }
}
