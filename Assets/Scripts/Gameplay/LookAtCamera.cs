using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] public bool invert;
    public  Transform cameraTransform;

    private void Start()
    {        
        cameraTransform = Camera.main?.transform;
    }

    private void LateUpdate()
    {
        if (invert)
        {
            Vector3 dirToCamera = (cameraTransform.position - transform.position).normalized;
            transform.LookAt(transform.position + dirToCamera * -1);
        }
        else
        {
            transform.LookAt(cameraTransform);
        }
    }
}
