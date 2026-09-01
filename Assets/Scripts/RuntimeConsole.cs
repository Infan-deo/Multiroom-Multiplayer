using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RuntimeConsole : MonoBehaviour
{
    [Header("Console")]
    [SerializeField] private GameObject consolePanel;
    [SerializeField] private TMP_Text consoleText;

    [Header("Input")]
    [SerializeField] private InputActionReference consoleAction;
    [SerializeField] private InputActionReference clearConsoleAction;

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;

        if (consoleAction != null)
            consoleAction.action.Enable();
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;

        if (consoleAction != null)
            consoleAction.action.Disable();
    }

    private void Update()
    {
        if (consoleAction != null &&
            consoleAction.action.WasPressedThisFrame())
        {
            bool newState = !consolePanel.activeSelf;

            consolePanel.SetActive(newState);

            Debug.Log($"Console: {newState}");
        }
        if (clearConsoleAction != null &&
            clearConsoleAction.action.WasPressedThisFrame())
        {
            consoleText.text = "Chat Cleared";
        }
    }

    private void HandleLog(
        string logString,
        string stackTrace,
        LogType type)
    {
        if (consoleText == null)
            return;

        consoleText.text += $"[{type}] {logString}\n";
        consoleText.text += $"Press P to close, C to Clear\n";
    }
}