// SetBootOption.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>

void ShowCurrentBootSetting();
bool IsImageConfiguredHeaded();
void SetBootMode(DWORD dwValue);
void Usage();

int _tmain(int argc, _TCHAR* argv[])
{
	wprintf(L"Set Athens Boot Options\n");

	if (argc == 1)
	{
		Usage();
		ShowCurrentBootSetting();
		return 0;
	}

	if (argc == 2)
	{
		wchar_t *pLwr;
		errno_t err;

		err = _wcslwr_s(pLwr = _wcsdup(argv[1]), wcslen(argv[1]) + 1);

		if (0 == wcscmp(L"headless", pLwr))
		{
			SetBootMode(1);
			return 0;
		}

		if (0 == wcscmp(L"headed", pLwr))
		{
			SetBootMode(0);
			return 0;
		}

		wprintf(L"Invalid option:\n");
		Usage();
	}

	return 0;
}

bool IsImageConfiguredHeaded()
{
	HKEY key = NULL;
	DWORD dwData = 0;
	DWORD dwSizeOfData = sizeof(DWORD);

	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"system\\currentcontrolset\\control\\wininit", 0, KEY_QUERY_VALUE, &key))
	{
		if (ERROR_SUCCESS == RegQueryValueEx(key, L"headless", NULL,NULL, (LPBYTE)&dwData, &dwSizeOfData))
		{
		}
		else
		{
			wprintf(L"Something went wrong, can't read the current boot setting\n");
		}
		RegCloseKey(key);
	}
	return (bool)dwData;
}

void ShowCurrentBootSetting()
{
	wprintf(L"Current Configuration: ");

	if (!IsImageConfiguredHeaded())
		wprintf(L"headed");
	else
		wprintf(L"headless");

}

void SetBootMode(DWORD dwValue)
{
	HKEY hKey = NULL;

	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"system\\currentcontrolset\\control\\wininit", 0, KEY_SET_VALUE, &hKey))
	{
		if (ERROR_SUCCESS == RegSetValueEx(hKey, L"headless", 0, REG_DWORD, (LPBYTE)&dwValue, (DWORD)sizeof(dwValue)))
		{
			wprintf(L"Success - boot mode now set to ");
			if (dwValue)
			{
				wprintf(L"headless");
			}
			else
			{
				wprintf(L"headed");
			}
			wprintf(L"\n");
			wprintf(L"Don't forget to reboot to get the new value\n");
			wprintf(L"Hint: 'shutdown /r /t 0\n");
			wprintf(L"\n");
		}
		else
		{
			wprintf(L"Something went wrong, couldn't set boot mode\n");
		}
		RegCloseKey(hKey);
	}
}

void Usage()
{
	wprintf(L"Usage: SetBootOption [headed | headless]\n");
	wprintf(L"Running the app without any options will show the current setting\n");
	wprintf(L"Note: Changing a setting will require a reboot\n");
	wprintf(L"\n");

}