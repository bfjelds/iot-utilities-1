// SetPassword.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

/*++

Module Name:

SetPassword.cpp

Username is argv[1]
new password is argv[2]
old password is argv[3]. This allows non-admin password changes.

Note that admin or account operator privilege is required on the
target machine unless argv[3] is present and represents the correct
current password.

NetUserSetInfo() at info-level 1003 is appropriate for administrative
override of an existing password.

NetUserChangePassword() allows for an arbitrary user to override
an existing password providing that the current password is confirmed.

Link with netapi32.lib

--*/

#include <windows.h>
#include <stdio.h>

#include <lm.h>

#define RTN_OK 0
#define RTN_USAGE 1
#define RTN_ERROR 13

void
DisplayErrorText(
DWORD dwLastError
);

// 
// Unicode entry point and argv
// 

int _tmain(int argc, _TCHAR* argv[])
{
	LPWSTR          wUserName;
	LPWSTR          wComputerName = NULL; // default to local machine
	LPWSTR          wOldPassword;
	LPWSTR          wNewPassword;
	NET_API_STATUS  nas;

	if (argc != 4)
	{
		fprintf(stderr, "Usage: <username> <new_password> <old_password>\n");
		return RTN_USAGE;
	}

	// 
	// set command line arguments
	// 

	wUserName = argv[1];
	wNewPassword = argv[2];
	wOldPassword = argv[3];

	// 
	// allows user to change their own password
	// 

	nas = NetUserChangePassword(
		wComputerName,	// NULL for this machine
		wUserName,
		wOldPassword,
		wNewPassword
		);

	if (nas != NERR_Success) {
		DisplayErrorText(nas);
		return RTN_ERROR;
	}
	else
	{
		wprintf(L"Password has been changed\n");
	}

	return RTN_OK;
}

void
DisplayErrorText(
DWORD dwLastError
)
{
	HMODULE hModule = NULL; // default to system source
	LPSTR MessageBuffer;
	DWORD dwBufferLength;
	DWORD dwFormatFlags;

	dwFormatFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER |
		FORMAT_MESSAGE_IGNORE_INSERTS |
		FORMAT_MESSAGE_FROM_SYSTEM;

	// 
	// if dwLastError is in the network range, load the message source
	// 
	if (dwLastError >= NERR_BASE && dwLastError <= MAX_NERR) {
		hModule = LoadLibraryEx(
			TEXT("netmsg.dll"),
			NULL,
			LOAD_LIBRARY_AS_DATAFILE
			);

		if (hModule != NULL)
			dwFormatFlags |= FORMAT_MESSAGE_FROM_HMODULE;
	}

	// 
	// call FormatMessage() to allow for message text to be acquired
	// from the system or the supplied module handle.
	// 
	if (dwBufferLength = FormatMessageA(
		dwFormatFlags,
		hModule, // module to get message from (NULL == system)
		dwLastError,
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), // default language
		(LPSTR)&MessageBuffer,
		0,
		NULL
		))
	{
		DWORD dwBytesWritten;

		// 
		// Output message string on stderr
		// 
		WriteFile(
			GetStdHandle(STD_ERROR_HANDLE),
			MessageBuffer,
			dwBufferLength,
			&dwBytesWritten,
			NULL
			);

		// 
		// free the buffer allocated by the system
		// 
		LocalFree(MessageBuffer);
	}

	// 
	// if you loaded a message source, unload it.
	// 
	if (hModule != NULL)
		FreeLibrary(hModule);
}
