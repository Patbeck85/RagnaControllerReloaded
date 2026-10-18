namespace RagnaController.Core
{
    /// <summary>
    /// Callback-Signatur für Win32 EnumWindows (TASK-1602).
    /// Wird von INativeMethods, NativeMethods und NativeMethodsImpl konsistent verwendet.
    /// </summary>
    public delegate bool EnumWindowsProcDelegate(IntPtr hWnd, IntPtr lParam);
}
