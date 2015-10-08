#include "stdafx.h"

#include "util.h"

PWSTR ConvertCStrToWStr(PCSTR str)
{
    if (!str)
    {
        return NULL;
    }

    PWSTR ret;
    size_t str_size_in_words = strlen(str) + 1;
    size_t str_size = (str_size_in_words)*sizeof(WCHAR);
    size_t converted;

    ret = (PWSTR)malloc(str_size);

    if (!ret)
    {
        return NULL;
    }

    if (ERROR_SUCCESS == mbstowcs_s(&converted, ret, str_size_in_words, str, _TRUNCATE))
    {
        return ret;
    }

    return NULL;
}

PSTR ConvertWStrToCStr(PCWSTR str)
{
    if (!str)
    {
        return NULL;
    }

    PSTR ret;
    size_t str_size = (wcslen(str) + 1)*sizeof(CHAR);
    size_t converted;

    ret = (PSTR)malloc(str_size);

    if (!ret)
    {
        return NULL;
    }

    if (ERROR_SUCCESS == wcstombs_s(&converted, ret, str_size, str, _TRUNCATE))
    {
        return ret;
    }

    return NULL;
}

BOOL StartsWithAJ_(PCSTR str)
{
    if (!str)
    {
        return FALSE;
    }

    int count = 0;

    while (*str && count < 4)
    {
        count++;
        str++;
    }

    // ensures the string is at least 3 chars long
    if (count >= 3)
    {
        // If the string is not 3 chars long go back with the pointer
        // to cover cases where the string is 3 chars long as well
        // as other cases
        if (count > 3)
        {
            str--;
        }

        if (*(str - 3) == 'A' && *(str - 2) == 'J' && *(str - 1) == '_')
        {
            return TRUE;
        }
    }

    return FALSE;
}

PWSTR CipherToString(DOT11_CIPHER_ALGORITHM alg)
{
    switch (alg)
    {
    case DOT11_CIPHER_ALGO_NONE:
        return L"none";
    case DOT11_CIPHER_ALGO_WEP:
    case DOT11_CIPHER_ALGO_WEP104:
    case DOT11_CIPHER_ALGO_WEP40:
        return L"WEP";
    case DOT11_CIPHER_ALGO_TKIP:
        return L"TKIP";
    case DOT11_CIPHER_ALGO_CCMP:
        return L"AES";
    default:
        return L"undefined";
    }
}

PWSTR AuthToString(DOT11_AUTH_ALGORITHM alg)
{
    switch (alg)
    {
    case DOT11_AUTH_ALGO_80211_OPEN:
        return L"open";
    case DOT11_AUTH_ALGO_80211_SHARED_KEY:
        return L"shared";
    case DOT11_AUTH_ALGO_WPA:
        return L"WPA";
    case DOT11_AUTH_ALGO_WPA_PSK:
        return L"WPAPSK";
    case DOT11_AUTH_ALGO_RSNA:
        return L"WPA2";
    case DOT11_AUTH_ALGO_RSNA_PSK:
        return L"WPA2PSK";
    default:
        return L"undefined";
    }
}

HRESULT inline HResultFromQStatus(QStatus status)
{
    switch (status)
    {
    case ER_OK:
        return S_OK;
    case ER_BAD_ARG_1:
    case ER_BAD_ARG_2:
    case ER_BAD_ARG_3:
    case ER_BAD_ARG_4:
    case ER_BAD_ARG_5:
    case ER_BAD_ARG_6:
    case ER_BAD_ARG_7:
    case ER_BAD_ARG_8:
        return E_INVALIDARG;
    case ER_INVALID_ADDRESS:
        return E_POINTER;
    default:
        return E_FAIL;
    }
}
