// DeployAppx.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>
#include <strsafe.h>

#include <roapi.h>
using namespace std;
#include <cctype>
#include <string>
#include <algorithm>

#include <wrl\client.h>
#include <wrl\wrappers\corewrappers.h>
#include <windows.management.deployment.h>

using namespace Platform;
using namespace Windows::Management::Deployment;

void ShowBanner();
void ShowUsage();
bool ParseCommandLine(Platform::Array<Platform::String^>^ args);
bool InstallCertificate(std::wstring CertName);
bool DoesFileExist(std::wstring strFile);

bool bInstall = false;			// assume uninstall.
wstring AppxPackageName(L"");	// name of package to install

int main(Platform::Array<Platform::String^>^ args)
{
	ShowBanner();

	

	if (args->Length != 3)	// <app> <un|install> <Appx>
	{
		ShowUsage();
		return -1;
	}

	if (ParseCommandLine(args))
	{
		// bInstall will be set (true | false)
		// now check on the APPX and .CER
		AppxPackageName = args[2]->Begin();	// hopefully the .APPX file.
		bool bHaveAppx = DoesFileExist(AppxPackageName);

		bool bHaveCert = false;
		std::wstring CertName = AppxPackageName;
		std::string::size_type found = CertName.rfind(L".appx");
		if (std::string::npos != found)
		{
			CertName = CertName.substr(0, found) + L".cer";
			wprintf(L"Looking for certificate  %s\n", CertName.c_str());
			bHaveCert = DoesFileExist(CertName);
		}

		if (!bHaveCert)
		{
			wprintf(L"Did not find certificate file %s\n", CertName.c_str());
			return -1;
		}

		if (!InstallCertificate(CertName))
		{
			wprintf(L"Failed to install certificate\n");
			return -1;
		}

		HRESULT hr = ::RoInitialize(RO_INIT_SINGLETHREADED);
		if (FAILED(hr))
		{
			wprintf(L"Failed to initialize Runtime\n");
			return -1;
		}

		String^ inputPackageUri = args[1];
	}
	else
	{
		ShowUsage();
		return -1;
	}

	return 0;
}


void ShowBanner()
{
	wstring wsAppName(L"DeployAppx");
}

void ShowUsage()
{
	wstring wsAppName(L"DeployAppx");
	wprintf(L"DeployAppx installs/removes APPX packages\n");
	wprintf(L"Usage:\n");
	wprintf(L"%s install MyApp.appx    // Install MyApp.appx (and certificate)\n\n", wsAppName.c_str());
	wprintf(L"%s uninstall MyApp.appx  // Uninstall MyApp.appx\n\n", wsAppName.c_str());
}

bool ParseCommandLine(Platform::Array<Platform::String^>^ args)
{
	// find out whether this is install or uninstall
	// we will check whether the APPX and .CER exist later.
	bool bRet = false;
	// convert 'command' to lower case.
	wstring wCommand(args[1]->Begin());
	transform(
		wCommand.begin(), wCommand.end(),
		wCommand.begin(),
		tolower);

	if (0 == wCommand.compare(L"install"))
	{
		bRet = true;
		bInstall = true;
	}

	if (0 == wCommand.compare(L"uninstall"))
	{
		bRet = true;
		bInstall = false;
	}

return bRet;
}

// uses certmgr on the device to install the cert.
bool InstallCertificate(std::wstring CertName)
{
	bool bRet = false;

	WCHAR localCommandLine[MAX_PATH];
	HRESULT hr = StringCchPrintf(
		localCommandLine,
		ARRAYSIZE(localCommandLine),
		L"certmgr.exe -add \"%s\" -r localMachine -s root",
		CertName.c_str());

	if (SUCCEEDED(hr))
	{
		PROCESS_INFORMATION processInformation = { 0 };
		STARTUPINFO startupInfo = { 0 };
		startupInfo.cb = sizeof(startupInfo);

		BOOL result = CreateProcess(NULL, localCommandLine,
			NULL, NULL, FALSE,
			NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW,
			NULL, NULL, &startupInfo, &processInformation);
		if (result)
		{
			wprintf(L"CreateProcess ok\n");
			DWORD exitCode = 0;
			// have a wait on the process for max of 30 seconds.
			DWORD dwWait = WaitForSingleObject(processInformation.hProcess, 30000);
			if (WAIT_OBJECT_0 == dwWait)  // process completed.
			{
				wprintf(L"WaitForSingleObject OK\n");
				result = GetExitCodeProcess(processInformation.hProcess, &exitCode);
				// Close the handles.
				CloseHandle(processInformation.hProcess);
				CloseHandle(processInformation.hThread);

				wprintf(L"Certmgr Exit Code - %ld\n", exitCode);
				// Certmgr has two return values (-1 for failure or 0 for success).
				if (0 == exitCode)
					bRet = true;
			}
			else
			{
				wprintf(L"WaitForSingleObject OK\n");
			}
		}
		else
		{
			wprintf(L"Failed to CreateProcess 'CertMgr.exe'\n");
		}
	}
	return bRet;
}

bool DoesFileExist(std::wstring strFile)
{
	bool bRet = false;
	WIN32_FIND_DATA fd = { 0 };
	HANDLE hFile = FindFirstFile(strFile.c_str(), &fd);
	if (INVALID_HANDLE_VALUE != hFile)
	{
		bRet = true;
		FindClose(hFile);
	}

	return bRet;
}
