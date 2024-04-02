using System.Net.Http;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoMinesweeper;

internal class Control
{
    private const uint CLR_INVALID = 0xFFFFFFFF;

    [SupportedOSPlatform("windows5.0")]
    internal static COLORREF GetPixelColor(HWND hwnd, int x, int y)
    {
        var hdc = PInvoke.GetDC(hwnd);

        var pixel = PInvoke.GetPixel(hdc, x, y);

        _ = PInvoke.ReleaseDC(hwnd, hdc);

        return pixel;
    }

    private static LPARAM MakeLParamFromXY(int x, int y)
    {
        return (y << 16) | x;
    }

    [SupportedOSPlatform("windows5.0")]
    public static void ControlClick(HWND hWnd, int x, int y)
    {
        var lParam = MakeLParamFromXY(x, y);
        _ = PInvoke.PostMessage(hWnd, 513, 0, lParam);
        _ = PInvoke.PostMessage(hWnd, 514, 0, lParam);

        //PInvoke.GetWindowRect(hWnd, out RECT rect);

        //PInvoke.SetCursorPos(rect.left + x, rect.top + y);
        //PInvoke.SendInput([new INPUT
        //{
        //    type = INPUT_TYPE.INPUT_MOUSE,
        //    Anonymous = new()
        //    {
        //        mi = new MOUSEINPUT
        //        {
        //            dx = (int)(x * 65535 / rect.right),
        //            dy = (int)(y * 65535 / rect.bottom),
        //            dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE
        //        }
        //    }
        //}], 1);
    }

    [SupportedOSPlatform("windows5.0")]
    public static void SendKeyDown(HWND hWnd, uint key)
    {
        _ = PInvoke.PostMessage(hWnd, 256, wParam: key, 0);
    }

    [SupportedOSPlatform("windows5.0")]
    public static int GetPixelFromWindow(HWND hWnd, int x, int y)
    {
        var colorRef = GetPixelColor(hWnd, x, y);

        var b = (int)(colorRef & 0x00FF0000) >> 16;
        var g = (int)(colorRef & 0x0000FF00) >> 8;
        var r = (int)(colorRef & 0x000000FF);

        return (r << 16) | (g << 8) | b;
    }

    public static async Task<int> GetPixelFromWindowAsync(
        string SType, string SValue, int X, int Y,
        bool PW = false)
    {
        var result = -1;

        var stringContent = new StringContent( 
            $$"""
            {
                "SType": "{{SType}}",
                "SValue": "{{SValue}}",
                "X": {{X}},
                "Y": {{Y}},
                "PW": {{PW.ToString().ToLowerInvariant()}}
            }
            """);

        using (var client = new HttpClient())
        {
            try
            {
                var response = await client.PostAsync(
                    "http://localhost:2020/getPixelFromWindow", stringContent);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();

                try
                {
                    var m = Regex.Matches(responseString, @"{\""Color\"":\""(\w+)\""}")[0];
                    result = int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                }
                catch
                {
                    return result;
                }
            }
            catch
            {
                return result;
            }
        }
        return result;
    }
}