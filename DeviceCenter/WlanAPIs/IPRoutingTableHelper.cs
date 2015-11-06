// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WlanAPIs
{
    /// <summary>
    /// Helper class for IP routing table
    /// </summary>
    public class IPRoutingTableHelper
    {
        private const string RouteAddLocalEntryArgument = "add $destIP mask 255.255.255.255 0.0.0.0 if $ifIndex";
        private const string RouteDeleteEntryArgument = "delete $destIP if $ifIndex";
        private const int GETIP_RETRY_NUMBER = 20;
        private const int GETIP_DELAY_DURATION = 500;

        static public IPRoutingTableHelper CreateByNicGuid(Guid interfaceGuid)
        {
            if (interfaceGuid == Guid.Empty)
            {
                return null;
            }

            var newInstance = new IPRoutingTableHelper();
            newInstance._networkInterfaceGuid = interfaceGuid;
            
            return newInstance;
        }

        public bool AddLocalEntryIfNeeded(string ipAddress)
        {
            NetworkInterface networkInterface = GetNetworkInterface();
            IPAddress ipv4 = Util.GetIpv4(networkInterface);

            // already got a 192.168.173.x address, no need to update routing table
            if (Util.IsDhcpipAddress(ipv4))
            {
                return true;
            }

            // routing entry already exists
            if(IsRoutingEntryExist(IPAddress.Parse(ipAddress)))
            {
                return true;
            }

            // eg. route add 192.168.173.1 mask 255.255.255.255 0.0.0.0 if 9
            Util.Info("IPRoutingTableHelper: Adding local IP [{0}]", ipAddress);
            var argument = RouteAddLocalEntryArgument;
            argument = argument.Replace("$destIP", ipAddress);
            argument = argument.Replace("$ifIndex", Util.GetIndex(networkInterface).ToString());
            return Util.RunRouteElevated(argument);
        }

        public bool DeleteEntryIfNeeded(string ipAddress)
        {
            var networkInterface = GetNetworkInterface();

            // routing entry doesn't exists
            if (!IsRoutingEntryExist(IPAddress.Parse(ipAddress)))
            {
                return true;
            }

            Util.Info("IPRoutingTableHelper: Deleting entry for IP [{0}]", ipAddress);
            var argument = RouteDeleteEntryArgument;
            argument = argument.Replace("$destIP", ipAddress);
            argument = argument.Replace("$ifIndex", Util.GetIndex(networkInterface).ToString());
            return Util.RunRouteElevated(argument);
        }

        private NetworkInterface GetNetworkInterface()
        {
            NetworkInterface networkInterface = null;

            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var interf in interfaces.Where(interf => Guid.Parse(interf.Id) == _networkInterfaceGuid))
            {
                Util.Info("Found name [{0}] for guid [{1}]", interf.Name, _networkInterfaceGuid.ToString());
                networkInterface = interf;
            }

            if (networkInterface == null)
            {
                Util.Error("Can't Find name for guid [{0}]", _networkInterfaceGuid.ToString());
            }

            return networkInterface;
        }

        private IPHelperInterop.IPForwardTable ParseNativeIPForwardTable(IntPtr ipFwdTable)
        {
            var nativeIPTable = (IPHelperInterop.IPForwardTable)Marshal.PtrToStructure(
                                    ipFwdTable, typeof(IPHelperInterop.IPForwardTable));

            var nativeRows = new IPHelperInterop.IPFORWARDROW[nativeIPTable.Size];
            var rowPtr = new IntPtr(ipFwdTable.ToInt64() + Marshal.SizeOf(nativeIPTable.Size));
            for (int i = 0; i < nativeIPTable.Size; ++i)
            {
                nativeRows[i] = (IPHelperInterop.IPFORWARDROW)Marshal.PtrToStructure(rowPtr, typeof(IPHelperInterop.IPFORWARDROW));
                rowPtr = new IntPtr(rowPtr.ToInt64() + Marshal.SizeOf(typeof(IPHelperInterop.IPFORWARDROW)));
            }
            nativeIPTable.Table = nativeRows;

            return nativeIPTable;
        }

        private bool IsRoutingEntryExist(IPAddress ipAddress)
        {
            bool isExist = false;
            var ipTablePtr = IntPtr.Zero;
            int size = 0;

            var result = IPHelperInterop.GetIpForwardTable(ipTablePtr, ref size, true);
            ipTablePtr = Marshal.AllocHGlobal(size);

            try
            {
                Util.ThrowIfFail(
                    IPHelperInterop.GetIpForwardTable(ipTablePtr, ref size, true),
                    "GetIpForwardTable");

                var uintIpAddress = BitConverter.ToUInt32(ipAddress.GetAddressBytes(), 0);

                var forwardTable = ParseNativeIPForwardTable(ipTablePtr);
                foreach (var row in forwardTable.Table)
                {
                    if (row.dwForwardDest == uintIpAddress)
                    {
                        isExist = true;
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                Util.Error("IsRoutingEntryExist: " + ex.ToString());
            }
            finally
            {
                Marshal.FreeHGlobal(ipTablePtr);
            }

            Util.Info("[{0}] exist in ip routing table? [{1}]", ipAddress.ToString(), isExist);
            return isExist;
        }

        private IPRoutingTableHelper()
        {
        }

        private Guid _networkInterfaceGuid;
    }
}
