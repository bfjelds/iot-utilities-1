// GetAdapterInfo.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include <stdio.h>
#include <wchar.h>
#include <WinSock2.h>
#include <Ws2tcpip.h>
#include <iphlpapi.h>
#include <stdlib.h>
#pragma comment(lib, "IPHLPAPI.lib")
#include <assert.h>
#include "strsafe.h"

#define MALLOC(x) HeapAlloc(GetProcessHeap(), 0, (x))
#define FREE(x) HeapFree(GetProcessHeap(), 0, (x))

const int host_len = 33;
const int ipv4_len = 4 * 4 + 1;
const int mac_len = 3 * 8 + 1;
const char DestAddr[] = "239.0.0.222";


struct BufferForBroadcast
{
	WCHAR host[host_len];
	WCHAR ipv4[ipv4_len];
	WCHAR mac[mac_len];
} UdpBroadcastBuffer, LocalBroadcastBuffer;

void DisplayComputerName();
void DebugPrint(WCHAR *sz, ...);
void EnumAdapters();

int _tmain(int argc, _TCHAR* argv[])
{
	wprintf(L"Enumerate Network Adapters\n");

	DisplayComputerName();
	EnumAdapters();

	return 0;
}

void DisplayComputerName()
{
	WCHAR HostName[host_len] = { 0 };
	DWORD HostNameWchars = host_len;            // should be MAX_COMPUTERNAME_LENGTH - but we don't want to impact the existing eboot_watcher.

	if (GetComputerNameExW(ComputerNameDnsHostname, HostName, &HostNameWchars))
	{
		wprintf(L"Hostname: %s\n", HostName);
		// display the HostName and copy to the broadcast buffer.
		DebugPrint(L"Hostname: %s\n", HostName);
	}
	else
	{
		wprintf(L"Failed to get Computer Name\n");
		DebugPrint(L"Failed to get Computer Name\n");
	}
}


void EnumAdapters()
{
	BufferForBroadcast *buffer = &LocalBroadcastBuffer;

	// It is possible for an adapter to have multiple
	// IPv4 addresses, gateways, and secondary WINS servers
	// assigned to the adapter. 
	//
	// Note that this sample code only prints out the 
	// first entry for the IP address/mask, and gateway, and
	// the primary and secondary WINS server for each adapter. 

	PIP_ADAPTER_INFO pAdapterInfo = NULL;
	PIP_ADAPTER_INFO pAdapter = NULL;
	DWORD dwRetVal = 0;
	bool bRet = false;

	ULONG ulOutBufLen = sizeof(IP_ADAPTER_INFO);
	pAdapterInfo = (IP_ADAPTER_INFO *)MALLOC(sizeof(IP_ADAPTER_INFO));
	if (pAdapterInfo == NULL) {
		DebugPrint(L"Error allocating memory\n");
		return;
	}
	// Make an initial call to GetAdaptersInfo to get
	// the necessary size into the ulOutBufLen variable
//#pragma warning(suppress: __WARNING_IPV6_USE_EX_VERSION)
	if (GetAdaptersInfo(pAdapterInfo, &ulOutBufLen) == ERROR_BUFFER_OVERFLOW) {
		FREE(pAdapterInfo);
		pAdapterInfo = (IP_ADAPTER_INFO *)MALLOC(ulOutBufLen);
		if (pAdapterInfo == NULL) {
			DebugPrint(L"Error allocating memory\n");
			return;
		}
	}

//#pragma warning(suppress: __WARNING_IPV6_USE_EX_VERSION)
	if ((dwRetVal = GetAdaptersInfo(pAdapterInfo, &ulOutBufLen)) == NO_ERROR) {
		pAdapter = pAdapterInfo;

		while (pAdapter) {
			bool bEthernet = false;

			switch (pAdapter->Type) {
			case MIB_IF_TYPE_ETHERNET:
				bEthernet = true;
				break;
			case MIB_IF_TYPE_LOOPBACK:
				break;
			}
			printf("Adapter Name: %s\n", pAdapter->AdapterName);
			printf("Description : %s\n", pAdapter->Description);
			printf("IP Address  : %s\n", pAdapter->IpAddressList.IpAddress.String);
			printf("Ethernet: ");
			switch (pAdapter->Type)
			{
			case MIB_IF_TYPE_OTHER:
				printf("MIB_IF_TYPE_OTHER");
				break;
			case MIB_IF_TYPE_ETHERNET:
				printf("MIB_IF_TYPE_ETHERNET");
				break;
			case MIB_IF_TYPE_TOKENRING:
				printf("MIB_IF_TYPE_TOKENRING");
				break;
			case MIB_IF_TYPE_FDDI:
				printf("MIB_IF_TYPE_FDDI");
				break;
			case MIB_IF_TYPE_PPP:
				printf("MIB_IF_TYPE_PPP");
				break;
			case MIB_IF_TYPE_LOOPBACK:
				printf("MIB_IF_TYPE_LOOPBACK");
				break;
			case MIB_IF_TYPE_SLIP:
				printf("MIB_IF_TYPE_SLIP");
				break;
			case IF_TYPE_IEEE80211:
				printf("Wireless Adapter!");
				break;
			default:
				printf("MIB Type - %ld\n", pAdapter->Type);
				break;
			}
			printf("\n");

			if (bEthernet && strncmp("0.0.0.0", pAdapter->IpAddressList.IpAddress.String, 7))
			{
				swprintf_s(buffer->ipv4, ipv4_len, L"%S", pAdapter->IpAddressList.IpAddress.String);
				DebugPrint(L"IP Address: \t%s\n", buffer->ipv4);
				swprintf_s(buffer->mac, mac_len, L"%02x:%02x:%02x:%02x:%02x:%02x:%02x:%02x",
					(BYTE)pAdapter->Address[0],
					(BYTE)pAdapter->Address[1],
					(BYTE)pAdapter->Address[2],
					(BYTE)pAdapter->Address[3],
					(BYTE)pAdapter->Address[4],
					(BYTE)pAdapter->Address[5],
					(BYTE)pAdapter->Address[6],
					(BYTE)pAdapter->Address[7]);
				printf("Mac Address: %s\n", pAdapter->Address);
				DebugPrint(L"Mac Address: \t%s\n", buffer->mac);
				bRet = true;
				break;
			}
			printf("\n");
			pAdapter = pAdapter->Next;
		}
	}
	else {
		DebugPrint(L"Getting Adapter Info failed with error: %d\n", dwRetVal);
	}
	FREE(pAdapterInfo);
}

void DebugPrint(WCHAR *sz, ...)
{
	WCHAR buffer[2000];
	va_list args;

	va_start(args, sz);
	HRESULT hr = StringCbVPrintf(buffer, sizeof(buffer), sz, args);

	if (STRSAFE_E_INSUFFICIENT_BUFFER == hr || S_OK == hr)
	{
		OutputDebugString(buffer);
	}
	else
	{
		OutputDebugString(L"StringCbVPrintf error.");
	}

	va_end(args);
}
