using UnityEngine;

[DisallowMultipleComponent]
public class MainGunTargetSubSystem : MonoBehaviour
{
    [Header("Наведение")]
    [Tooltip("Целевой bearing для главной цели в градусах 0..360.")]
    [SerializeField] private float targetBearing;

    [Tooltip("Целевая дистанция до главной цели.")]
    [SerializeField] private float targetDistanse;

    private void OnValidate()
    {
        targetBearing = NormalizeBearing(targetBearing);
        targetDistanse = Mathf.Max(0f, targetDistanse);
    }

    public void SetTargetBearing(float bearing)
    {
        targetBearing = NormalizeBearing(bearing);
    }

    public void SetTargetDistanse(float distance)
    {
        targetDistanse = Mathf.Max(0f, distance);
    }

    public float GetTargetBearing()
    {
        return targetBearing;
    }

    public float GetTargetDistanse()
    {
        return targetDistanse;
    }

    private static float NormalizeBearing(float bearing)
    {
        return Mathf.Repeat(bearing, 360f);
    }
}
