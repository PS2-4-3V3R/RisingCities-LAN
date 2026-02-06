//COMMON.RESOURCES.SCRIPTS.VT100 V2.0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Common.Scripts
{
    internal static class VT100
    {
        public static string Normal = "\u001b[0m";
        public static class Color
        {
            public static string Foreground(int r, int g, int b) => $"\u001b[38;2;{r};{g};{b}m";
            public static string Background(int r, int g, int b) => $"\u001b[48;2;{r};{g};{b}m";
            public static class Line
            {
                public static string Foreground(int r, int g, int b) => $"\u001b[38;2;{r};{g};{b}m{Clear.Line}{Normal}";
                public static string Background(int r, int g, int b) => $"\u001b[48;2;{r};{g};{b}m{Clear.Line}{Normal}";
            }
        }
        public static class Move
        {
            public static string To(int x, int y) => $"\u001b[{y};{x}H";
            public static class Relative
            {
                public static string Up(int n) => $"\u001b[{n}A";
                public static string Down(int n) => $"\u001b[{n}B";
                public static string Right(int n) => $"\u001b[{n}C";
                public static string Left(int n) => $"\u001b[{n}D";
            }
        }
        public static class Clear
        {
            public static string LineFromCursorToEnd = "\u001b[0J";
            public static string ScreenFromCursor = "\u001b[0J";
            public static string ScreenToCursor = "\u001b[1J";
            public static string Screen = "\u001b[2J";
            public static string LineFromCursor = "\u001b[0K";
            public static string LineToCursor = "\u001b[1K";
            public static string Line = "\u001b[2K";
        }
    }
}
