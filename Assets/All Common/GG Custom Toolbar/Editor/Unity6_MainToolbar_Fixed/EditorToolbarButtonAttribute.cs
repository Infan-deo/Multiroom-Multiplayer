using System;

namespace GGCustomToolbar
{
    /// <summary>
    /// Kept only for source compatibility with older scripts.
    ///
    /// Do not use this attribute for Unity 6 main-toolbar elements.
    /// Use UnityEditor.Toolbars.MainToolbarElementAttribute instead.
    /// </summary>
    [Obsolete(
        "EditorToolbarButtonAttribute is obsolete. " +
        "Use UnityEditor.Toolbars.MainToolbarElementAttribute.")]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EditorToolbarButtonAttribute : Attribute
    {
        public string IconName { get; }
        public string Tooltip { get; }
        public int Priority { get; }
        public EditorToolbarPosition Position { get; }
        public bool DisableOnPlayMode { get; }

        public EditorToolbarButtonAttribute(
            string iconName,
            string tooltip = "",
            int priority = 0,
            EditorToolbarPosition position = EditorToolbarPosition.LeftLeft,
            bool disableOnPlayMode = false)
        {
            IconName = iconName;
            Tooltip = tooltip;
            Priority = priority;
            Position = position;
            DisableOnPlayMode = disableOnPlayMode;
        }
    }
}
