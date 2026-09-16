using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class SpectateSystem : NetworkBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera spectateCamera;

    [Header("Free Roam")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float boostSpeed = 15f;

    private float pitch;
    private float yaw;
    private bool isSpectating;

    private void Start()
    {
        if (spectateCamera != null)
        {
            spectateCamera.enabled = false;

            if (spectateCamera.TryGetComponent(out AudioListener listener))
                listener.enabled = false;
        }
    }

    public void SetLocalPlayerCamera(Camera camera)
    {
        playerCamera = camera;
    }

    private void Update()
    {
        if (!isSpectating)
            return;

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * lookSensitivity;
        pitch -= mouseY * lookSensitivity;

        pitch = Mathf.Clamp(pitch, -89f, 89f);

        spectateCamera.transform.rotation =
            Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction =
            spectateCamera.transform.right * horizontal +
            spectateCamera.transform.forward * vertical;

        if (Input.GetKey(KeyCode.E))
            direction += Vector3.up;

        if (Input.GetKey(KeyCode.Q))
            direction += Vector3.down;

        float speed =
            Input.GetKey(KeyCode.LeftShift)
                ? boostSpeed
                : moveSpeed;

        spectateCamera.transform.position +=
            direction.normalized * speed * Time.deltaTime;
    }

    public void StartSpectating()
    {
        if (isSpectating)
            return;

        isSpectating = true;

        SetCameraActive(playerCamera, false);
        SetCameraActive(spectateCamera, true);

        if (spectateCamera != null)
        {
            Vector3 angles = spectateCamera.transform.eulerAngles;

            yaw = angles.y;
            pitch = angles.x;

            if (pitch > 180f)
                pitch -= 360f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void StopSpectating()
    {
        if (!isSpectating)
            return;

        isSpectating = false;

        SetCameraActive(spectateCamera, false);
        SetCameraActive(playerCamera, true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetCameraActive(Camera camera, bool active)
    {
        if (camera == null)
            return;

        camera.enabled = active;

        if (camera.TryGetComponent(out AudioListener listener))
            listener.enabled = active;
    }

    [Server]
    public void ServerBeginSpectating(NetworkConnection connection)
    {
        TargetBeginSpectating(connection);
    }

    [TargetRpc]
    private void TargetBeginSpectating(NetworkConnection connection)
    {
        StartSpectating();
    }
}