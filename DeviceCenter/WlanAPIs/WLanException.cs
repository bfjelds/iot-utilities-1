// Copyright (c) Microsoft. All rights reserved.

using System;

namespace WlanAPIs
{
    public class WLanException : Exception
    {
        public uint ErrorCode;
        public string NativeMethod;

        public WLanException(uint error, string method)
        {
            ErrorCode = error;
            NativeMethod = method;
        }

        public override string ToString()
        {
            return $"Native function [{ErrorCode}] failed, error code [{NativeMethod}]";
        }
    }
}
