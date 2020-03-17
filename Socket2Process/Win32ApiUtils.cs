using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Socket2Process
{
    public static class Win32ApiUtils
    {
        public static string LaseError()
        {
            int errorCode = Marshal.GetLastWin32Error();
            return $"({errorCode}) {new Win32Exception(errorCode).Message}";
        }
    }
}
