using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KeysVisorOverlay.Services;

public sealed class NativeInputHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int WmMButtonDown = 0x0207;
    private const int WmMButtonUp = 0x0208;
    private const int WmXButtonDown = 0x020B;
    private const int WmXButtonUp = 0x020C;
    private const int XButton1 = 1;
    private const int XButton2 = 2;

    private readonly HookProc keyboardProc;
    private readonly HookProc mouseProc;
    private IntPtr keyboardHook;
    private IntPtr mouseHook;
    private bool disposed;

    public NativeInputHook()
    {
        keyboardProc = OnKeyboardHook;
        mouseProc = OnMouseHook;
    }

    public event Action<int>? KeyDown;
    public event Action<int>? KeyUp;
    public event Action<int>? MouseDown;
    public event Action<int>? MouseUp;

    public void Start()
    {
        if (keyboardHook != IntPtr.Zero || mouseHook != IntPtr.Zero)
        {
            return;
        }

        IntPtr moduleHandle = GetCurrentModuleHandle();
        keyboardHook = SetWindowsHookEx(WhKeyboardLl, keyboardProc, moduleHandle, 0);
        if (keyboardHook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not install the keyboard hook.");
        }

        mouseHook = SetWindowsHookEx(WhMouseLl, mouseProc, moduleHandle, 0);
        if (mouseHook == IntPtr.Zero)
        {
            UnhookWindowsHookEx(keyboardHook);
            keyboardHook = IntPtr.Zero;
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not install the mouse hook.");
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(keyboardHook);
            keyboardHook = IntPtr.Zero;
        }

        if (mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHook);
            mouseHook = IntPtr.Zero;
        }
    }

    private IntPtr OnKeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int message = wParam.ToInt32();
            KbdLlHookStruct data = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);

            if (message is WmKeyDown or WmSysKeyDown)
            {
                KeyDown?.Invoke(data.VkCode);
            }
            else if (message is WmKeyUp or WmSysKeyUp)
            {
                KeyUp?.Invoke(data.VkCode);
            }
        }

        return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr OnMouseHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int message = wParam.ToInt32();
            MsLlHookStruct data = Marshal.PtrToStructure<MsLlHookStruct>(lParam);
            int? code = GetMouseCode(message, data.MouseData);

            if (code is int inputCode)
            {
                if (message is WmLButtonDown or WmRButtonDown or WmMButtonDown or WmXButtonDown)
                {
                    MouseDown?.Invoke(inputCode);
                }
                else
                {
                    MouseUp?.Invoke(inputCode);
                }
            }
        }

        return CallNextHookEx(mouseHook, nCode, wParam, lParam);
    }

    private static int? GetMouseCode(int message, uint mouseData)
    {
        return message switch
        {
            WmLButtonDown or WmLButtonUp => InputCodes.MouseLeft,
            WmRButtonDown or WmRButtonUp => InputCodes.MouseRight,
            WmMButtonDown or WmMButtonUp => InputCodes.MouseMiddle,
            WmXButtonDown or WmXButtonUp => GetHighWord(mouseData) switch
            {
                XButton1 => InputCodes.MouseX1,
                XButton2 => InputCodes.MouseX2,
                _ => null
            },
            _ => null
        };
    }

    private static int GetHighWord(uint value)
    {
        return (int)((value >> 16) & 0xFFFF);
    }

    private static IntPtr GetCurrentModuleHandle()
    {
        using Process process = Process.GetCurrentProcess();
        using ProcessModule? module = process.MainModule;
        return module?.ModuleName is string moduleName ? GetModuleHandle(moduleName) : IntPtr.Zero;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public int VkCode;
        public int ScanCode;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsLlHookStruct
    {
        public Point Pt;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
