using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SkyPilot.App.Services;

/// <summary>
/// Paints the Windows title bar dark grey, the colour of the window's top bar, so the window reads as one piece.
/// Windows 10 gets the dark title bar only; older systems keep theirs.
/// </summary>
public static class DarkTitleBar
{
    private const int UseImmersiveDarkMode = 20;
    private const int BorderColor = 34;
    private const int CaptionColor = 35;
    private const int TextColor = 36;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;
        try
        {
            int on = 1;
            DwmSetWindowAttribute(hwnd, UseImmersiveDarkMode, ref on, sizeof(int));
            // COLORREF is 0x00BBGGRR: the Panel grey #242528, its border #36383C and the text #E4E5E7.
            int caption = 0x282524, border = 0x3C3836, text = 0xE7E5E4;
            DwmSetWindowAttribute(hwnd, CaptionColor, ref caption, sizeof(int));
            DwmSetWindowAttribute(hwnd, BorderColor, ref border, sizeof(int));
            DwmSetWindowAttribute(hwnd, TextColor, ref text, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // No desktop window manager: the system title bar stays.
        }
        catch (EntryPointNotFoundException)
        {
        }
    }
}
