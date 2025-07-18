using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [Header("Camera Objects")]
    public GameObject firstPersonCamera;
    public GameObject thirdPersonCamera;

    private bool isFirstPerson = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Start in first-person mode by default
        SwitchToFirstPerson();
    }

    void Update()
    {
        // Switch camera mode when 'C' key is pressed
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            ToggleCamera();
        }
    }

    public void ToggleCamera()
    {
        isFirstPerson = !isFirstPerson;
        if (isFirstPerson)
        {
            SwitchToFirstPerson();
        }
        else
        {
            SwitchToThirdPerson();
        }
    }

    public void SwitchToFirstPerson()
    {
        isFirstPerson = true;
        if (firstPersonCamera != null) firstPersonCamera.SetActive(true);
        if (thirdPersonCamera != null) thirdPersonCamera.SetActive(false);
    }

    public void SwitchToThirdPerson()
    {
        isFirstPerson = false;
        if (firstPersonCamera != null) firstPersonCamera.SetActive(false);
        if (thirdPersonCamera != null) thirdPersonCamera.SetActive(true);
    }
}