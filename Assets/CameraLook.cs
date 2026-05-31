using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraLook : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Объект, который поворачивается по горизонтали (влево/вправо). Обычно это игрок/корабль/пивот.")]
    public Transform yawTarget;

    [Tooltip("Объект, который поворачивается по вертикали (вверх/вниз). Обычно это сама камера или её пивот.")]
    public Transform pitchTarget;

    [Header("Settings")]
    [Min(0.01f)]
    public float sensitivity = 2.0f;

    public bool invertY = false;

    [Tooltip("Ограничение по вертикали (в градусах).")]
    public float minPitch = -80f;

    [Tooltip("Ограничение по вертикали (в градусах).")]
    public float maxPitch = 80f;

    [Header("Cursor")]
    public bool lockCursorOnStart = true;
    public KeyCode toggleCursorKey = KeyCode.Escape;

    private float _pitch;

    private void Reset()
    {
        yawTarget = transform.parent != null ? transform.parent : transform;
        pitchTarget = transform;
    }

    private void Awake()
    {
        if (yawTarget == null) yawTarget = transform.parent != null ? transform.parent : transform;
        if (pitchTarget == null) pitchTarget = transform;

        _pitch = GetPitchFromTransform(pitchTarget);
        _pitch = ClampPitch(_pitch);
        SetPitchOnTransform(pitchTarget, _pitch);
    }

    private void Start()
    {
        if (lockCursorOnStart) SetCursorLocked(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleCursorKey))
        {
            bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
            SetCursorLocked(shouldLock);
        }

        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * sensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivity;

        yawTarget.Rotate(0f, mouseX, 0f, Space.Self);

        float y = invertY ? mouseY : -mouseY;
        _pitch = ClampPitch(_pitch + y);
        SetPitchOnTransform(pitchTarget, _pitch);
    }

    private float ClampPitch(float value)
    {
        return Mathf.Clamp(value, minPitch, maxPitch);
    }

    private static float GetPitchFromTransform(Transform t)
    {
        float pitch = t.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        return pitch;
    }

    private static void SetPitchOnTransform(Transform t, float pitch)
    {
        Vector3 e = t.localEulerAngles;
        e.x = pitch;
        t.localEulerAngles = e;
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}

