using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlatformController : MonoBehaviour
{
    [Header("Поворот платформы")]
    [Tooltip("Объект, который нужно поворачивать вокруг оси Y.")]
    [SerializeField] private Transform platformTransform;

    [Tooltip("Скорость поворота в градусах в секунду.")]
    [SerializeField] private float rotationSpeedDegreesPerSecond = 30f;

    [Tooltip("Допуск по углу, при котором наведение считается завершённым.")]
    [SerializeField] private float toleranceDegrees = 0.5f;

    [Header("Наведение")]
    [Tooltip("Целевой bearing для связанных башен. Значение сохраняется отдельно от курса платформы.")]
    [SerializeField] private float targetBearing;

    [Tooltip("Целевая дистанция до цели. Значение задаётся из UI панели дистанции.")]
    [SerializeField] private float targetDistanse;

    [Tooltip("Список башен, которым делегируется команда Aim.")]
    [SerializeField] private List<TurretWithCannons> turrets = new List<TurretWithCannons>();

    [Tooltip("Автоматически добавлять найденные дочерние TurretWithCannons в список без дубликатов.")]
    [SerializeField] private bool autoFindTurrets = true;

    [SerializeField] private float desiredCourse;
    [SerializeField] private bool hasCourseCommand;

    private void OnValidate()
    {
        rotationSpeedDegreesPerSecond = Mathf.Max(0f, rotationSpeedDegreesPerSecond);
        toleranceDegrees = Mathf.Max(0.01f, toleranceDegrees);
        targetBearing = NormalizeCourse(targetBearing);
        targetDistanse = Mathf.Max(0f, targetDistanse);
        desiredCourse = NormalizeCourse(desiredCourse);

        if (autoFindTurrets)
        {
            FindTurretsInChildren();
        }
    }

    private void Update()
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

    public void SetTargetBearing(float bearing)
    {
        targetBearing = NormalizeCourse(bearing);
    }

    public void SetTargetDistanse(float distance)
    {
        targetDistanse = Mathf.Max(0f, distance);
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

    public float GetTargetBearing()
    {
        return targetBearing;
    }

    public float GetTargetDistanse()
    {
        return targetDistanse;
    }

    public void Aim(float bearing, float elevation)
    {
        if (autoFindTurrets)
        {
            FindTurretsInChildren();
        }

        if (turrets == null || turrets.Count == 0)
        {
            Debug.LogWarning("PlatformController: список TurretWithCannons пуст, делегировать Aim некому.", this);
            return;
        }

        bool hasValidTurret = false;

        for (int i = 0; i < turrets.Count; i++)
        {
            TurretWithCannons turret = turrets[i];
            if (turret == null)
            {
                continue;
            }

            hasValidTurret = true;
            turret.Aim(bearing, elevation);
        }

        if (!hasValidTurret)
        {
            Debug.LogWarning("PlatformController: в списке TurretWithCannons нет валидных ссылок для делегирования Aim.", this);
        }
    }

    private void FindTurretsInChildren()
    {
        TurretWithCannons[] foundTurrets = GetComponentsInChildren<TurretWithCannons>(true);
        if (foundTurrets.Length == 0)
        {
            return;
        }

        if (turrets == null)
        {
            turrets = new List<TurretWithCannons>();
        }

        for (int i = 0; i < foundTurrets.Length; i++)
        {
            TurretWithCannons turret = foundTurrets[i];
            if (turret == null || turrets.Contains(turret))
            {
                continue;
            }

            turrets.Add(turret);
        }
    }

    private static float NormalizeCourse(float course)
    {
        return Mathf.Repeat(course, 360f);
    }
}
