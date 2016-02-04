/*++

Copyright (c) Microsoft Corporation.  All rights reserved.

    THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
    PURPOSE.

Module Name:

    common.h

Environment:

    User mode

--*/

#pragma once

#include <pshpack1.h>

//
// input from device to system
//
typedef struct _HIDINJECTOR_INPUT_REPORT {
    unsigned char ReportId;   

	union {
		struct {
			UCHAR  Modifiers;
			UCHAR Key1;
			UCHAR Key2;
			UCHAR Key3;
			UCHAR Key4;
		} KeyReport;
		struct {
			UCHAR Buttons;
			SHORT AbsoluteX;
			SHORT AbsoluteY;
 		} MouseReport;
	} Report;
} HIDINJECTOR_INPUT_REPORT, *PHIDINJECTOR_INPUT_REPORT;

// Values for ReportId
#define KEYBOARD_REPORT_ID		1
#define MOUSE_REPORT_ID			2

// Values for KeyReport.Modifiers
#define KEBBOARD_LEFT_CONTROL	0x01
#define KEYBOARD_LEFT_SHIFT		0x02
#define KEYBOARD_LEFT_ALT		0x04
#define KEYBOARD_LEFT_GUI		0x08
#define KEBBOARD_RIGHT_CONTROL	0x10
#define KEYBOARD_RIGHT_SHIFT	0x20
#define KEYBOARD_RIGHT_ALT		0x40
#define KEYBOARD_RIGHT_GUI		0x80

// Values for MouseReport.Buttons
#define MOUSE_BUTTON_1			0x01
#define MOUSE_BUTTON_2			0x02
#define MOUSE_BUTTON_3			0x04

#include <poppack.h>

