// Startup.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>
#include <string.h>

#define string _TCHAR *

static const string sBootKey = L"System\\ControlSet001\\Services\\Bootsh\\Parameters\\Commands"; 

void ShowUsage();
bool CheckForDisplay(const string sOption);
bool AddItem(string sOption, string sName, string sValue);
bool DeleteItem(string sOption, string sName);
void ShowStartupApps();

int _tmain(int argc, _TCHAR* argv[])
{
	wprintf(L"Athens Startup Apps\r\n\r\n");

	if (argc < 2)
	{
		ShowUsage();
		return 0;
	}

	if (argc == 2)	// could be '/d'
	{
		if (!CheckForDisplay(argv[1]))
			ShowUsage();
	}

	if (argc == 3)
	{
		if (!DeleteItem(argv[1], argv[2]))
			ShowUsage();
	}

	if (argc == 4)
	{
		if (!AddItem(argv[1], argv[2], argv[3]))
		{
			ShowUsage();
		}
	}

	return 0;
}

bool AddItem(string sOption, string sName,string sValue)
{
	bool bRet = false;
	errno_t tErr = 0;
	HKEY hKey=NULL;

	int iLen = wcslen(sOption);
	if (iLen == 2)
	{
		string sLwrString = _tcsdup(sOption);
		// lowerize the string.
		tErr = _wcslwr_s(sLwrString, iLen + 1);
		if (0 == wcscmp(sLwrString, L"/a"))
		{
			wprintf(L"Adding %s\r\n\r\n", sName);
			if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, sBootKey, 0, KEY_SET_VALUE, &hKey))
			{
				RegSetValueExW(hKey, sName,NULL, REG_SZ, (LPBYTE)sValue, wcslen(sValue)*sizeof(wchar_t));
				RegCloseKey(hKey);
				ShowStartupApps();
				bRet = true;
			}
			else
			{
				wprintf(L"Failed to open RUN key for writing.\r\n");
			}
		}
		free(sLwrString);
	}

	return bRet;
}

bool DeleteItem(string sOption, string sName)
{
	bool bRet = false;
	errno_t tErr = 0;
	HKEY hKey = NULL;

	int iLen = wcslen(sOption);
	if (iLen == 2)
	{
		string sLwrString = _tcsdup(sOption);
		// lowerize the string.
		tErr = _wcslwr_s(sLwrString, iLen + 1);
		if (0 == wcscmp(sLwrString, L"/r"))
		{
			wprintf(L"Deleting %s\r\n", sName);
			if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, sBootKey, 0, KEY_SET_VALUE, &hKey))
			{
				RegDeleteValueW(hKey, sName);
				RegCloseKey(hKey);
				ShowStartupApps();
				bRet = true;
			}
			else
			{
				wprintf(L"Failed to open RUN key for writing.\r\n");
			}
		}
		free(sLwrString);
	}

	return bRet;
}


bool CheckForDisplay(const string sOption)
{
	bool bRet = false;	// assume all is bad...
	errno_t tErr = 0;

	int iLen = wcslen(sOption);
	if (iLen == 2)
	{
		string sLwrString = _tcsdup(sOption);
		// lowerize the string.
		tErr = _wcslwr_s(sLwrString, iLen+1);
		if (0 == wcscmp(sLwrString, L"/d"))
		{
			bRet = true;
			free(sLwrString);
			// go get the registry stuff.
			ShowStartupApps();
		}
	}

	return bRet;
}

void ShowStartupApps()
{
	HKEY hKey = NULL;
	if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, sBootKey, 0, KEY_READ, &hKey))
	{
		wchar_t sSubKey[255];
		DWORD dwSubKeyLen = 255;
		wchar_t byteData[255];
		DWORD dwDataLen = 255;
		DWORD dwType = 0;
		DWORD dwIndex = 0;
		LONG lRet = 0;
		// get values under this key.
		while (lRet == ERROR_SUCCESS)
		{
			lRet = RegEnumValueW(hKey, dwIndex, sSubKey, &dwSubKeyLen, 0, &dwType, (LPBYTE)byteData, &dwDataLen);
			dwIndex++;
			if (lRet == ERROR_SUCCESS && dwType == REG_SZ)
			{
				wprintf(L"%s ", sSubKey);
				if (dwDataLen > 0)
				{
					wprintf(L"- %s", byteData);
				}
				wprintf(L"\r\n");
			}
			dwSubKeyLen = 255;
			dwDataLen = 255;
		}
		RegCloseKey(hKey);
	}
	else
	{
		wprintf(L"Failed to open Athens RUN key\r\n");
	}
}


void ShowUsage()
{
	wprintf(L"/d - display the list of startup apps\r\n");
	wprintf(L"/a <Name> <command line> - add an app into the list of startup apps\r\n");
	wprintf(L"/r - <Name> remove an app from the list of startup apps\r\n");
	wprintf(L"\r\n");
	wprintf(L"where:\r\n");
	wprintf(L"<Name> is the name of the app in the startup registry\r\n");
	wprintf(L"<command line> is the full command line for the app\r\n");
	wprintf(L"Example:\r\n");
	wprintf(L"Startup /a EbootPinger \"start \\windows\\system32\\EbootPinger.exe\"\r\n");
	wprintf(L"\r\n");
}