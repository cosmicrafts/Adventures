using UnityEngine;
using Cinemachine;

public class CinemachineZoom : MonoBehaviour
{
    private CinemachineVirtualCamera virtualCamera;

    [Header("Camera Zoom Settings")]
    public float zoomSmoothSpeed = 5f; // Smooth transition speed between zoom levels
    public float[] zoomLevels = { 8f, 12f, 18f, 24f, 28f, 32f, 36f }; // Predefined zoom levels
    private int currentZoomIndex; // The current zoom level index

    private void Start()
    {
        // Get the Cinemachine Virtual Camera component
        virtualCamera = GetComponent<CinemachineVirtualCamera>();

        if (virtualCamera != null)
        {
            // Initialize the zoom level to the closest matching level to the current orthographic size
            currentZoomIndex = GetClosestZoomIndex(virtualCamera.m_Lens.OrthographicSize);
        }
    }

    private void Update()
    {
        // Handle Zoom Input and Zoom Update
        HandleCameraZoomInput();
        HandleCameraZoom();
    }

    private void HandleCameraZoomInput()
    {
        // Handle mouse scroll for zooming (desktop)
        float zoomScrollInput = Input.GetAxis("Mouse ScrollWheel"); // Get scroll input

        // Detect if there's a scroll input
        if (zoomScrollInput > 0 && currentZoomIndex > 0)
        {
            currentZoomIndex--;
        }
        else if (zoomScrollInput < 0 && currentZoomIndex < zoomLevels.Length - 1)
        {
            currentZoomIndex++;
        }
    }

    private void HandleCameraZoom()
    {
        // Smoothly interpolate the camera's orthographic size to the target zoom level
        float targetZoom = zoomLevels[currentZoomIndex];
        virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(virtualCamera.m_Lens.OrthographicSize, targetZoom, Time.deltaTime * zoomSmoothSpeed);
    }

    private int GetClosestZoomIndex(float currentZoom)
    {
        int closestIndex = 0;
        float closestDifference = Mathf.Abs(currentZoom - zoomLevels[0]);

        for (int i = 1; i < zoomLevels.Length; i++)
        {
            float difference = Mathf.Abs(currentZoom - zoomLevels[i]);
            if (difference < closestDifference)
            {
                closestDifference = difference;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}
