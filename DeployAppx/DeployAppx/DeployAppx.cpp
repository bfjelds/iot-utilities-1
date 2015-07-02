// DeployAppx.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>
#include <strsafe.h>
#include <Psapi.h>

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
bool InstallCertificate(std::wstring CertName);
bool DoesFileExist(std::wstring strFile);
bool Impersonate();
bool Revert();
HANDLE OpenProcessByName(std::wstring ProcessName);

bool bInstall = false;			// assume uninstall.
wstring AppxPackageName(L"");	// name of package to install

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


		HRESULT hr = ::RoInitialize(RO_INIT_MULTITHREADED);
		if (FAILED(hr))
		{
			wprintf(L"Failed to initialize Runtime\n");
			return -1;
		}

		HANDLE completedEvent = nullptr;
		int returnValue = 0;
		String^ inputPackageUri = args[2];
		try
		{
			completedEvent = CreateEventEx(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, EVENT_ALL_ACCESS);
			if (completedEvent == nullptr)
			{
				wcout << L"CreateEvent Failed, error code=" << GetLastError() << endl;
				returnValue = 1;
			}
			else
			{
				wcout << L"Create Event OK" << endl;

				if (!Impersonate())
				{
					wprintf(L"Cannot install APPX - Impersonate SiHost Failed.\n");
					return -1;
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

				wcout << L"Installing package " << inputPackageUri->Data() << endl;

				wcout << L"Waiting for installation to complete..." << endl;

				WaitForSingleObject(completedEvent, INFINITE);

				if (deploymentOperation->Status == Windows::Foundation::AsyncStatus::Error)
				{
					auto deploymentResult = deploymentOperation->GetResults();
					wcout << L"Installation Error: " << deploymentOperation->ErrorCode.Value << endl;
					wcout << L"Detailed Error Text: " << deploymentResult->ErrorText->Data() << endl;
				}
				else if (deploymentOperation->Status == Windows::Foundation::AsyncStatus::Canceled)
				{
					wcout << L"Installation Canceled" << endl;
				}
				else if (deploymentOperation->Status == Windows::Foundation::AsyncStatus::Completed)
				{
					wcout << L"Installation succeeded!" << endl;
				}

				// revert back to the logged on user.
				Revert();
			}
		}
		catch (Exception^ ex)
		{
			wcout << L"AddPackageSample failed, error message: " << ex->ToString()->Data() << endl;
			returnValue = 1;
		}

		if (completedEvent != nullptr)
			CloseHandle(completedEvent);


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

bool Revert()
{
	if (!RevertToSelf())
		return false;

	return true;
}