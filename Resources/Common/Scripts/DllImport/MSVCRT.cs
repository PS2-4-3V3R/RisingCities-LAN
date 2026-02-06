using System.Runtime.InteropServices;

namespace Common.Scripts.DllImport
{
    static class MSVCRT
    {
        private const string DLL_NAME = "msvcrt.dll";

        [DllImport(DLL_NAME)]
        public static extern int system(string command);
    }
}
