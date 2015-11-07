// Copyright (c) Microsoft. All rights reserved.

using System;

namespace WlanAPIs
{
    public class WLanException : Exception
    {
        public const uint ERROR_IPROUTINGTABLE_REMOVE_FAILED = 0x80071001;

        public uint ErrorCode;
        public string NativeMethod;

        public WLanException(uint error, string method)
        {
            ErrorCode = error;
            NativeMethod = method;
        }

        public override string ToString()
        {
            return $"Native function [{NativeMethod}] failed, error code [0x{ErrorCode:x}]";
        }
    }
}
