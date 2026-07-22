using UnityEngine;

[DisallowMultipleComponent]
public class MainGunTargetSubSystem : MonoBehaviour
{
    [Header("Наведение")]
    [Tooltip("Целевой bearing для главной цели в градусах 0..360.")]
    [SerializeField] private float targetBearing;

    [Tooltip("Целевая дистанция до главной цели.")]
    [SerializeField] private float targetDistanse;

    [Tooltip("Целевой курс главной цели в градусах 0..360.")]
    [SerializeField] private float targetCourse;

    [Tooltip("Целевая скорость главной цели.")]
    [SerializeField] private float targetSpeed;

    private void OnValidate()
    {
        targetBearing = NormalizeBearing(targetBearing);
        targetDistanse = Mathf.Max(0f, targetDistanse);
        targetCourse = NormalizeBearing(targetCourse);
        targetSpeed = Mathf.Max(0f, targetSpeed);
    }

    public void SetTargetBearing(float bearing)
    {
        targetBearing = NormalizeBearing(bearing);
    }

    public void SetTargetDistanse(float distance)
    {
        targetDistanse = Mathf.Max(0f, distance);
    }

    public void SetTargetCourse(float course)
    {
        targetCourse = NormalizeBearing(course);
    }

    public void SetTargetSpeed(float speed)
    {
        targetSpeed = Mathf.Max(0f, speed);
    }

    public float GetTargetBearing()
    {
        return targetBearing;
    }

    public float GetTargetDistanse()
    {
        return targetDistanse;
    }

    public float GetTargetCourse()
    {
        return targetCourse;
    }

    public float GetTargetSpeed()
    {
        return targetSpeed;
    }

    private static float NormalizeBearing(float bearing)
    {
        return Mathf.Repeat(bearing, 360f);
    }
}
