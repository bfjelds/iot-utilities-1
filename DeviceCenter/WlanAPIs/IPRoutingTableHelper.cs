﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace WlanAPIs
{
    /// <summary>
    /// Helper class for retrieval and modification of local IPv4 routing table
    /// </summary>
    public class IPRoutingTableHelper
    {
        private const string RouteAddLocalEntryArgument = "add $destIP mask 255.255.255.255 0.0.0.0 if $ifIndex";
        private const string RouteDeleteEntryArgument = "delete $destIP if $ifIndex";

        /// <summary>
        /// Create a new instance based on the network interface Guid
        /// </summary>
        /// <param name="interfaceGuid"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Add a local(ON-LINK) routing entry to IP routing table if
        /// 1) The current IP Address is not "192.168.173.x"
        /// And 2) No entry exists for this IP
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public bool AddLocalEntryIfNeeded(string ipAddress)
        {
            NetworkInterface networkInterface = GetNetworkInterface();
            IPAddress ipv4 = Util.GetIpv4(networkInterface);

            // already got a 192.168.173.x address, no need to update routing table
            // ipv4 might be 255.255.255.255 at this moment if the connection is not ready
            // update routing table for this case
            if (Util.IsDhcpipAddress(ipv4))
            {
                return true;
            }

            // routing entry already exists
            if(DoesRoutingEntryExist(IPAddress.Parse(ipAddress)))
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

        /// <summary>
        /// Delete a routing entry to IP routing table if the entry exists for this IP
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public bool DeleteEntryIfNeeded(string ipAddress)
        {
            var networkInterface = GetNetworkInterface();

            // routing entry doesn't exists
            if (!DoesRoutingEntryExist(IPAddress.Parse(ipAddress)))
            {
                return true;
            }

            // eg. route delete 192.168.173.1
            Util.Info("IPRoutingTableHelper: Deleting entry for IP [{0}]", ipAddress);
            var argument = RouteDeleteEntryArgument;
            argument = argument.Replace("$destIP", ipAddress);
            argument = argument.Replace("$ifIndex", Util.GetIndex(networkInterface).ToString());
            var result = Util.RunRouteElevated(argument);
            if(result)
            {
                // failed to delete the entry
                if (DoesRoutingEntryExist(IPAddress.Parse(ipAddress)))
                {
                    throw new WLanException(WLanException.ERROR_IPROUTINGTABLE_REMOVE_FAILED, "DeleteEntryIfNeeded");
                }
            }

            return result;
        }

        /// <summary>
        /// Get a NetworkInterface instance for the specified physical network adapter
        /// This NetworkInterface instance is used to retrieve the current IPv4 address and interface index
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Parse the passed in pointer to a IPHelperInterop.IPForwardTable instance
        /// </summary>
        /// <param name="ipFwdTable"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Check if the specified ip address already exists in IP routing table
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        private bool DoesRoutingEntryExist(IPAddress ipAddress)
        {
            bool isExist = false;
            var ipTablePtr = IntPtr.Zero;
            int size = 0;

            try
            {
                var ret = IPHelperInterop.GetIpForwardTable(ipTablePtr, ref size, true);
                if(ret != IPHelperInterop.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new WLanException(ret, "GetIpForwardTableSize");
                }

                ipTablePtr = Marshal.AllocHGlobal(size);

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
