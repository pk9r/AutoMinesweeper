using System.Runtime.Versioning;
using AutoMinesweeper.Abstractions;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoMinesweeper.Infrastructure;
internal class NativeMethodsService : IControlWindowService<HWND>, IPixelFromWindowService<HWND>
{
    private const uint CLR_INVALID = 0xFFFFFFFF;
    private const uint WM_LBUTTONDOWN = 513;
    private const uint WM_LBUTTONUP = 514;
    private const uint WM_KEYDOWN = 256;

    [SupportedOSPlatform("windows5.0")]
    public void ClickToWindow(HWND hWnd, int x, int y)
    {
        var lParam = MakeLParamFromXY(x, y);
        PInvoke.PostMessage(hWnd, WM_LBUTTONDOWN, 0, lParam);
        PInvoke.PostMessage(hWnd, WM_LBUTTONUP, 0, lParam);
    }

    [SupportedOSPlatform("windows5.0")]
    public void SendKeyToWindow(HWND hWnd, uint key)
    {
        PInvoke.PostMessage(hWnd, WM_KEYDOWN, wParam: key, 0);
    }

    [SupportedOSPlatform("windows5.0")]
    public int GetPixelFromWindow(HWND hWnd, int x, int y)
    {
        var colorRef = GetPixelColor(hWnd, x, y);

        var b = (int)(colorRef & 0x00FF0000) >> 16;
        var g = (int)(colorRef & 0x0000FF00) >> 8;
        var r = (int)(colorRef & 0x000000FF);

        return (r << 16) | (g << 8) | b;
    }

    [SupportedOSPlatform("windows5.0")]
    private static COLORREF GetPixelColor(HWND hwnd, int x, int y)
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
}
