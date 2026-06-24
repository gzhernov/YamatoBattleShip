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
    [Header("Turret Settings")]
    [SerializeField] private TurretData turretData;

    [Header("Turret Rotation")]
    [SerializeField] private bool rotateWholeTurret = true;
    [SerializeField] private Transform turretPivot;

    [Header("Cannons")]
    [SerializeField] private List<Cannon> cannons = new List<Cannon>();
    [SerializeField] private bool autoFindCannons = true;
    [SerializeField] private bool autoInitializeCannons = true;

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

        if (autoInitializeCannons && turretData != null)
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
        var foundCannons = GetComponentsInChildren<Cannon>();
        if (foundCannons.Length == 0)
            return;

        UnsubscribeFromCannons();
        cannons.Clear();
        cannons.AddRange(foundCannons);
        SubscribeToCannons();

        Debug.Log($"Found {cannons.Count} cannons in children");
    }

    private void InitializeCannons()
    {
        if (turretData == null)
        {
            Debug.LogError("TurretData is not assigned!", this);
            return;
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

        Debug.Log($"Initialized {cannons.Count} cannons with data from {turretData.name}");
    }

    private void InitializeCannonFromData(Cannon cannon)
    {
        cannon.InitializeFromData(turretData);
        Debug.Log($"Initialized cannon: {cannon.CannonId}");
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

    private Transform GetRotationSource()
    {
        return rotateWholeTurret ? transform : turretPivot;
    }

    private void RotateTurretToBearing()
    {
        if (!hasAimCommand || turretData == null)
            return;

        if (IsTurretBearingReached())
            return;

        Transform rotationSource = GetRotationSource();
        if (rotationSource == null)
            return;

        Quaternion targetRotation = Quaternion.Euler(0f, desiredBearing, 0f);
        rotationSource.rotation = Quaternion.RotateTowards(
            rotationSource.rotation,
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
        Transform rotationSource = GetRotationSource();
        if (rotationSource == null)
            return 0f;

        return rotationSource.rotation.eulerAngles.y;
    }

    public void AddCannon(Cannon cannon)
    {
        if (cannon == null || cannons.Contains(cannon))
            return;

        cannons.Add(cannon);
        cannon.OnCycleCompleted -= HandleCannonCycleCompleted;
        cannon.OnAimCompleted -= HandleCannonAimCompleted;
        cannon.OnCycleCompleted += HandleCannonCycleCompleted;
        cannon.OnAimCompleted += HandleCannonAimCompleted;

        if (turretData != null)
        {
            InitializeCannonFromData(cannon);
        }
    }

    public void RemoveCannon(Cannon cannon)
    {
        if (cannon != null)
        {
            cannon.OnCycleCompleted -= HandleCannonCycleCompleted;
            cannon.OnAimCompleted -= HandleCannonAimCompleted;
        }

        cannons.Remove(cannon);
    }

    public void ReinitializeAllCannons()
    {
        InitializeCannonsFromData();
    }

    public List<Cannon> GetCannons() => cannons;

    public void ForceReloadAll()
    {
        foreach (var cannon in cannons)
        {
            cannon?.ForceReload();
        }
    }
}
