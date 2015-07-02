// DeployAppx.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>
#include <strsafe.h>
#include <Psapi.h>
#include <stdio.h>

#include <roapi.h>
using namespace std;
#include <cctype>
#include <string>
#include <algorithm>
#include <iostream>
#include <vector>

#include <wrl\client.h>
#include <wrl\wrappers\corewrappers.h>
#include <windows.management.deployment.h>

using namespace Windows::Foundation;
using namespace Platform;
using namespace Windows::Management::Deployment;

void ShowBanner();
void ShowUsage();
bool ParseCommandLine(Platform::Array<Platform::String^>^ args);

bool DoesFileExist(std::wstring FileToCheck);

// Functions to impersonate 'SiHost' user account
bool Impersonate();
HANDLE OpenProcessByName(std::wstring ProcessName);
bool Revert();

// Install and uninstall APPX Packages + Cert
bool InstallAppxPackage(std::wstring PackageName);
bool InstallCertificate(std::wstring CertName);
bool UninstallAppxPackage(std::wstring PackageName);

bool bInstall = false;			// assume uninstall.

[MTAThread]
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
		std::wstring AppxName(args[2]->Begin());
		if (bInstall)
			InstallAppxPackage(AppxName);
		else
			UninstallAppxPackage(AppxName);
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

bool InstallAppxPackage(std::wstring PackageName)
{
	bool retVal = true;
	bool bHaveCert = false;

	wchar_t wcBuffer[MAX_PATH] = { 0 };
	if (GetFullPathName(PackageName.c_str(), MAX_PATH, wcBuffer, NULL))
	{
		PackageName = wcBuffer;
	}

	std::wstring CertName(PackageName.c_str());
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
		return false;
	}

	if (!InstallCertificate(CertName))
	{
		wprintf(L"Failed to install certificate\n");
		return false;
	}

	// looks like we have both files... now try to install.
	HRESULT hr = ::RoInitialize(RO_INIT_MULTITHREADED);
	if (FAILED(hr))
	{
		wprintf(L"Failed to initialize Runtime\n");
		return false;
	}

	HANDLE completedEvent = nullptr;
	int returnValue = 0;
	String^ inputPackageUri = ref new Platform::String(PackageName.c_str());
	try
	{
		completedEvent = CreateEventEx(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, EVENT_ALL_ACCESS);
		if (completedEvent == nullptr)
		{
			wprintf(L"CreateEvent Failed, error code= %ld\n", GetLastError());
			returnValue = 1;
		}
		else
		{
			if (!Impersonate())
			{
				wprintf(L"Cannot install APPX - Impersonate SiHost Failed.\n");
				return false;
			}

			auto packageUri = ref new Uri(inputPackageUri);

			auto packageManager = ref new PackageManager();
			auto deploymentOperation = packageManager->AddPackageAsync(packageUri, nullptr, DeploymentOptions::None);

			deploymentOperation->Completed =
				ref new AsyncOperationWithProgressCompletedHandler<DeploymentResult^, DeploymentProgress>(
					[&completedEvent](IAsyncOperationWithProgress<DeploymentResult^, DeploymentProgress>^ operation, Windows::Foundation::AsyncStatus)
			{
				SetEvent(completedEvent);
			});
			wprintf(L"Installing Package %s\n", inputPackageUri->Data());

			wprintf(L"Waiting for install to complete...\n");

			WaitForSingleObject(completedEvent, INFINITE);

			if (deploymentOperation->Status == Windows::Foundation::AsyncStatus::Error)
			{
				auto deploymentResult = deploymentOperation->GetResults();
				wprintf(L"Install Error: %ld\n", deploymentOperation->ErrorCode.Value);
				wprintf(L"Detailed Error Information: %s\n", deploymentResult->ErrorText->Data());
			}
			else if (deploymentOperation->Status == Windows::Foundation::AsyncStatus::Canceled)
			{
				wprintf(L"Installation Cancelled\n");
			}
			else if (deploymentOperation->Status == Windows::Foundation::AsyncStatus::Completed)
			{
				wprintf(L"Install Succeeded!\n");
			}

			// revert back to the logged on user.
			Revert();
		}
	}
	catch (Exception^ ex)
	{
		wprintf(L"Installation Failed, Error Message : %s\n", ex->ToString()->Data());
		retVal = false;
	}

	if (completedEvent != nullptr)
		CloseHandle(completedEvent);

	return retVal;
}

bool UninstallAppxPackage(std::wstring AppPackage)
{
	bool bRet = true;


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

bool DoesFileExist(std::wstring FileToCheck)
{
	bool bRet = false;
	WIN32_FIND_DATA fd = { 0 };
	HANDLE hFile = FindFirstFile(FileToCheck.c_str(), &fd);
	if (INVALID_HANDLE_VALUE != hFile)
	{
		bRet = true;
		FindClose(hFile);
	}

	return bRet;
}

// impersonate the Default Account user to install the .APPX Package.
bool Impersonate()
{
	std::wstring siProcess = L"sihost.exe";

	HANDLE hProcess = OpenProcessByName(siProcess);
	if (NULL == hProcess)
	{
		return false;
	}

	HANDLE InteractiveProcessToken = NULL;
	if (!OpenProcessToken(hProcess, TOKEN_ALL_ACCESS, &InteractiveProcessToken))
	{
		return false;
	}

	HANDLE ImpersonationToken = NULL;
	if (!DuplicateToken(InteractiveProcessToken, SECURITY_IMPERSONATION_LEVEL::SecurityImpersonation, &ImpersonationToken))
	{
		return false;
	}

	if (!ImpersonateLoggedOnUser(ImpersonationToken))
	{
		return false;
	}

	return true;
}

// Walk the process list looking for 'SiHost' so we can get the Process Handle
HANDLE OpenProcessByName(std::wstring ProcessName)
{
	const size_t ProcessNameLength = wcslen(ProcessName.c_str());
	vector<DWORD> spProcessIds(1024);
	DWORD bytesReturned = 0;
	WCHAR imageFileName[1024];

	if (EnumProcesses(&spProcessIds.front(),
		static_cast<unsigned int>(spProcessIds.size() * sizeof(DWORD)),
		&bytesReturned))
	{
		const unsigned int actualProcessIds = bytesReturned / sizeof(unsigned int);
		wprintf(L"Enumerating %ld processes\n", actualProcessIds);
		for (unsigned int i = 0; i < actualProcessIds; i++)
		{
			HANDLE hProcess=OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ,
				FALSE,
				spProcessIds[i]);
			if (NULL != hProcess)
			{
				const size_t imageFileNameLength = GetProcessImageFileNameW(hProcess, imageFileName, sizeof(imageFileName));
				wprintf(L"Process %d - Name %s\n", i, &imageFileName[imageFileNameLength - ProcessNameLength]);
				if (imageFileNameLength >= ProcessNameLength && (_wcsicmp(ProcessName.c_str(), &imageFileName[imageFileNameLength-ProcessNameLength])) == 0)
				{
					return hProcess;
				}
			}
		}
	}

	return NULL;
}

// Revert from Default Account to Logged in user
bool Revert()
{
	if (!RevertToSelf())
		return false;

	return true;
}

