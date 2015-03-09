// ListServices.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <stdio.h>
#include <Windows.h>

// will probably expand to include support for starting/stopping services
void ListServices();
void ErrorDescription(DWORD p_dwError);

int _tmain(int argc, _TCHAR* argv[])
{
	wprintf(L"Utility to list installed Windows (NT) Services on Athens\n");

	ListServices();

	return 0;
}

void ListServices()
{
	PUCHAR  pBuf = NULL;
	ULONG  dwBufSize = 0x00;
	ULONG  dwBufNeed = 0x00;
	ULONG  dwNumberOfService = 0x00;

	LPENUM_SERVICE_STATUS_PROCESS pInfo = NULL;

	SC_HANDLE hHandle = OpenSCManager(NULL, NULL, SC_MANAGER_ENUMERATE_SERVICE);
	if (NULL == hHandle)
	{
		ErrorDescription(GetLastError());
		return;
	}

	EnumServicesStatusEx(
		hHandle,
		SC_ENUM_PROCESS_INFO,
		SERVICE_WIN32, // SERVICE_DRIVER
		SERVICE_STATE_ALL,
		NULL,
		dwBufSize,
		&dwBufNeed,
		&dwNumberOfService,
		NULL,
		NULL);

	if (dwBufNeed < 0x01)
	{
		printf_s("EnumServicesStatusEx fail ?? \n");
		return;
	}

	dwBufSize = dwBufNeed + 0x10;
	pBuf = (PUCHAR)malloc(dwBufSize);

	EnumServicesStatusEx(
		hHandle,
		SC_ENUM_PROCESS_INFO,
		SERVICE_WIN32,  // SERVICE_DRIVER,
		SERVICE_STATE_ALL,  //SERVICE_ACTIVE,
		pBuf,
		dwBufSize,
		&dwBufNeed,
		&dwNumberOfService,
		NULL,
		NULL);

	pInfo = (LPENUM_SERVICE_STATUS_PROCESS)pBuf;
	for (ULONG i = 0; i<dwNumberOfService; i++)
	{
		wprintf_s(L"Display Name  : %ls \n", pInfo[i].lpDisplayName);
		wprintf_s(L"Service Name  : %ls \n", pInfo[i].lpServiceName);
		wprintf_s(L"Process Id    : %04x (%d) \n", pInfo[i].ServiceStatusProcess.dwProcessId, pInfo[i].ServiceStatusProcess.dwProcessId);
		wprintf(  L"State         : ");
		switch (pInfo[i].ServiceStatusProcess.dwCurrentState)
		{
			case SERVICE_CONTINUE_PENDING:
				wprintf(L"Continue Pending");
				break;

			case SERVICE_PAUSE_PENDING:
				wprintf(L"Pause Pending");
				break;

			case SERVICE_PAUSED:
				wprintf(L"Paused");
				break;

			case SERVICE_RUNNING:
				wprintf(L"Running");
				break;

			case SERVICE_START_PENDING:
				wprintf(L"Start Pending");
				break;

			case SERVICE_STOP_PENDING:
				wprintf(L"Stop Pending");
				break;

			case SERVICE_STOPPED:
				wprintf(L"Stopped");
				break;

			default:
				wprintf(L"Unknown!");
				break;
		}
		wprintf(L"\n----------------------------------\n");
	}

	free(pBuf);

	if (!CloseServiceHandle(hHandle))
	{
		ErrorDescription(GetLastError());
	}
}
// get the description of error
void ErrorDescription(DWORD p_dwError)
{
	wprintf(L"Error: %ld\n", p_dwError);
}