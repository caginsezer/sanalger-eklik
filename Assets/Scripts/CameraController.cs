using UnityEngine;

/// <summary>
/// Kamera kontrolcüsü - Mevcut kamerayı kullanır
/// Portrait ekrana uygun, Mouse + Mobil desteği
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Kamera Ayarları")]
    public float distance = 9f; // Ekranın tam sığdığından %100 emin olmak için sabitledik
    public float height = 8f;
    public float rotationSpeed = 100f;
    public float zoomSpeed = 0.5f;
    public float minZoom = 2f;
    public float maxZoom = 10f;
    public float smoothSpeed = 10f;

    private float currentAngle = 0f;
    private float currentZoom;
    private Camera cam;
    private float lastTouchDistance = 0f;

    public void SetupExistingCamera(Camera existingCam)
    {
        cam = existingCam;
        // DİKKAT: Sahyedeki (Scene) varsayılan zoom yerine Kod'daki distance değerini kullanmaya zorluyoruz:
        currentZoom = distance;
            
        UpdateCameraPosition();
    }

    public Camera SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.15f, 0.12f, 0.1f);
        cam.fieldOfView = 50f;
        currentZoom = distance;
        UpdateCameraPosition();
        return cam;
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Editor'de her zaman mouse kullan
        bool useTouchInput = false;
        #if !UNITY_EDITOR
        if (Application.isMobilePlatform && Input.touchCount > 0)
            useTouchInput = true;
        #endif

        if (useTouchInput)
        {
            HandleTouchCamera();
        }
        else
        {
            HandleMouseCamera();
        }

        UpdateCameraPosition();
    }

    private void HandleMouseCamera()
    {
        if (Input.GetMouseButton(1))
        {
            float horizontalInput = Input.GetAxis("Mouse X");
            currentAngle += horizontalInput * rotationSpeed * Time.deltaTime;
        }

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            currentZoom -= scrollInput * zoomSpeed * 20f;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }
    }

    private void HandleTouchCamera()
    {
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            float touchDistance = Vector2.Distance(touch0.position, touch1.position);
            if (lastTouchDistance > 0)
            {
                float delta = lastTouchDistance - touchDistance;
                currentZoom += delta * 0.02f;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }
            lastTouchDistance = touchDistance;

            Vector2 avgDelta = (touch0.deltaPosition + touch1.deltaPosition) / 2f;
            currentAngle += avgDelta.x * 0.3f;
        }
        else
        {
            lastTouchDistance = 0;
        }
    }

    private void UpdateCameraPosition()
    {
        if (cam == null) return;

        // TAM TEPEDEN ORTHOGRAPHIC - Hocanın referansı gibi
        cam.orthographic = true;
        cam.orthographicSize = currentZoom;
        cam.transform.position = new Vector3(0, 15f, 0);
        cam.transform.rotation = Quaternion.Euler(90, 0, 0);
    }
}
