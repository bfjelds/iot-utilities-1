// SetBootOption.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>

void ShowCurrentDisplayTimeoutSetting();
bool IsDisplayTimeoutConfigured();
bool SetDisplayTimeout(DWORD dwValue);
void Usage();
bool SetRegKeyValue(LPCWSTR RegPath, LPCWSTR RegKey, DWORD dwVal);
bool GetRegKeyValue(LPCWSTR RegPath, LPCWSTR RegKey, LPDWORD dwRetVal);
bool IsNum(LPCWSTR lpString);

LPCWSTR PowerSettingsRegPath = L"SYSTEM\\ControlSet001\\Control\\Power\\PowerSettings\\7516b95f-f776-4464-8c53-06167f40cc99\\3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e";

int _tmain(int argc, _TCHAR* argv[])
{
	wprintf(L"Set Windows 10 IoT Core Display Timeout\n\n");

	if (argc == 1)	// no command line options
	{
		Usage();
		ShowCurrentDisplayTimeoutSetting();
		return 0;
	}

	if (argc >= 2)
	{
		wchar_t *pLwr;
		errno_t err;
		bool bOptionOk = false;
		DWORD dwTimeoutSetting = 0;	// disable timeout (default).

		err = _wcslwr_s(pLwr = _wcsdup(argv[1]), wcslen(argv[1]) + 1);

		if (0 == wcscmp(L"on", pLwr))
		{
			bOptionOk = true;	// good option!
			dwTimeoutSetting = 600;		// set ten minute default timeout.

			if (argc == 3)	// app, timeout on/off and timeout value (maybe, need to check).
			{
				if (IsNum(argv[2]))
				{
					int iVal = _wtoi(argv[2]);
					if (iVal > 0)
						dwTimeoutSetting = (DWORD)iVal;
				}
				else
				{
					bOptionOk = false;
				}
			}
		}

		if (0 == wcscmp(L"off", pLwr))
		{
			bOptionOk = true;
			dwTimeoutSetting = 0;
		}

		if (bOptionOk)
		{
			if (dwTimeoutSetting > 0)
			{
				wprintf(L"Setting display timeout at %d seconds\n\n", dwTimeoutSetting);
			}
			
			SetDisplayTimeout(dwTimeoutSetting);
			ShowCurrentDisplayTimeoutSetting();
			wprintf(L"\nDon't forget to reboot to get the new setting!\n");
			wprintf(L"Hint: shutdown -r -t 0\n");
		}
		else
		{
			wprintf(L"Invalid option!\n");
			Usage();
		}
	}

	return 0;
}

bool IsDisplayTimeoutConfigured()
{
	HKEY key = NULL;
	DWORD dwData = 0;
	DWORD dwSizeOfData = sizeof(DWORD);

	DWORD dwDCPower = 0;
	DWORD dwACPower = 0;

	bool bRet = false;

	if (GetRegKeyValue(PowerSettingsRegPath, L"AcSettingIndex", &dwACPower) && GetRegKeyValue(PowerSettingsRegPath, L"DcSettingIndex", &dwDCPower))
	{
		if (dwACPower || dwDCPower)
		{
			bRet = TRUE;		// AC or DC are set to '1', therefore display timeout is active (on) [display will timeout].
		}
	}

	return bRet;
}

void ShowCurrentDisplayTimeoutSetting()
{
	if (IsDisplayTimeoutConfigured())
		wprintf(L"Display will timeout");
	else
		wprintf(L"Display will not timeout");

	wprintf(L"\n");
}

bool SetDisplayTimeout(DWORD dwValue)
{
	bool bRet = false;

	if (SetRegKeyValue(PowerSettingsRegPath, L"AcSettingIndex", dwValue) && SetRegKeyValue(PowerSettingsRegPath, L"DcSettingIndex", dwValue))
	{
		bRet = true;
	}

	return bRet;
}

bool GetRegKeyValue(LPCWSTR RegPath, LPCWSTR RegKey,LPDWORD dwRetVal)
{
	bool bRet= false;	// assume something is going to go wrong, update if everything is ok :)
	HKEY hKey = NULL;
	DWORD dwSize = sizeof(DWORD);
	DWORD dwValue = 0;

	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, RegPath, 0, KEY_READ, &hKey))
	{
		if (ERROR_SUCCESS == RegQueryValueEx(hKey, RegKey, NULL, NULL, (LPBYTE)&dwValue, &dwSize))
		{
			*dwRetVal = dwValue;
			bRet = true;	// opened and read ok - we're good.
		}
		RegCloseKey(hKey);
	}

	return bRet;
}

bool SetRegKeyValue(LPCWSTR RegPath, LPCWSTR RegKey, DWORD dwVal)
{
	bool bRet = false;	// assume something went wrong, update if all goes ok :)
	HKEY hKey = NULL;

	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, RegPath, 0, KEY_SET_VALUE, &hKey))
	{
		if (ERROR_SUCCESS == RegSetValueEx(hKey, RegKey, 0, REG_DWORD, (LPBYTE)&dwVal, (DWORD)sizeof(dwVal)))
		{
			bRet = true;	// set value ok, success.
		}
		RegCloseKey(hKey);
	}
	return bRet;
}

bool IsNum(LPCWSTR lpString)
{
	bool bRet = true;

	try
	{
		_wtoi(lpString);
	}
	catch(...)
	{
		bRet = false;
	}

	return bRet;
}

void Usage()
{
	wprintf(L"Usage: SetDisplayTimeout [on <timeout in seconds> | off]\n");
	wprintf(L"Examples:\n");
	wprintf(L"SetDisplayTimeout off    // disables screen timeout\n");
	wprintf(L"SetDisplayTimeout on 600 // enables screen timeout at 10 minutes [600 seconds]\n");
	wprintf(L"Running the app without any options will show the current setting\n");
	wprintf(L"Note: Changing a setting will require a reboot\n");
	wprintf(L"\n");
}
