using UnityEngine;

public class SpectateSystem : MonoBehaviour
{
    [SerializeField] private Camera spectateCamera;

    private Transform currentTarget;

    public void StartSpectating(Transform target)
    {
        currentTarget = target;

        spectateCamera.gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (currentTarget == null)
            return;

        transform.position = currentTarget.position;
        transform.rotation = currentTarget.rotation;
    }

    public void StopSpectating()
    {
        currentTarget = null;
        spectateCamera.gameObject.SetActive(false);
    }
}