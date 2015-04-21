// Ver.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <windows.h>
#include <Shlwapi.h>

void ReportError();
void GetFileInfo(LPCWSTR szVersionFile);
bool FileExists(LPCWSTR lpFile);

int _tmain(int argc, _TCHAR* argv[])
{
	LPCWSTR lpInfoFile = L"c:\\windows\\system32\\cabinet.dll";

	if (FileExists(lpInfoFile))
	{
		GetFileInfo(lpInfoFile);
	}
	else
	{
		ReportError();
	}

	return 0;
}

void ReportError()
{
	wprintf(L"Could not get Windows version information\n");
}

void GetFileInfo(LPCWSTR szVersionFile)
{
	DWORD dwSig = 0xFEEF04BD;	// VS_FIXEDFILEINFO->dwSignature
	DWORD  versionHandle = NULL;
	UINT   size = 0;
	LPBYTE lpBuffer = NULL;
	DWORD  versionSize = GetFileVersionInfoSizeExW(0,szVersionFile, &versionHandle);

	if (versionSize != NULL)
	{
		LPSTR versionData = new char[versionSize];

		if (GetFileVersionInfoExW(0,szVersionFile, versionHandle, versionSize, versionData))
		{
			if (VerQueryValue(versionData, L"\\", (LPVOID*)&lpBuffer, &size))
			{
				if (size)
				{
					VS_FIXEDFILEINFO *verInfo = (VS_FIXEDFILEINFO *)lpBuffer;
					if (verInfo->dwSignature == dwSig)
					{
						wprintf(L"Version: %d.%d.%d.%d\n",
							(verInfo->dwFileVersionMS >> 16) & 0xffff,
							(verInfo->dwFileVersionMS >> 0) & 0xffff,
							(verInfo->dwFileVersionLS >> 16) & 0xffff,
							(verInfo->dwFileVersionLS >> 0) & 0xffff
							);
					}
				}
			}
		}
		delete[] versionData;
	}
	else
	{
		ReportError();
	}
}

bool FileExists(LPCWSTR lpFile)
{
	bool bRet = false;	// assume file doesn't exist
	WIN32_FIND_DATA fd;
	memset(&fd, 0x00, sizeof(WIN32_FIND_DATA));

	HANDLE hFind = FindFirstFile(lpFile, &fd);
	if (INVALID_HANDLE_VALUE != hFind)
	{
		bRet = true;
	}

	FindClose(hFind);

	return bRet;
}
