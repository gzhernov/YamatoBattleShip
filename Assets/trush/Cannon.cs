using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

public enum CannonCycleState
{
    Ready,
    Aiming,
    PrepareFiring,
    Firing,
    PrepareLoading,
    Loading
}

public class Cannon : MonoBehaviour
{
    [Header("Cannon Settings")]
    [SerializeField] private string cannonId = "Cannon";
    [SerializeField] private Transform cannonPivot;
    [SerializeField] private Transform firePoint;

    [Header("Cannon Parameters")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cannonSpeed = 30f;

    [Header("Cannon Durations")] 
    [SerializeField] private float prepareFiringDuration = 0f;
    [SerializeField] private float firingDuration = 5f;
    [SerializeField] private float prepareLoadingDuration = 5f;
    [SerializeField] private float reloadDuration = 30f;
    
    
    public Dictionary<CannonCycleState, float> FiringDurations =
        new Dictionary<CannonCycleState, float>
        {
            [CannonCycleState.PrepareFiring] = 0,
            [CannonCycleState.Firing] = 5f,
            [CannonCycleState.PrepareLoading] = 5f,
            [CannonCycleState.Loading] = 5
        };
    
    
    
    [Header(" ")]
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float loadingAngle = 0f;
    [SerializeField] private float microDelayMin = 0f;
    [SerializeField] private float microDelayMax = 0.15f;

    [Header("Current State")]
    [SerializeField] private CannonCycleState cycleState = CannonCycleState.Ready;
    [SerializeField] private float currentAngle = 0f;
    [SerializeField] private float stateTimer = 0f;

    [Header("Aim State")]
    [SerializeField] private float desiredAngle = 0f;
    [SerializeField] private bool hasAimCommand = false;

    // [Header("Events")]
    // public UnityEvent OnShoot;
    // public UnityEvent OnReloadStart;
    // public UnityEvent OnReloadEnd;
    // public UnityEvent<float> OnAngleChanged;

    public event Action<CannonCycleState> OnStateChanged;
    // public event Action OnAimStarted;
    public event Action OnAimCompleted;
    
    public event Action OnFireStarted;
    public event Action OnFirePerformed;
    public event Action OnPreprareLoadingStarted;
    
    public event Action OnLoadingStarted;
    
    public event Action OnReadyStarted;
    public event Action OnCycleCompleted;

    public string CannonId => cannonId;
    public bool IsReady => cycleState == CannonCycleState.Ready;
    public bool IsBusy => cycleState == CannonCycleState.Firing || cycleState == CannonCycleState.Loading;
    public float CurrentAngle => currentAngle;
    public Transform FirePoint => firePoint;
    public CannonCycleState CycleState => cycleState;

    private void OnEnable()
    {
        OnFireStarted += LogCurrentStatus;
        OnPreprareLoadingStarted += LogCurrentStatus;
        OnLoadingStarted += LogCurrentStatus;
        OnReadyStarted += LogCurrentStatus;
    }

    private void OnDisable()
    {
        OnFireStarted -= LogCurrentStatus;
        OnPreprareLoadingStarted -= LogCurrentStatus;
        OnLoadingStarted -= LogCurrentStatus;
        OnReadyStarted -= LogCurrentStatus;
    }

    private void Update()
    {
        RotateCannonToAngle();
        UpdateStateMachine();
    }

    public void InitializeFromData(TurretData data)
    {
        damage = data.Damage;
        cannonSpeed = data.CannonSpeed;
        reloadDuration = data.ReloadTime;
        maxAngle = data.MaxAngle;
    }

    private void RotateCannonToAngle()
    {
        if (!hasAimCommand || cannonPivot == null)
            return;

        if (IsAngleReached())
        {
            hasAimCommand = false;
            if (cycleState == CannonCycleState.Aiming)
            {
                SetCycleState(CannonCycleState.Ready);
                OnAimCompleted?.Invoke();
            }

            return;
        }

        currentAngle = Mathf.MoveTowards(
            currentAngle,
            desiredAngle,
            cannonSpeed * Time.deltaTime
        );

        cannonPivot.localRotation = Quaternion.Euler(-currentAngle, 0f, 0f);
        // OnAngleChanged?.Invoke(currentAngle);

        if (IsAngleReached())
        {
            hasAimCommand = false;
            if (cycleState == CannonCycleState.Aiming)
            {
                SetCycleState(CannonCycleState.Ready);
                OnAimCompleted?.Invoke();
            }
        }
    }

    private void UpdateStateMachine()
    {
        if (stateTimer > 0f)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
            {
                return;
            }

            stateTimer = 0f;
        }

        switch (cycleState)
        {
            case CannonCycleState.PrepareFiring:
                stateTimer =  GetDuration(CannonCycleState.Firing) + GetRandomMicroDelay();
                SetCycleState(CannonCycleState.Firing);
                OnFireStarted?.Invoke();
                
                break;
            case CannonCycleState.Firing:
                stateTimer = GetDuration(CannonCycleState.PrepareLoading) + GetRandomMicroDelay();
                SetCycleState(CannonCycleState.PrepareLoading);
                OnPreprareLoadingStarted?.Invoke();
                    
                break;
            case CannonCycleState.PrepareLoading:
                stateTimer = GetDuration(CannonCycleState.Loading) + GetRandomMicroDelay();
                SetCycleState(CannonCycleState.Loading);
                OnLoadingStarted?.Invoke();
                
                break;
            case CannonCycleState.Loading:
                SetCycleState(CannonCycleState.Ready);
                OnReadyStarted?.Invoke();
                CompleteLoadingPhase();
                break;
        }
        
    }

