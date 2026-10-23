using System.Runtime.InteropServices;
using System.Text;
using AppWeb.Core.Models;
using AppWeb.Core.Services;
using AppWeb.Core.Validation;

namespace AppWeb.Infrastructure.Shortcuts;

public sealed class ShellLinkService(AppPaths paths) : IShortcutService
{
    private const int ClsCtxInprocServer = 1;
    private const ushort VtLpwstr = 31;
    private const uint ShcneAssocChanged = 0x08000000;

    private static readonly Guid ShellLinkClsid = new("00021401-0000-0000-C000-000000000046");
    private static readonly Guid ShellLinkIid = new("000214F9-0000-0000-C000-000000000046");
    private static readonly PropertyKey AppUserModelIdKey = new(
        new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        5);

    public Task<string> SyncAsync(
        WebAppDefinition app,
        string? previousShortcutPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(app);

        var shortcutPath = BuildShortcutPath(app, previousShortcutPath);
        CreateShortcut(shortcutPath, app);

        if (!string.IsNullOrWhiteSpace(previousShortcutPath) &&
            !string.Equals(previousShortcutPath, shortcutPath, StringComparison.OrdinalIgnoreCase) &&
            File.Exists(previousShortcutPath))
        {
            File.Delete(previousShortcutPath);
        }

        SHChangeNotify(ShcneAssocChanged, 0, IntPtr.Zero, IntPtr.Zero);
        return Task.FromResult(shortcutPath);
    }

    public void Delete(string? shortcutPath)
    {
        if (!string.IsNullOrWhiteSpace(shortcutPath) && File.Exists(shortcutPath))
        {
            File.Delete(shortcutPath);
            SHChangeNotify(ShcneAssocChanged, 0, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private string BuildShortcutPath(WebAppDefinition app, string? previousShortcutPath)
    {
        Directory.CreateDirectory(paths.StartMenuDirectory);

        var baseName = FileNamePolicy.Sanitize(app.Name);
        var candidate = Path.Combine(paths.StartMenuDirectory, $"{baseName}.lnk");

        if (!string.IsNullOrWhiteSpace(previousShortcutPath) &&
            string.Equals(previousShortcutPath, candidate, StringComparison.OrdinalIgnoreCase))
        {
            return candidate;
        }

        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var suffix = app.Id.ToString("N")[..8];
        candidate = Path.Combine(paths.StartMenuDirectory, $"{baseName} ({suffix}).lnk");

        var counter = 2;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(paths.StartMenuDirectory, $"{baseName} ({suffix}-{counter}).lnk");
            counter++;
        }

        return candidate;
    }

    private static void CreateShortcut(string shortcutPath, WebAppDefinition app)
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("Não foi possível localizar o executável do AppWeb.");
        }

        var shellLink = CreateComObject<IShellLinkW>(ShellLinkClsid, ShellLinkIid);
        IPersistFile? persistFile = null;
        IPropertyStore? propertyStore = null;

        try
        {
            shellLink.SetPath(executablePath);
            shellLink.SetArguments($"--webapp {app.Id:D}");
            shellLink.SetDescription(
                string.IsNullOrWhiteSpace(app.Description) ? app.Name : app.Description);
            shellLink.SetWorkingDirectory(Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory);
            shellLink.SetShowCmd(1);

            if (!string.IsNullOrWhiteSpace(app.IconPath) && File.Exists(app.IconPath))
            {
                shellLink.SetIconLocation(app.IconPath, 0);
            }

            persistFile = (IPersistFile)shellLink;
            propertyStore = (IPropertyStore)shellLink;
            SetAppUserModelId(propertyStore, AppIdentity.GetWebAppAumid(app.Id));
            persistFile.Save(shortcutPath, remember: true);
        }
        finally
        {
            ReleaseComObject(propertyStore);
            ReleaseComObject(persistFile);
            ReleaseComObject(shellLink);
        }
    }

    private static void SetAppUserModelId(IPropertyStore propertyStore, string appUserModelId)
    {
        var key = AppUserModelIdKey;
        var value = new PropVariant
        {
            Vt = VtLpwstr,
            PointerValue = Marshal.StringToCoTaskMemUni(appUserModelId)
        };

        try
        {
            ThrowIfFailed(propertyStore.SetValue(ref key, ref value));
            ThrowIfFailed(propertyStore.Commit());
        }
        finally
        {
            Marshal.FreeCoTaskMem(value.PointerValue);
        }
    }

    private static T CreateComObject<T>(Guid classId, Guid interfaceId)
    {
        var classIdLocal = classId;
        var interfaceIdLocal = interfaceId;
        var result = CoCreateInstance(
            ref classIdLocal,
            IntPtr.Zero,
            ClsCtxInprocServer,
            ref interfaceIdLocal,
            out var instance);

        ThrowIfFailed(result);
        return (T)instance;
    }

    private static void ThrowIfFailed(int hresult)
    {
        if (hresult < 0)
        {
            Marshal.ThrowExceptionForHR(hresult);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CoCreateInstance(
        ref Guid classId,
        IntPtr outer,
        int classContext,
        ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out object instance);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder filePath,
            int maxPath,
            IntPtr findData,
            uint flags);

        void GetIDList(out IntPtr itemIdList);
        void SetIDList(IntPtr itemIdList);

        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, int maxName);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);

        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder directory, int maxDirectory);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);

        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int maxArguments);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);

        void GetHotkey(out short hotkey);
        void SetHotkey(short hotkey);
        void GetShowCmd(out int showCommand);
        void SetShowCmd(int showCommand);

        void GetIconLocation(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder iconPath,
            int maxIconPath,
            out int iconIndex);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int iconIndex);

        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string relativePath, uint reserved);

        void Resolve(IntPtr window, uint flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string filePath);
    }

    [ComImport]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid classId);

        [PreserveSig]
        int IsDirty();

        void Load([MarshalAs(UnmanagedType.LPWStr)] string fileName, uint mode);

        void Save([MarshalAs(UnmanagedType.LPWStr)] string? fileName, [MarshalAs(UnmanagedType.Bool)] bool remember);

        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string fileName);
        void GetCurFile(out IntPtr fileName);
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig]
        int GetCount(out uint count);

        [PreserveSig]
        int GetAt(uint index, out PropertyKey key);

        [PreserveSig]
        int GetValue(ref PropertyKey key, out PropVariant value);

        [PreserveSig]
        int SetValue(ref PropertyKey key, ref PropVariant value);

        [PreserveSig]
        int Commit();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private readonly struct PropertyKey(Guid formatId, uint propertyId)
    {
        public readonly Guid FormatId = formatId;
        public readonly uint PropertyId = propertyId;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)]
        public ushort Vt;

        [FieldOffset(8)]
        public IntPtr PointerValue;
    }
}
