#pragma once

PWSTR ConvertCStrToWStr(PCSTR str);
PSTR ConvertWStrToCStr(PCWSTR str);
BOOL StartsWithAJ_(PCSTR str);
PWSTR CipherToString(DOT11_CIPHER_ALGORITHM alg);
PWSTR AuthToString(DOT11_AUTH_ALGORITHM alg);

HRESULT inline HResultFromQStatus(QStatus status);

#define CIPHER_STRING_MAX_LENGTH 9
#define AUTH_STRING_MAX_LENGTH 9