    public float GetDuration(CannonCycleState state)
    {
        return FiringDurations.TryGetValue(state, out float duration)
            ? duration
            : 0f;
    }
    
    private void BeginLoadingPhase()
    {
        SetCycleState(CannonCycleState.Loading);
        // OnLoadingStarted?.Invoke();
        // OnReloadStart?.Invoke();

        // desiredAngle = Mathf.Clamp(loadingAngle, -maxAngle, maxAngle);
        // hasAimCommand = cannonPivot != null;

        stateTimer = reloadDuration + GetRandomMicroDelay();
    }

    private void CompleteLoadingPhase()
    {
        OnCycleCompleted?.Invoke();
        hasAimCommand = false;
    }

    private void SetCycleState(CannonCycleState newState)
    {
        if (cycleState == newState)
            return;

        cycleState = newState;
        OnStateChanged?.Invoke(cycleState);
    }

    private void LogCurrentStatus()
    {
        Debug.Log($"Cannon {cannonId} current status: {cycleState}");
    }

    private float GetRandomMicroDelay()
    {
        if (microDelayMax <= 0f)
            return 0f;

        float min = Mathf.Min(microDelayMin, microDelayMax);
        float max = Mathf.Max(microDelayMin, microDelayMax);
        return UnityEngine.Random.Range(min, max);
    }

    private bool IsAngleReached(float toleranceDegrees = 0.5f)
    {
        return Mathf.Abs(currentAngle - desiredAngle) <= toleranceDegrees;
    }

    public bool TryStartFireCycle()
    {
        if (!IsReady)
            return false;
        
        stateTimer =  GetDuration(CannonCycleState.PrepareFiring) + GetRandomMicroDelay();
        SetCycleState(CannonCycleState.PrepareFiring);
        
        // OnFireStarted?.Invoke();
        // OnShoot?.Invoke();
        // OnShotPerformed?.Invoke();

        Debug.Log($"<color=red>Cannon {cannonId} fired! Damage: {damage}</color>");
        return true;
    }

    public bool Shoot()
    {
        return TryStartFireCycle();
    }

    public void Aim(float angle)
    {
        SetAim(angle);
    }

    public void SetAim(float angle)
    {
        desiredAngle = Mathf.Clamp(angle, -maxAngle, maxAngle);
        hasAimCommand = true;

        if (cycleState == CannonCycleState.Ready && !IsAngleReached())
        {
            SetCycleState(CannonCycleState.Aiming);
            // OnAimStarted?.Invoke();
        }
    }

    public void ClearAim()
    {
        hasAimCommand = false;
    }

    public bool HasAimCommand() => hasAimCommand;

    public bool IsAimed(float toleranceDegrees = 0.5f)
    {
        return Mathf.Abs(currentAngle - desiredAngle) <= toleranceDegrees;
    }

    public float GetDesiredAngle() => desiredAngle;

    public void ForceReload()
    {
        BeginLoadingPhase();
    }
}
