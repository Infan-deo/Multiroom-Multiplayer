using UnityEngine;
using UnityEngine.InputSystem;

public class RuntimeConsole : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference consoleAction;
    [SerializeField] private InputActionReference clearConsoleAction;

    private string consoleText = "";
    private bool consoleVisible;

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;

        consoleAction?.action.Enable();
        clearConsoleAction?.action.Enable();
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;

        consoleAction?.action.Disable();
        clearConsoleAction?.action.Disable();
    }

    private void Update()
    {
        if (consoleAction != null &&
            consoleAction.action.WasPressedThisFrame())
        {
            consoleVisible = !consoleVisible;
        }

        if (clearConsoleAction != null &&
            clearConsoleAction.action.WasPressedThisFrame())
        {
            consoleText = "Console Cleared\n";
        }
    }

    private void HandleLog(
        string logString,
        string stackTrace,
        LogType type)
    {
        consoleText += $"[{type}] {logString}\n";
    }

    private void OnGUI()
    {
        if (!consoleVisible)
            return;

        GUI.Box(
            new Rect(
                10,
                10,
                Screen.width - 20,
                Screen.height - 20
            ),
            "Runtime Console"
        );

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            fontSize = 14
        };

        GUI.Label(
            new Rect(
                20,
                45,
                Screen.width - 40,
                Screen.height - 80
            ),
            consoleText,
            style
        );

        GUI.Label(
            new Rect(
                20,
                Screen.height - 30,
                Screen.width - 40,
                20
            ),
            "Press P to close, C to Clear"
        );
    }
}