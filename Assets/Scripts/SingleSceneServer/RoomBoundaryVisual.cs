// RoomBoundaryVisual.cs
using UnityEngine;

public class RoomBoundaryVisual : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color boundaryColor = Color.green;
    public float fadeSpeed = 0.5f;
    public float pulseSpeed = 0.5f;

    private LineRenderer lineRenderer;
    private float pulseTimer;

    private void Start()
    {
        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        // Create a rectangle boundary
        Vector3[] points = new Vector3[5];
        float halfSize = 1f;
        
        points[0] = new Vector3(-halfSize, 0.1f, -halfSize);
        points[1] = new Vector3(halfSize, 0.1f, -halfSize);
        points[2] = new Vector3(halfSize, 0.1f, halfSize);
        points[3] = new Vector3(-halfSize, 0.1f, halfSize);
        points[4] = new Vector3(-halfSize, 0.1f, -halfSize);

        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = boundaryColor;
        lineRenderer.endColor = boundaryColor;
    }

    private void Update()
    {
        // Pulse animation
        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulse = Mathf.Sin(pulseTimer) * 0.5f + 0.5f;
        
        Color color = boundaryColor;
        color.a = 0.3f + pulse * 0.7f;
        
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}