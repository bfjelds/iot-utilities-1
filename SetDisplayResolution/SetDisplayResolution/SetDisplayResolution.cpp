// SetDisplayResolution.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>

void ShowUsage();
void ShowCurrentResolution();
void SetDisplayResolution(int Width, int Height);
bool GetDisplayName_and_DevMode(DISPLAY_DEVICE *pdisplayDevice, DEVMODEW *devMode);

int _tmain(int argc, _TCHAR* argv[])
{
	if (argc == 1) // no parameters
	{
		ShowCurrentResolution();
		return 0;
	}

	if (argc != 3)
	{
		ShowUsage();
		return 0;
	}

	int Width = _wtoi(argv[1]);
	int Height = _wtoi(argv[2]);

	if (Width == 0 || Height == 0)
	{
		wprintf(L"Width or Height don't appear to be valid\n");
		return 0;
	}

	SetDisplayResolution(Width, Height);

	return 0;
}

void ShowUsage()
{
	wprintf(L"SetDisplayResolution\n\n");
	wprintf(L"Usage:\n");
	wprintf(L"SetDisplayResolution (with no parameters - displays current resolution)\n");
	wprintf(L"SetDisplayResolution <width> <height> (sets new width/height resolution)\n\n");
}

void SetDisplayResolution(int Width, int Height)
{
	DEVMODEW devMode = { 0 };
	DISPLAY_DEVICEW pdisplayDevice = { 0 };

	if (GetDisplayName_and_DevMode(&pdisplayDevice, &devMode))
	{
		if (Width == devMode.dmPelsWidth && Height == devMode.dmPelsWidth)
		{
			wprintf(L"Height and Width match the current display resolution\n");
			wprintf(L"Nothing for me to do...\n");
			return;
		}

		// Set new resolution here...
		devMode.dmPelsWidth = Width;
		devMode.dmPelsHeight = Height;
		devMode.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;
		LONG lRet=ChangeDisplaySettingsExW(pdisplayDevice.DeviceName, &devMode, NULL, CDS_UPDATEREGISTRY, NULL);
		switch (lRet)
		{
		case DISP_CHANGE_SUCCESSFUL:
			wprintf(L"Welcome to your new resolution! %dx%d\n",Width,Height);
				break;
		case DISP_CHANGE_BADFLAGS:
			wprintf(L"BAD_FLAGS\n");
			break;
		case DISP_CHANGE_BADMODE:
			wprintf(L"BAD_MODE\n");
			break;
		case DISP_CHANGE_BADPARAM:
			wprintf(L"BAD_PARAM\n");
			break;
		case DISP_CHANGE_FAILED:
			wprintf(L"Hmm, I failed to set the new display mode\n");
			break;
		case DISP_CHANGE_RESTART:
			wprintf(L"Change successful, you now need to restart to get the new resolution\n");
			wprintf(L"Hint: 'shutdown -r -t 0'\n");
			break;
		default:
			break;
		}
	}
}

bool GetDisplayName_and_DevMode(DISPLAY_DEVICE *pdisplayDevice, DEVMODEW *devMode)
{
	bool bRet = false;

	if (pdisplayDevice == NULL || devMode == NULL)	// not initialized - return.
		return bRet;

	ZeroMemory(pdisplayDevice, sizeof(DISPLAY_DEVICE));
	ZeroMemory(devMode, sizeof(DEVMODEW));

	devMode->dmSize = sizeof(DEVMODEW);
	pdisplayDevice->cb = sizeof(DISPLAY_DEVICEW);

	if (EnumDisplayDevicesW(NULL, 0, pdisplayDevice, EDD_GET_DEVICE_INTERFACE_NAME))
	{
		if (EnumDisplaySettingsExW(pdisplayDevice->DeviceName, ENUM_CURRENT_SETTINGS, devMode, EDS_RAWMODE))
		{
			bRet = true;
		}
	}

	return bRet;
}

void ShowCurrentResolution()
{
	// we will cheat and assume that we only have one display adapter :)
	// perhaps this is something to fixup later. (much later).
	DEVMODEW devMode = { 0 };
	DISPLAY_DEVICEW pdisplayDevice = { 0 };

	if (GetDisplayName_and_DevMode(&pdisplayDevice, &devMode))
	{
		wprintf(L"Current Display Resolution:\n");
		wprintf(L"Width      : %ld\n", devMode.dmPelsWidth);
		wprintf(L"Height     : %ld\n", devMode.dmPelsHeight);
		wprintf(L"Frq Hz     : %ld\n", devMode.dmDisplayFrequency);
		wprintf(L"Orientation: ");
		switch (devMode.dmOrientation)
		{
		case DMDO_DEFAULT:
			wprintf(L"Default");
			break;
		case DMDO_90:
			wprintf(L"Rotated 90 Degrees");
			break;
		case DMDO_180:
			wprintf(L"Rotated 180 Degrees");
			break;
		case DMDO_270:
			wprintf(L"Rotated 270 Degrees");
			break;
		default:
			break;
		}
		wprintf(L"\n");
	}
	else
		wprintf(L"Failed to get display information\n");
}
