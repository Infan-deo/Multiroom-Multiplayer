using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    public class InputManager : MonoBehaviour
    {
        public InputActionReference pauseButton;

        private void Start()
        {
            if (pauseButton != null)
                pauseButton.action.Enable();
        }

        public bool GetPauseInput()
        {
            return pauseButton.action.WasPressedThisFrame();
        }
    }
}
