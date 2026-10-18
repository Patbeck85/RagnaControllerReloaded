namespace RagnaController.Core
{
    /// <summary>
    /// Callback-Signatur for Win32 EnumWindows (TASK-1602).
    /// Is from INativeMethods, NativeMethods and NativeMethodsImpl konsistent verwendet.
    /// </summary>
    public delegate bool EnumWindowsProcDelegate(IntPtr hWnd, IntPtr lParam);
}
