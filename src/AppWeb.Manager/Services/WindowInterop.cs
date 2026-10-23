using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace AppWeb.Manager.Services;

internal static class WindowInterop
{
    private const uint MbIconError = 0x00000010;

    public static void SetAppUserModelId(string appUserModelId)
    {
        _ = SetCurrentProcessExplicitAppUserModelID(appUserModelId);
    }

    public static void SetIcon(Window window, string? iconPath)
    {
        if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
        {
            window.AppWindow.SetIcon(iconPath);
        }
    }

    public static void BringToForeground(Window window)
    {
        var windowHandle = WindowNative.GetWindowHandle(window);
        _ = ShowWindowAsync(windowHandle, 9);
        _ = SetForegroundWindow(windowHandle);
    }

    public static void ShowError(string message)
    {
        _ = MessageBoxW(IntPtr.Zero, message, "AppWeb", MbIconError);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appUserModelId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr window, string text, string caption, uint type);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr window, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);
}
