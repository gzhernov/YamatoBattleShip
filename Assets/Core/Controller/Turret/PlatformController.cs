using UnityEngine;

[DisallowMultipleComponent]
public class TurrentPlatformController : MonoBehaviour
{
    [Header("Поворот платформы")]
    [Tooltip("Объект, который нужно поворачивать вокруг оси Y.")]
    [SerializeField] private Transform platformTransform;

    [Tooltip("Скорость поворота в градусах в секунду.")]
    [SerializeField] private float rotationSpeedDegreesPerSecond = 30f;

    [Tooltip("Допуск по углу, при котором наведение считается завершённым.")]
    [SerializeField] private float toleranceDegrees = 0.5f;

    [SerializeField] private float desiredCourse;
    [SerializeField] private bool hasCourseCommand;

    private void OnValidate()
    {
        rotationSpeedDegreesPerSecond = Mathf.Max(0f, rotationSpeedDegreesPerSecond);
        toleranceDegrees = Mathf.Max(0.01f, toleranceDegrees);
        desiredCourse = NormalizeCourse(desiredCourse);
    }

    private void Update()
    {
        UpdateCourseCommand();
    }

    private void UpdateCourseCommand()
    {
        if (!hasCourseCommand || platformTransform == null)
        {
            return;
        }

        if (IsAimed())
        {
            hasCourseCommand = false;
            return;
        }

        Quaternion targetRotation = Quaternion.Euler(0f, desiredCourse, 0f);
        platformTransform.rotation = Quaternion.RotateTowards(
            platformTransform.rotation,
            targetRotation,
            rotationSpeedDegreesPerSecond * Time.deltaTime);

        if (IsAimed())
        {
            hasCourseCommand = false;
        }
    }

    public void SetCourse(float course)
    {
        desiredCourse = NormalizeCourse(course);
        hasCourseCommand = true;
    }

    public void ClearCourseCommand()
    {
        hasCourseCommand = false;
    }

    public bool HasCourseCommand()
    {
        return hasCourseCommand;
    }

    public bool IsAimed()
    {
        if (platformTransform == null)
        {
            return false;
        }

        float angleDelta = Mathf.Abs(Mathf.DeltaAngle(GetCurrentCourse(), desiredCourse));
        return angleDelta <= toleranceDegrees;
    }

    public float GetCurrentCourse()
    {
        if (platformTransform == null)
        {
            return 0f;
        }

        return NormalizeCourse(platformTransform.rotation.eulerAngles.y);
    }

    public float GetDesiredCourse()
    {
        return desiredCourse;
    }

    private static float NormalizeCourse(float course)
    {
        return Mathf.Repeat(course, 360f);
    }
}
