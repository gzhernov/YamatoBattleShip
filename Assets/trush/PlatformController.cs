using System.Collections.Generic;
using UnityEngine;
[DisallowMultipleComponent]
public class PlatformController : MonoBehaviour
{
    private enum SalvoFireLoopState
    {
        Idle,
        Aiming,
        AimCooldown,
        WaitingTurretsReady
    }

    [Header("Поворот платформы")]
    [Tooltip("Объект, который нужно поворачивать вокруг оси Y.")]
    [SerializeField] private Transform platformTransform;

    [Tooltip("Скорость поворота в градусах в секунду.")]
    [SerializeField] private float rotationSpeedDegreesPerSecond = 30f;

    [Tooltip("Допуск по углу, при котором наведение считается завершённым.")]
    [SerializeField] private float toleranceDegrees = 0.5f;

    [Header("Наведение")]
    [Tooltip("Подсистема, в которой хранится состояние главной цели.")]
    [SerializeField] private MainGunTargetSubSystem mainGunTargetSubSystem;

    [Tooltip("Список башен, которым делегируется команда Aim.")]
    [SerializeField] private List<TurretWithCannons> turrets = new List<TurretWithCannons>();

    [Tooltip("Автоматически добавлять найденные дочерние TurretWithCannons в список без дубликатов.")]
    [SerializeField] private bool autoFindTurrets = true;

    [Header("Залповый огонь")]
    [Tooltip("Включает автоматический цикл: наведение, короткая пауза, залп, ожидание готовности башен.")]
    [SerializeField] private bool autoSalvoFireEnabled;

    [Tooltip("Пауза после завершения наведения перед началом залпа.")]
    [SerializeField] private float aimCooldownSeconds = 0.25f;

    [Tooltip("Минимальный расчётный угол возвышения орудий главного калибра.")]
    [SerializeField] private float minMainGunElevation = -5f;

    [Tooltip("Максимальный расчётный угол возвышения орудий главного калибра.")]
    [SerializeField] private float maxMainGunElevation = 45f;

    [Tooltip("Текущее состояние автоматического цикла залпового огня.")]
    [SerializeField] private SalvoFireLoopState salvoFireLoopState = SalvoFireLoopState.Idle;

    [SerializeField] private float desiredCourse;
    [SerializeField] private bool hasCourseCommand;

    private float salvoStateTimer;
    private bool aimCommandIssued;

    private void OnValidate()
    {
        rotationSpeedDegreesPerSecond = Mathf.Max(0f, rotationSpeedDegreesPerSecond);
        toleranceDegrees = Mathf.Max(0.01f, toleranceDegrees);
        aimCooldownSeconds = Mathf.Max(0f, aimCooldownSeconds);
        if (maxMainGunElevation < minMainGunElevation)
        {
            maxMainGunElevation = minMainGunElevation;
        }

        desiredCourse = NormalizeCourse(desiredCourse);
        TryAssignMainGunTargetSubSystem();

        if (autoFindTurrets)
        {
            FindTurretsInChildren();
        }
    }

    private void Awake()
    {
        TryAssignMainGunTargetSubSystem();
    }

