namespace System.Windows.Input
{
    /// <summary>
    /// Minimal portable key values used by HELIOS global hotkey registration.
    /// </summary>
    public enum Key
    {
        M = 0x4D,
        OemPlus = 0xBB,
        OemMinus = 0xBD,
        P = 0x50,
        S = 0x53,
        PrintScreen = 0x2C
    }

    /// <summary>
    /// Minimal portable modifier flags used by HELIOS global hotkey registration.
    /// </summary>
    [System.Flags]
    public enum ModifierKeys
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }

    /// <summary>
    /// Converts portable key values to Win32 virtual-key codes.
    /// </summary>
    public static class KeyInterop
    {
        public static int VirtualKeyFromKey(Key key) => (int)key;
    }
}
