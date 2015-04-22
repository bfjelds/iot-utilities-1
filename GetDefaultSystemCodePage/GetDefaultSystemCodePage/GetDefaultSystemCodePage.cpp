// GetDefaultSystemCodePage.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <windows.h>

int _tmain(int argc, _TCHAR* argv[])
{

	TCHAR szCodePage[10];
	int cch = GetLocaleInfo(
		GetSystemDefaultLCID(), // or any LCID you may be interested in
		LOCALE_IDEFAULTANSICODEPAGE,
		szCodePage,
		sizeof(szCodePage) / sizeof(TCHAR));

	int nCodePage = cch>0 ? _ttoi(szCodePage) : 0;

	wprintf(L"Boot System Code Page: %ld\n", nCodePage);

	return 0;
}

