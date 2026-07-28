using System;
using System.Collections.Generic;
using UnityEngine;

public enum TurretCycleState
{
    Ready,
    Aiming,
    Aimed,
    SalvoInProgress
}

public class TurretWithCannons : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform turretMesh;

    [Header("Turret Settings")]
    [SerializeField] private TurretData turretData;

    [Header("Cannons")]
    [SerializeField] private List<CannonController> cannons = new List<CannonController>();
    [SerializeField] private bool autoFindCannons = true;
    [SerializeField] private bool autoInitializeCannons = true;

    [Header("Cannon Parameters")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cannonSpeed = 30f;
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float loadingAngle = 0f;
    [SerializeField] private float microDelayMin = 0f;
    [SerializeField] private float microDelayMax = 0.15f;

    [Header("Cannon Durations")]
    [SerializeField] private float prepareFiringDuration = 0f;
    [SerializeField] private float firingDuration = 5f;
    [SerializeField] private float prepareLoadingDuration = 5f;
    [SerializeField] private float reloadDuration = 5f;

    [Header("Cycle State")]
    [SerializeField] private TurretCycleState cycleState = TurretCycleState.Ready;

    public float desiredBearing = 0f;
    public bool hasAimCommand = false;

    public event Action<TurretCycleState> OnTurretStateChanged;
    public event Action OnAimCompleted;
    public event Action OnSalvoStarted;
    public event Action OnSalvoCompleted;

    public TurretCycleState CycleState => cycleState;
    public bool IsReady => cycleState == TurretCycleState.Ready;
    public bool IsBusy => cycleState == TurretCycleState.Aiming || cycleState == TurretCycleState.SalvoInProgress;
    public int TotalGunCount => cannons.Count;
    public int ReadyGunCount => CountReadyCannons();

    private void Start()
    {
        InitializeCannons();
        SubscribeToCannons();
    }

    private void OnDestroy()
    {
        UnsubscribeFromCannons();
    }

    private void OnValidate()
    {

        // В редакторе автоматически находим и инициализируем орудия.
        if (autoFindCannons)
        {
            FindAllCannons();
        }

        if (autoInitializeCannons)
        {
            InitializeCannonsFromData();
        }
    }

    private void Update()
    {
        RotateTurretToBearing();
        UpdateTurretCycleState();
    }

    private void FindAllCannons()
    {
        // Находим все компоненты Cannon в дочерних объектах.
        var foundCannons = GetComponentsInChildren<CannonController>();
        if (foundCannons.Length == 0)
            return;

        // UnsubscribeFromCannons();
        cannons.Clear();
        cannons.AddRange(foundCannons);
        // SubscribeToCannons();

        Debug.Log($"Found {cannons.Count} cannons in children");
    }

    private void InitializeCannons()
    {
        if (turretData == null)
        {
            Debug.LogWarning("TurretData is not assigned. Turret rotation speed will not be available.", this);
        }

        InitializeCannonsFromData();
    }

    private void InitializeCannonsFromData()
    {

        if (cannons == null || cannons.Count == 0)
        {
            Debug.LogWarning("No cannons to initialize. Try enabling autoFindCannons or assign cannons manually.");
            return;
        }

        foreach (var cannon in cannons)
        {
            if (cannon != null)
            {
                InitializeCannonFromData(cannon);
            }
        }

        Debug.Log($"Initialized {cannons.Count} cannons with turret settings");
    }

    private void InitializeCannonFromData(CannonController cannonController)
    {
        cannonController.ApplyTurretSettings(
            damage,
            cannonSpeed,
            maxAngle,
            loadingAngle,
            microDelayMin,
            microDelayMax,
            prepareFiringDuration,
            firingDuration,
            prepareLoadingDuration,
            reloadDuration);

        Debug.Log($"Initialized cannon: {cannonController.CannonId}");
    }

    private void SubscribeToCannons()
    {
        foreach (var cannon in cannons)
        {
            if (cannon == null)
                continue;
    
            cannon.OnCycleCompleted -= HandleCannonCycleCompleted;
            cannon.OnAimCompleted -= HandleCannonAimCompleted;
            cannon.OnCycleCompleted += HandleCannonCycleCompleted;
            cannon.OnAimCompleted += HandleCannonAimCompleted;
        }
    }

    private void UnsubscribeFromCannons()
    {
        foreach (var cannon in cannons)
        {
            if (cannon == null)
                continue;
    
            cannon.OnCycleCompleted -= HandleCannonCycleCompleted;
            cannon.OnAimCompleted -= HandleCannonAimCompleted;
        }
    }

    private void HandleCannonCycleCompleted()
    {
        UpdateTurretCycleState();
    }

    private void HandleCannonAimCompleted()
    {
        UpdateTurretCycleState();
    }

    private void UpdateTurretCycleState()
    {
        if (cycleState == TurretCycleState.SalvoInProgress)
        {
            if (AreAllCannonsReady())
            {
                SetCycleState(TurretCycleState.Ready);
                OnSalvoCompleted?.Invoke();
            }

            return;
        }

        if (cycleState == TurretCycleState.Aiming && IsAimSatisfied())
        {
            hasAimCommand = false;
            SetCycleState(TurretCycleState.Aimed);
            OnAimCompleted?.Invoke();
        }
    }

    private void RotateTurretToBearing()
    {
        if (!hasAimCommand || turretData == null)
            return;

        if (IsTurretBearingReached())
            return;

        if (turretMesh == null)
            return;

        Quaternion targetRotation = Quaternion.Euler(0f, desiredBearing, 0f);
        turretMesh.rotation = Quaternion.RotateTowards(
            turretMesh.rotation,
            targetRotation,
            turretData.TurretSpeed * Time.deltaTime
        );
    }

    private void SetCycleState(TurretCycleState newState)
    {
        if (cycleState == newState)
            return;

        cycleState = newState;
        OnTurretStateChanged?.Invoke(cycleState);
    }

    private bool AreAllCannonsReady()
    {
        if (cannons.Count == 0)
            return false;

        foreach (var cannon in cannons)
        {
            if (cannon == null || !cannon.IsReady)
                return false;
        }

        return true;
    }

    private bool IsAimSatisfied()
    {
        if (!IsTurretBearingReached())
            return false;

        foreach (var cannon in cannons)
        {
            if (cannon == null || !cannon.IsAimed())
                return false;
        }

        return true;
    }

    private int CountReadyCannons()
    {
        int readyCount = 0;

        foreach (var cannon in cannons)
        {
            if (cannon != null && cannon.IsReady)
            {
                readyCount++;
            }
        }

        return readyCount;
    }

    private bool IsTurretBearingReached(float toleranceDegrees = 0.5f)
    {
        float angleDelta = Mathf.Abs(Mathf.DeltaAngle(GetCurrentBearing(), desiredBearing));
        return angleDelta <= toleranceDegrees;
    }

    public void Aim(float bearing, float elevationAngle)
    {
        SetAim(bearing, elevationAngle);
    }

    public void SetAim(float bearing, float elevationAngle)
    {
        desiredBearing = Mathf.Repeat(bearing, 360f);
        hasAimCommand = true;
        SetCycleState(TurretCycleState.Aiming);

        foreach (var cannon in cannons)
        {
            cannon?.SetAim(elevationAngle);
        }
    }

    public void ClearAim()
    {
        hasAimCommand = false;
    }

    public bool TryStartSalvo()
    {
        if (cannons.Count == 0)
            return false;

        if (cycleState != TurretCycleState.Aimed && !IsAimSatisfied())
            return false;

        bool anyStarted = false;

        foreach (var cannon in cannons)
        {
            if (cannon != null && cannon.TryStartFireCycle())
            {
                anyStarted = true;
            }
        }

        if (!anyStarted)
            return false;

        SetCycleState(TurretCycleState.SalvoInProgress);
        OnSalvoStarted?.Invoke();
        return true;
    }

    public bool HasAimCommand() => hasAimCommand;

    public bool IsAimed(float toleranceDegrees = 0.5f)
    {
        if (!IsTurretBearingReached(toleranceDegrees))
            return false;

        foreach (var cannon in cannons)
        {
            if (cannon == null || !cannon.IsAimed(toleranceDegrees))
                return false;
        }

        return true;
    }

    public float GetDesiredBearing() => desiredBearing;

    public float GetCurrentBearing()
    {
        if (turretMesh == null)
            return 0f;

        return turretMesh.rotation.eulerAngles.y;
    }

    public void AddCannon(CannonController cannonController)
    {
        if (cannonController == null || cannons.Contains(cannonController))
            return;
    
        cannons.Add(cannonController);
        cannonController.OnCycleCompleted -= HandleCannonCycleCompleted;
        cannonController.OnAimCompleted -= HandleCannonAimCompleted;
        cannonController.OnCycleCompleted += HandleCannonCycleCompleted;
        cannonController.OnAimCompleted += HandleCannonAimCompleted;
    
        InitializeCannonFromData(cannonController);
    }

    public void RemoveCannon(CannonController cannonController)
    {
        if (cannonController != null)
        {
            cannonController.OnCycleCompleted -= HandleCannonCycleCompleted;
            cannonController.OnAimCompleted -= HandleCannonAimCompleted;
        }
    
        cannons.Remove(cannonController);
    }

    public void ReinitializeAllCannons()
    {
        InitializeCannonsFromData();
    }

    public List<CannonController> GetCannons() => cannons;

    // public void ForceReloadAll()
    // {
    //     foreach (var cannon in cannons)
    //     {
    //         cannon?.ForceReload();
    //     }
    // }
}
