using Gameplay;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    public InputManager inputManager;

    private void Update()
    {
        if (inputManager.GetPauseInput())
        {
            ToggleSettings();
        }
    }

    private void ToggleSettings()
    {
        bool isOpen = !settingsPanel.activeSelf;

        settingsPanel.SetActive(isOpen);

        if (isOpen)
        {
            // Settings open → mouse available
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Settings closed → mouse controls camera
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}