    private void Update()
    {
        UpdateCourseCommand();
        UpdateAutoSalvoFire();
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

    public float GetTargetBearing()
    {
        if (mainGunTargetSubSystem == null)
        {
            Debug.LogError("PlatformController: не назначена ссылка на MainGunTargetSubSystem.", this);
            return 0f;
        }

        return mainGunTargetSubSystem.GetTargetBearing();
    }

    public float GetTargetDistanse()
    {
        if (mainGunTargetSubSystem == null)
        {
            Debug.LogError("PlatformController: не назначена ссылка на MainGunTargetSubSystem.", this);
            return 0f;
        }

        return mainGunTargetSubSystem.GetTargetDistanse();
    }

    public void StartAutoSalvoFire()
    {
        // TryAssignMainGunTargetSubSystem();

        if (autoFindTurrets)
        {
            FindTurretsInChildren();
        }

        if (mainGunTargetSubSystem == null)
        {
            Debug.LogError("PlatformController: невозможно запустить автоогонь, не назначена ссылка на MainGunTargetSubSystem.", this);
            return;
        }

        if (!HasAnyValidTurret())
        {
            Debug.LogWarning("PlatformController: невозможно запустить автоогонь, нет валидных башен.", this);
            return;
        }

        autoSalvoFireEnabled = true;
        aimCommandIssued = false;
        salvoStateTimer = 0f;
        salvoFireLoopState = SalvoFireLoopState.Aiming;
    }

    public void StopAutoSalvoFire()
    {
        autoSalvoFireEnabled = false;
        aimCommandIssued = false;
        salvoStateTimer = 0f;
        salvoFireLoopState = SalvoFireLoopState.Idle;
    }

    public bool IsAutoSalvoFireActive()
    {
        return autoSalvoFireEnabled;
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

    public bool TryStartSalvo()
    {
        if (autoFindTurrets)
        {
            FindTurretsInChildren();
        }

        if (turrets == null || turrets.Count == 0)
        {
            Debug.LogWarning("PlatformController: список TurretWithCannons пуст, делегировать команду залпа некому.", this);
            return false;
        }

        bool hasValidTurret = false;
        bool anySalvoStarted = false;

        for (int i = 0; i < turrets.Count; i++)
        {
            TurretWithCannons turret = turrets[i];
            if (turret == null)
            {
                continue;
            }

            hasValidTurret = true;

            if (turret.TryStartSalvo())
            {
                anySalvoStarted = true;
            }
        }

        if (!hasValidTurret)
        {
            Debug.LogWarning("PlatformController: в списке TurretWithCannons нет валидных ссылок для делегирования залпа.", this);
            return false;
        }

        return anySalvoStarted;
    }

    private void UpdateAutoSalvoFire()
    {
        if (!autoSalvoFireEnabled)
        {
            return;
        }

        if (mainGunTargetSubSystem == null)
        {
            Debug.LogError("PlatformController: автоогонь остановлен, не назначена ссылка на MainGunTargetSubSystem.", this);
            StopAutoSalvoFire();
            return;
        }

        if (autoFindTurrets)
        {
            FindTurretsInChildren();
        }

        if (!HasAnyValidTurret())
        {
            Debug.LogWarning("PlatformController: автоогонь остановлен, нет валидных башен.", this);
            StopAutoSalvoFire();
            return;
        }

        switch (salvoFireLoopState)
        {
            case SalvoFireLoopState.Idle:
                salvoFireLoopState = SalvoFireLoopState.Aiming;
                break;

            case SalvoFireLoopState.Aiming:
                UpdateAutoSalvoAiming();
                break;

            case SalvoFireLoopState.AimCooldown:
                UpdateAutoSalvoCooldown();
                break;

            case SalvoFireLoopState.WaitingTurretsReady:
                UpdateAutoSalvoWaitingTurretsReady();
                break;
        }
    }

    private void UpdateAutoSalvoAiming()
    {
        if (!aimCommandIssued)
        {
            float bearing = mainGunTargetSubSystem.GetTargetBearing();
            float distanceKilometers = mainGunTargetSubSystem.GetTargetDistanse();
            float elevation = CalculateTargetElevation(distanceKilometers);

            Aim(bearing, elevation);
            aimCommandIssued = true;
        }

        if (!AreAllTurretsAimed())
        {
            return;
        }

        salvoStateTimer = aimCooldownSeconds;
        salvoFireLoopState = SalvoFireLoopState.AimCooldown;
    }

    private void UpdateAutoSalvoCooldown()
    {
        salvoStateTimer -= Time.deltaTime;
        if (salvoStateTimer > 0f)
        {
            return;
        }

        if (TryStartSalvo())
        {
            salvoFireLoopState = SalvoFireLoopState.WaitingTurretsReady;
            return;
        }

        aimCommandIssued = false;
        salvoFireLoopState = SalvoFireLoopState.Aiming;
    }

    private void UpdateAutoSalvoWaitingTurretsReady()
    {
        if (!AreAllTurretsReady())
        {
            return;
        }

        aimCommandIssued = false;
        salvoFireLoopState = SalvoFireLoopState.Aiming;
    }

    private float CalculateTargetElevation(float targetDistanceKilometers)
    {
        float elevation = targetDistanceKilometers;
        return Mathf.Clamp(elevation, minMainGunElevation, maxMainGunElevation);
    }

    private bool AreAllTurretsAimed()
    {
        bool hasValidTurret = false;

        for (int i = 0; i < turrets.Count; i++)
        {
            TurretWithCannons turret = turrets[i];
            if (turret == null)
            {
                continue;
            }

            hasValidTurret = true;
            if (!turret.IsAimed(toleranceDegrees))
            {
                return false;
            }
        }

        return hasValidTurret;
    }

    private bool AreAllTurretsReady()
    {
        bool hasValidTurret = false;

        for (int i = 0; i < turrets.Count; i++)
        {
            TurretWithCannons turret = turrets[i];
            if (turret == null)
            {
                continue;
            }

            hasValidTurret = true;
            if (!turret.IsReady)
            {
                return false;
            }
        }

        return hasValidTurret;
    }

    private bool HasAnyValidTurret()
    {
        if (turrets == null || turrets.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < turrets.Count; i++)
        {
            if (turrets[i] != null)
            {
                return true;
            }
        }

        return false;
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

    private void TryAssignMainGunTargetSubSystem()
    {
        if (mainGunTargetSubSystem == null)
        {
            mainGunTargetSubSystem = GetComponent<MainGunTargetSubSystem>();
        }
    }
}
