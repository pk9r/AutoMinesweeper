using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoMinesweeper
{
    public class Control
    {
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        public static IntPtr MakeLParamFromXY(int x, int y)
        {
            return (IntPtr)((y << 16) | x);
        }
        public static IntPtr FindWindowHandle(string className, string windowName)
        {
            return FindWindow(className, windowName);
        }

        public static RECT GetWindowRect(IntPtr hWnd)
        {
            RECT lpRect = default(RECT);
            GetWindowRect(hWnd, ref lpRect);
            return lpRect;
        }

        public static void ControlClick(IntPtr controlHandle, int x, int y)
        {
            IntPtr lParam = MakeLParamFromXY(x, y);
            PostMessage(controlHandle, 513, new IntPtr(0), lParam);
            PostMessage(controlHandle, 514, new IntPtr(0), lParam);
        }

        public static void SendKeyBoardDown(IntPtr handle, int key)
        {
            PostMessage(handle, 256, new IntPtr((int)key), new IntPtr(0));
        }
        public static int GetPixelFromWindow(string SType, string SValue, int X, int Y, bool PW = false)
        {
            int result = -1;
            using (WebClient client = new WebClient())
            {
                try
                {
                    string response = client.UploadString(
                        "http://localhost:2020/getPixelFromWindow",
                        "POST",
                        "{" +
                            $"\n\t\"SType\": \"{SType}\"," +
                            $"\n\t\"SValue\": \"{SValue}\"," +
                            $"\n\t\"X\": {X}," +
                            $"\n\t\"Y\": {Y}," +
                            $"\n\t\"PW\": {PW.ToString().ToLower()}" +
                        "}"
                    );
                    try
                    {
                        string pattern = @"{\""Color\"":\""(\w+)\""}";
                        Match m = Regex.Matches(response, pattern)[0];
                        result = Int32.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                    }
                    catch
                    {
                        return result;
                    }
                }
                catch (WebException)
                {
                    return result;
                }
            }
            return result;
        }
    }
}
