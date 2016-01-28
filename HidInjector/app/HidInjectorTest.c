/*++

Copyright (c) Microsoft Corporation.  All rights reserved.

    THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
    PURPOSE.

Module Name:

    testvhid.c

Environment:

    user mode only

Author:

--*/

#include <windows.h>
#include "common.h"
#include "HidDevice.h"
#include "SendInput.h"

BOOLEAN
SendTestInput(
	HANDLE File
	);

//
// Implementation
//
INT __cdecl
main(
    _In_ ULONG argc,
    _In_reads_(argc) PCHAR argv[]
    )
{
    HANDLE file = INVALID_HANDLE_VALUE;
    BOOLEAN found = FALSE;
    BOOLEAN bSuccess = FALSE;


    UNREFERENCED_PARAMETER(argc);
    UNREFERENCED_PARAMETER(argv);

    srand( (unsigned)time( NULL ) );

	found = OpenHidInjectorDevice();
    if (found) {
        printf("...sending control request to our device\n");

		bSuccess = SendTestInput(g_hFile);
		if (bSuccess == FALSE) {
			goto cleanup;
		}

    }
    else {
        printf("Failure: Could not find our HID device \n");
    }

cleanup:

    if (found && bSuccess == FALSE) {
        printf("****** Failure: one or more commands to device failed *******\n");
    }

    if (file != INVALID_HANDLE_VALUE) {
        CloseHandle(file);
    }

    return (bSuccess ? 0 : 1);
}
BOOLEAN SendReport(
	HANDLE File,
	void *Data,
	DWORD  Size
	)
{
	DWORD BytesWritten = 0;

	return WriteFile(
		File,
		Data,
		Size,
		&BytesWritten,
		NULL
		);
}

void SendMousePosition(
	HANDLE File,
	WORD X,
	WORD Y,
	UCHAR Buttons
	)
{
	InjectMouseMove(X * 2, Y * 2, Buttons);
}

void SendRawKey(
	HANDLE File,
	BYTE b
)
{
	HIDINJECTOR_INPUT_REPORT KeyDown = { 0 };
	HIDINJECTOR_INPUT_REPORT KeyUp = { 0 };

	KeyDown.ReportId = KEYBOARD_REPORT_ID;
	KeyDown.Report.KeyReport.Key1 = b;

	KeyUp.ReportId = KEYBOARD_REPORT_ID;

	SendReport(File, &KeyDown, sizeof(KeyDown));
	// Sleep(500);	// May not be necessary
	SendReport(File, &KeyUp, sizeof(KeyUp));

}

void SendKey(
	HANDLE File,
	BYTE Modifiers,
	char Key
	)
{
	HIDINJECTOR_INPUT_REPORT KeyDown = { 0 };
	HIDINJECTOR_INPUT_REPORT KeyUp = { 0 };

	KeyDown.ReportId = KEYBOARD_REPORT_ID;
	KeyDown.Report.KeyReport.Modifiers = Modifiers;
	KeyDown.Report.KeyReport.Key1 = Key - 'a' + 4;

	KeyUp.ReportId = KEYBOARD_REPORT_ID;

	SendReport(File, &KeyDown, sizeof(KeyDown));
	// Sleep(100);	// May not be necessary
	SendReport(File, &KeyUp, sizeof(KeyUp));


}

BOOLEAN
SendTestInput(
	HANDLE File
	)
{
	SendMousePosition(g_hFile, 3200, 800, MOUSEEVENTF_LEFTDOWN);
	SendMousePosition(g_hFile, 3200, 800, MOUSEEVENTF_LEFTUP);
	SendMousePosition(g_hFile, 800, 800, MOUSEEVENTF_LEFTDOWN);
	SendMousePosition(g_hFile, 800, 800, MOUSEEVENTF_LEFTUP);

	SendMousePosition(g_hFile, 3200, 800, MOUSEEVENTF_LEFTDOWN);
	SendMousePosition(g_hFile, 3200, 800, MOUSEEVENTF_LEFTUP);
	SendMousePosition(g_hFile, 800, 800, MOUSEEVENTF_LEFTDOWN);
	SendMousePosition(g_hFile, 800, 800, MOUSEEVENTF_LEFTUP);

	SendMousePosition(g_hFile, 0, 0, 0);
	SendMousePosition(g_hFile, 0x3fff, 0x3fff, 0);

	/*
	for (int i = 0; i < 5; i++)
	{
		InjectKeyDown(VK_RETURN);
		InjectKeyUp(VK_RETURN);
	}
	InjectUnicode('a');
	InjectScanKeyDown(42);
	InjectUnicode('A');
	InjectScanKeyUp(42);
	*/
	/*
	SendRawKey(File, 0x28);
	SendRawKey(File, 0x28);
	SendRawKey(File, 0x28);
	SendRawKey(File, 0x28);
	SendMouseDelta(File, 25, 25);
	SendMouseDelta(File, 25, 25);
	SendMouseDelta(File, 25, 25);
	SendMouseDelta(File, 25, 25);
	SendMouseDelta(File, -25, -25);
	SendMouseDelta(File, -25, -25);
	SendMouseDelta(File, -25, -25);
	SendMouseDelta(File, -25, -25);
	*/
	return TRUE;
}
 
