using System;
using System.Runtime.InteropServices;

namespace Common.Scripts.DllImport
{
    static class KERNEL32
    {
        private const string DLL_NAME = "kernel32.dll";

        [DllImport(DLL_NAME, SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport(DLL_NAME, SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport(DLL_NAME, SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

        [DllImport(DLL_NAME, SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

        [DllImport(DLL_NAME)]
        public static extern IntPtr GetConsoleWindow();
    }
}