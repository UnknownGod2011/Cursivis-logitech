using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cursivis.Companion.Services;

public sealed class GlobalMouseWheelService : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmMouseWheel = 0x020A;
    private const int WmMouseHWheel = 0x020E;
    private readonly LowLevelMouseProc _hookProc;
    private IntPtr _hookHandle;

    public GlobalMouseWheelService()
    {
        _hookProc = HookCallback;
    }

    public event EventHandler<GlobalMouseWheelEventArgs>? WheelMoved;

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var moduleHandle = module is null ? IntPtr.Zero : GetModuleHandle(module.ModuleName);
        _hookHandle = SetWindowsHookEx(WhMouseLl, _hookProc, moduleHandle, 0);
        if (_hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to install the global mouse wheel hook.");
        }
    }

    public void Dispose()
    {
        if (_hookHandle == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && (wParam == (IntPtr)WmMouseWheel || wParam == (IntPtr)WmMouseHWheel))
        {
            var payload = Marshal.PtrToStructure<MsllHookStruct>(lParam);
            var delta = (short)((payload.mouseData >> 16) & 0xffff);
            if (delta != 0)
            {
                var args = new GlobalMouseWheelEventArgs(
                    delta > 0 ? 1 : -1,
                    wParam == (IntPtr)WmMouseHWheel ? MouseWheelAxis.Horizontal : MouseWheelAxis.Vertical);
                WheelMoved?.Invoke(this, args);
                if (args.Handled)
                {
                    return new IntPtr(1);
                }
            }
        }

        return CallNextHookEx(_hookHandle, code, wParam, lParam);
    }

    private delegate IntPtr LowLevelMouseProc(int code, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct PointStruct
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsllHookStruct
    {
        public PointStruct pt;
        public int mouseData;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}

public enum MouseWheelAxis
{
    Vertical = 0,
    Horizontal = 1
}

public sealed class GlobalMouseWheelEventArgs(int deltaStep, MouseWheelAxis axis) : EventArgs
{
    public int DeltaStep { get; } = deltaStep;

    public MouseWheelAxis Axis { get; } = axis;

    public bool Handled { get; set; }
}
