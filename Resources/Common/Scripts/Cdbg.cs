//COMMON.RESOURCES.SCRIPTS.CDBG V3.1 (07042025PM1106)
#pragma warning disable CS8618
using System.Diagnostics;
using Common.Scripts.DllImport;

namespace Common.Scripts
{
    internal class Cdbg
    {
        static string key;
        static int messageCount = 1;
        const int SWP_NOZORDER = 0x0004;
        const int SWP_NOSIZE = 0x0001;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const int SW_SHOWMAXIMIZED = 3;

        /// <summary>
        /// Activa la consola de depuración. !Solo funciona en Windows y no se puede cerrar hasta que no se cierre la aplicación!
        /// </summary>
        public static void EnableConsole()
        {
            if ((Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "-dbg") || true)
            {
                KERNEL32.AllocConsole();
                EnableVT100();
                SetConsolePosition(-1800, 250);
                Thread.Sleep(10);
            }
        }
        public static void SetConsolePosition(int x, int y)
        {
            IntPtr console = KERNEL32.GetConsoleWindow();
            USER32.SetWindowPos(console, HWND_TOPMOST, x, y, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
            USER32.ShowWindow(console, SW_SHOWMAXIMIZED);
        }
        public static void EnableVT100()
        {
            const int ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
            const int STD_OUTPUT_HANDLE = -11;

            IntPtr handle = KERNEL32.GetStdHandle(STD_OUTPUT_HANDLE);

            // Activamos VT100 si obtenemos correctamente el modo de consola
            if (KERNEL32.GetConsoleMode(handle, out int mode))
            {
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                KERNEL32.SetConsoleMode(handle, mode);
            }
        }

        private static void Log(string level, string levelColor, string messageColor, string path, string message)
        {
            if (string.IsNullOrEmpty(path))
            {
                // Obtener el marco de la pila de llamadas (caller)
                var stackFrame = new StackTrace(2, true).GetFrame(0); // "1" omite este método Debug

                if (stackFrame != null)
                {
                    // Nombre del archivo y número de línea
                    string fileName = stackFrame.GetFileName() ?? "Unknown File";
                    int lineNumber = stackFrame.GetFileLineNumber();

                    // Crear el path de ubicación
                    path = $"{fileName}:{lineNumber}";
                }
                else
                {
                    path = "Unknown Location";
                }
            }
            //if (level == "DEBUG") return;
            if (key == $"{level}-{path}-{message}")
            {
                messageCount++;
                Console.WriteLine($"{VT100.Move.Relative.Up(1)}{levelColor}[{level}] {VT100.Color.Foreground(0, 255, 255)}{path} {VT100.Normal}=> {messageColor}{message} {VT100.Color.Foreground(144, 144, 144)}(x{messageCount}){VT100.Normal}");
            }
            else
            {
                key = $"{level}-{path}-{message}";
                messageCount = 1;
                Console.WriteLine($"{levelColor}[{level}] {VT100.Color.Foreground(0, 255, 255)}{path} {VT100.Normal}=> {messageColor}{message}{VT100.Normal}");
            }
        }

        public static void Debug(string message, string path = "")
        {
            Log("DEBUG", VT100.Color.Foreground(255, 140, 0), VT100.Color.Foreground(255, 140, 0), path, message);
        }
        public static void Rem(string message, string path = "")
        {
            Log("REM", VT100.Color.Foreground(0, 128, 0), VT100.Color.Foreground(0, 128, 0), path, message);
        }
        public static void Info(string message, string path = "")
        {
            Log("INFO", VT100.Color.Foreground(128, 128, 255), VT100.Color.Foreground(0, 255, 0), path, message);
        }

        public static void Warning(string message, string path = "")
        {
            Log("WARNING", VT100.Color.Foreground(255, 255, 0), VT100.Color.Foreground(255, 255, 0), path, message);
        }

        public static void Error(string message, string path = "")
        {
            Log("ERROR", VT100.Color.Foreground(255, 0, 0), VT100.Color.Foreground(255, 0, 0), path, message);
        }

        public class Task
        {
            private List<string> _message = new List<string>();
            public List<string> stateOf = new List<string>();
            private void EnsureCapacity(int step)
            {
                while (_message.Count <= step) _message.Add("");
                while (stateOf.Count <= step) stateOf.Add("");
            }
            public void Start(int step, string message)
            {
                EnsureCapacity(step);
                _message[step] = message;
                stateOf[step] = "Execute";
                Console.WriteLine($"{VT100.Color.Foreground(255, 255, 0)}[Execute] {message}{VT100.Normal}");
            }
            public void Error(int step, string message)
            {
                EnsureCapacity(step);
                stateOf[step] = "Error";
                Console.WriteLine($"{VT100.Color.Foreground(255, 0, 0)}{VT100.Move.Relative.Up(1)}{VT100.Clear.Line}[Errored] {_message[step]} => {message}{VT100.Normal}");
            }
            public void End(int step)
            {
                EnsureCapacity(step);
                stateOf[step] = "Done";
                Console.WriteLine($"{VT100.Color.Foreground(0, 255, 0)}{VT100.Move.Relative.Up(1)}{VT100.Clear.Line}[Done] {_message[step]}{VT100.Normal}");
            }
        }
    }
}
