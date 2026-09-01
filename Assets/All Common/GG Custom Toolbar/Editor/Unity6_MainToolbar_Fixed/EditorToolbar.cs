using UnityEditor;
using UnityEditor.Toolbars;

namespace GGCustomToolbar
{
    /// <summary>
    /// Compatibility helper for the old custom-toolbar package.
    ///
    /// The old implementation injected VisualElements into UnityEditor.Toolbar
    /// using reflection. That code has intentionally been removed.
    ///
    /// Unity 6.3+ now provides MainToolbarElementAttribute and MainToolbar APIs.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorToolbar
    {
        static EditorToolbar()
        {
            // Nothing needs to be injected here anymore.
            // Unity discovers [MainToolbarElement] methods automatically.
        }

        public static void Refresh(string elementId)
        {
            MainToolbar.Refresh(elementId);
        }
    }
}
