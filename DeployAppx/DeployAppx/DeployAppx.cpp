// DeployAppx.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>
#include <strsafe.h>
#include <Psapi.h>
#include <stdio.h>
#include <appmodel.h>
#include <collection.h>

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
using namespace Windows::Foundation::Collections;
using namespace Platform;
using namespace Windows::Management::Deployment;

void ShowBanner();
void ShowUsage();
bool ParseCommandLine(Platform::Array<Platform::String^>^ args);

bool DoesFileExist(std::wstring FileToCheck);
int ConvertStringToInt(std::string sInput);

// Functions to impersonate 'SiHost' user account
bool Impersonate();
HANDLE OpenProcessByName(std::wstring ProcessName);
bool Revert();

// Install and uninstall APPX Packages + Cert
bool InstallAppxPackage(std::wstring PackageName);
bool InstallCertificate(std::wstring CertName);
bool UninstallAppxPackage( );
void DisplayPackageInfo(Windows::ApplicationModel::Package^ package, int iPackageNum);
bool RemovePackage(Platform::String^ PackageFullName);

bool bInstall = false;			// assume uninstall.

[MTAThread]
int main(Platform::Array<Platform::String^>^ args)
{
	ShowBanner();

	// TODO: fix install/uninstall so that uninstall works with just 'uninstall' parameter
	if (args->Length < 2 || args->Length > 3)
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
			UninstallAppxPackage();
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
	wprintf(L"%s uninstall             // Uninstall an application (choose from a list)\n\n", wsAppName.c_str());
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

	if (0 == wCommand.compare(L"install") && args->Length == 3)
	{
		bRet = true;
		bInstall = true;
	}

	if (0 == wCommand.compare(L"uninstall") && args->Length == 2)
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
				wprintf(L"Cannot install APPX - Impersonate Default Account Failed.\n");
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

bool UninstallAppxPackage( )
{
	bool retVal = true;

	HRESULT hr = ::RoInitialize(RO_INIT_MULTITHREADED);
	if (FAILED(hr))
	{
		wprintf(L"Failed to initialize Runtime\n");
		return false;
	}
	auto packageManager = ref new PackageManager();

	Windows::Foundation::Collections::IIterable<Windows::ApplicationModel::Package^> ^packages =
		packageManager->FindPackages();

	int packageCount = 0;
	std::for_each(Windows::Foundation::Collections::begin(packages), Windows::Foundation::Collections::end(packages),
		[&](Windows::ApplicationModel::Package^ package)
	{
		DisplayPackageInfo(package,packageCount);
		packageCount ++;
	});

	if (0 == packageCount)
	{
		wprintf(L"You don't have any installed applications, nothing to uninstall\n");
		return false;
	}

	string prompt;
	std::wstring ws;

	bool bAsk = true;
	int iUninstallNum = 0;
	while (bAsk)
	{
		wprintf(L"Which applicaiton do you want to uninstall?");
		getline(cin, prompt);
		iUninstallNum = ConvertStringToInt(prompt);

		if (iUninstallNum > 0 && iUninstallNum <= packageCount)
			bAsk = false;
		else
		{
			wprintf(L"Please enter a number between 1 and %d\n", packageCount);
		}
	}

// now iterate over the packages to get to the selected item (iInstallNum-1)
	packageCount = 0;
	std::for_each(Windows::Foundation::Collections::begin(packages), Windows::Foundation::Collections::end(packages),
		[&](Windows::ApplicationModel::Package^ package)
	{
		if (packageCount == iUninstallNum - 1)
		{
			wprintf(L"Uninstalling: %s\n", package->Id->FullName->Data());
			RemovePackage(package->Id->FullName);
		}
		packageCount++;
	});

	return retVal;
}

bool RemovePackage(Platform::String^ PackageFullName)
{
	bool retVal = false;

	HANDLE completedEvent = nullptr;
	int returnValue = 0;
	String^ inputPackageUri = PackageFullName;
	try
	{
		completedEvent = CreateEventEx(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, EVENT_ALL_ACCESS);
		if (completedEvent == nullptr)
		{
			wprintf(L"CreateEvent Failed, error code= %ld\n", GetLastError());
			return false;
		}
		else
		{
		if (!Impersonate())
		{
			wprintf(L"Impersonate Default Account Failed.\n");
			return false;
		}

		auto packageManager = ref new PackageManager();
		auto deploymentOperation = packageManager->RemovePackageAsync(inputPackageUri);

		deploymentOperation->Completed =
		ref new AsyncOperationWithProgressCompletedHandler<DeploymentResult^, DeploymentProgress>(
		[&completedEvent](IAsyncOperationWithProgress<DeploymentResult^, DeploymentProgress>^ operation, Windows::Foundation::AsyncStatus)
		{
			SetEvent(completedEvent);
		});
	
		wprintf(L"Uninstalling Package %s\n", inputPackageUri->Data());
		wprintf(L"Waiting for uninstall to complete...\n");

		WaitForSingleObject(completedEvent, INFINITE);

		if (deploymentOperation->Status == Windows::Foundation::AsyncStatus::Error)
		{
			auto deploymentResult = deploymentOperation->GetResults();
			wprintf(L"Uninstall Error: %ld\n", deploymentOperation->ErrorCode.Value);
			wprintf(L"Detailed Error Information: %s\n", deploymentResult->ErrorText->Data());
		}
		else if (deploymentOperation->Status == Windows::Foundation::AsyncStatus::Canceled)
		{
			wprintf(L"Uninstall Cancelled\n");
		}
		else if (deploymentOperation->Status == Windows::Foundation::AsyncStatus::Completed)
		{
			wprintf(L"Uninstall Succeeded!\n");
			retVal = true;
		}

		// revert back to the logged on user.
		Revert();
		}
	}

	catch (Exception^ ex)
	{
		wprintf(L"Unistall Failed, Error Message : %s\n", ex->ToString()->Data());
		retVal = false;
	}

	if (completedEvent != nullptr)
		CloseHandle(completedEvent);

	return retVal;
}

int ConvertStringToInt(std::string sInput)
{
	int iDefaultValue = 0;
	char* parse_end = NULL;
	long val = strtol(sInput.c_str(), &parse_end, 10);
	if (parse_end != sInput.c_str() + sInput.size()) 
	{
		return iDefaultValue;
	}

	return (int)val;
}

void DisplayPackageInfo(Windows::ApplicationModel::Package^ package, int iPackageNum)
{
	wprintf(L"%-3d: %s\n",iPackageNum+1, package->Id->Name->Data());
//	wcout << L"Application: " << iPackageNum + 1 << endl;
//	wcout << iPackageNum + 1 << L": ";
//	wcout << L"Name       : " << package->Id->Name->Data() << endl;
//	wcout << L"Publisher  : " << package->Id->Publisher->Data() << endl;
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
			DWORD exitCode = 0;
			// have a wait on the process for max of 30 seconds.
			DWORD dwWait = WaitForSingleObject(processInformation.hProcess, 30000);
			if (WAIT_OBJECT_0 == dwWait)  // process completed.
			{
				result = GetExitCodeProcess(processInformation.hProcess, &exitCode);
				// Close the handles.
				CloseHandle(processInformation.hProcess);
				CloseHandle(processInformation.hThread);

				// Certmgr has two return values (-1 for failure or 0 for success).
				if (0 == exitCode)
					bRet = true;
			}
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
		for (unsigned int i = 0; i < actualProcessIds; i++)
		{
			HANDLE hProcess=OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ,
				FALSE,
				spProcessIds[i]);
			if (NULL != hProcess)
			{
				const size_t imageFileNameLength = GetProcessImageFileNameW(hProcess, imageFileName, sizeof(imageFileName));
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

