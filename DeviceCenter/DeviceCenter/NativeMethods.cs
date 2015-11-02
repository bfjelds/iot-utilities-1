using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DeviceCenter
{
    internal class NativeMethods
    {
        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_DLGMODALFRAME = 0x0001;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_FRAMECHANGED = 0x0020;

        public static void HideSystemMenu(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        public const UInt32 CTRL_C_EVENT = 0;
        public const UInt32 CTRL_BREAK_EVENT = 1;

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

        public delegate void AddDeviceCallbackDelegate(
          [MarshalAs(UnmanagedType.LPWStr)]string deviceName,
          [MarshalAs(UnmanagedType.LPWStr)]string ipV4Address,
          [MarshalAs(UnmanagedType.LPWStr)]string txtParameters);

        [DllImport("DeviceDiscovery.dll", ExactSpelling = true, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartDiscovery();

        [DllImport("DeviceDiscovery.dll", ExactSpelling = true, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StopDiscovery();

        [DllImport("DeviceDiscovery.dll", ExactSpelling = true, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterCallback(AddDeviceCallbackDelegate cbFunction);
    }
}
