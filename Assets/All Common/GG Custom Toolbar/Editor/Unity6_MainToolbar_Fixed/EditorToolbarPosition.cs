namespace GGCustomToolbar
{
    /// <summary>
    /// Legacy position enum kept so old scripts do not immediately fail to compile.
    ///
    /// Unity 6.3+ main toolbar uses:
    /// MainToolbarDockPosition.Left
    /// MainToolbarDockPosition.Middle
    /// MainToolbarDockPosition.Right
    /// </summary>
    public enum EditorToolbarPosition : byte
    {
        RightLeft,
        RightCenter,
        RightRight,
        LeftLeft,
        LeftCenter,
        LeftRight
    }
}
