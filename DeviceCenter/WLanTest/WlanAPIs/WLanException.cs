using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.WlanAPIs
{
    public class WLanException : Exception
    {
        public int ErrorCode;
        public string NativeMethod;

        public WLanException(int error, string method)
        {
            ErrorCode = error;
            NativeMethod = method;
        }

        public override string ToString()
        {
            return string.Format("Native function [{0}] failed, error code [{1}]", ErrorCode, NativeMethod);
        }
    }
}
