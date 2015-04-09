// Scraw.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>

LPCWSTR TcpName1 = L"system\\currentcontrolset\\services\\tcpip\\parameters";
LPCWSTR TcpName2 = L"system\\controlset001\\services\\tcpip\\parameters";

LPCWSTR CompName1 = L"system\\controlset001\\control\\computername\\computername";
LPCWSTR CompName2 = L"system\\controlset001\\control\\computername\\activecomputername";

LPCWSTR CompName3 = L"system\\currentcontrolset\\control\\computername\\computername";
LPCWSTR CompName4 = L"system\\currentcontrolset\\control\\computername\\activecomputername";

// LPCWSTR MachineName = L"雅典娜";		// TEST string.

bool ContainsInvalidChars(PCWSTR str);
bool WriteValue(HKEY hKey, LPCWSTR lpKeyName, LPCWSTR lpKeyValue, DWORD dwKeyValueLen);

int _tmain(int argc, _TCHAR* argv[])
{
	HKEY hKey = NULL;

	wprintf(L"Set Computername RAW\n");

	if (argc != 2)
	{
		wprintf(L"Usage: Scraw <new computer name>\n");
		return EXIT_FAILURE;
	}
	else
	{
		wprintf(L"We have the correct number of parameters...\n");
	}

	if (!ContainsInvalidChars(argv[1]))
	{
		wprintf(L"Your computername contains invalid characters\n");
		return EXIT_FAILURE;
	}
	else
	{
		wprintf(L"String appears ok, no invalid characters found\n");
	}

	DWORD dwLen = wcslen(argv[1]) * sizeof(wchar_t);
	wprintf(L"New machine name has %ld bytes\n", dwLen);


// do the hostname parts first...
// we need to set two keys/values - "HostName" and "NV HostName"

	// Start with system\\currentcontrolset\\services\\tcpip\\parameters
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, TcpName1, 0, KEY_SET_VALUE, &hKey))
	{
		wprintf(L"Opened Key %s\n", TcpName1);
		WriteValue(hKey, L"HostName", argv[1], dwLen);
		WriteValue(hKey, L"NV HostName", argv[1], dwLen);
		RegCloseKey(hKey);
		hKey = NULL;
	}
	else
	{
		wprintf(L"Cannot open registry key %s\n", TcpName1);
	}

	// now try system\\controlset001\\services\\tcpip\\parameters
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, TcpName2, 0, KEY_SET_VALUE, &hKey))
	{
		wprintf(L"Opened Key %s\n", TcpName2);
		WriteValue(hKey, L"HostName", argv[1], dwLen);
		WriteValue(hKey, L"NV HostName", argv[1], dwLen);
		RegCloseKey(hKey);
		hKey = NULL;
	}
	else
	{
		wprintf(L"Cannot open registry key %s\n", TcpName1);
	}

	// now the "computername" set (CompName1-4).
	// CompName1
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, CompName1, 0, KEY_SET_VALUE, &hKey))
	{
		wprintf(L"Opened Key %s\n", CompName1);
		WriteValue(hKey, L"ComputerName", argv[1], dwLen);
		RegCloseKey(hKey);
		hKey = NULL;
	}
	else
	{
		wprintf(L"Cannot open registry key %s\n", CompName1);
	}

	// CompName2
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, CompName1, 0, KEY_SET_VALUE, &hKey))
	{
		wprintf(L"Opened Key %s\n", CompName2);
		WriteValue(hKey, L"ComputerName", argv[1], dwLen);
		RegCloseKey(hKey);
		hKey = NULL;
	}
	else
	{
		wprintf(L"Cannot open registry key %s\n", CompName2);
	}

	// CompName3
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, CompName3, 0, KEY_SET_VALUE, &hKey))
	{
		wprintf(L"Opened Key %s\n", CompName3);
		WriteValue(hKey, L"ComputerName", argv[1], dwLen);
		RegCloseKey(hKey);
		hKey = NULL;
	}
	else
	{
		wprintf(L"Cannot open registry key %s\n", CompName3);
	}


	// CompName4
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, CompName4, 0, KEY_SET_VALUE, &hKey))
	{
		wprintf(L"Opened Key %s\n", CompName4);
		WriteValue(hKey, L"ComputerName", argv[1], dwLen);
		RegCloseKey(hKey);
		hKey = NULL;
	}
	else
	{
		wprintf(L"Cannot open registry key %s\n", CompName4);
	}

	return 0;
}


bool ContainsInvalidChars(PCWSTR str)
{
	bool bRet = true;    // assume we have a good string.
	const wchar_t *ptr;

	wchar_t *InvalidChars = L"\"/\\[]:|<>+=;,? !";
	size_t iLen = wcslen(InvalidChars);
	for (size_t x = 0; x < iLen; x++)
	{
		wchar_t c = InvalidChars[x];
		ptr = wcschr(str, c);
		if (NULL != ptr)
		{
			bRet = false;
			break;
		}
	}
	return bRet;
}


bool WriteValue(HKEY hKey, LPCWSTR lpKeyName, LPCWSTR lpKeyValue, DWORD dwKeyValueLen)
{
	wprintf(L"Set Value %s - ", lpKeyName);
	if (ERROR_SUCCESS == RegSetValueExW(hKey, lpKeyName, 0, REG_SZ, (LPBYTE)lpKeyValue, dwKeyValueLen))
	{
		wprintf(L"OK\n");
		return true;
	}

	wprintf(L"E_EPIC_FAIL\n");
	return false;
}
