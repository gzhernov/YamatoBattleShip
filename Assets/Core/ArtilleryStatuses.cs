using System;
using System.Collections.Generic;
using UnityEngine;

public enum TurretLoadStatus
{
    Loaded = 0,
    Loading = 1
}

public enum FireControlActivityStatus
{
    Pause = 0,
    Firing = 1
}

public enum SalvoProgressStatus
{
    InProgress = 0,
    ReadyToNextSalvo = 1
}

[Serializable]
public struct TargetInfoState : IEquatable<TargetInfoState>
{
    [SerializeField] private string flag;
    [SerializeField] private string targetName;
    [SerializeField] private float targetBearing;
    [SerializeField] private float targetDistance;
    [SerializeField] private float targetSpeed;
    [SerializeField] private float targetCourse;
    [SerializeField] private float targetClosure;
    [SerializeField] private float targetAspect;

    public string Flag => flag;
    public string TargetName => targetName;
    public float TargetBearing => targetBearing;
    public float TargetDistance => targetDistance;
    public float TargetSpeed => targetSpeed;
    public float TargetCourse => targetCourse;
    public float TargetClosure => targetClosure;
    public float TargetAspect => targetAspect;

    public TargetInfoState(
        string flag,
        string targetName,
        float targetBearing,
        float targetDistance,
        float targetSpeed,
        float targetCourse,
        float targetClosure,
        float targetAspect)
    {
        this.flag = flag ?? string.Empty;
        this.targetName = targetName ?? string.Empty;
        this.targetBearing = targetBearing;
        this.targetDistance = targetDistance;
        this.targetSpeed = targetSpeed;
        this.targetCourse = targetCourse;
        this.targetClosure = targetClosure;
        this.targetAspect = targetAspect;
    }

    public bool Equals(TargetInfoState other)
    {
        return string.Equals(flag, other.flag, StringComparison.Ordinal)
            && string.Equals(targetName, other.targetName, StringComparison.Ordinal)
            && Mathf.Approximately(targetBearing, other.targetBearing)
            && Mathf.Approximately(targetDistance, other.targetDistance)
            && Mathf.Approximately(targetSpeed, other.targetSpeed)
            && Mathf.Approximately(targetCourse, other.targetCourse)
            && Mathf.Approximately(targetClosure, other.targetClosure)
            && Mathf.Approximately(targetAspect, other.targetAspect);
    }

    public override bool Equals(object obj)
    {
        return obj is TargetInfoState other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(flag, StringComparer.Ordinal);
        hash.Add(targetName, StringComparer.Ordinal);
        hash.Add(targetBearing);
        hash.Add(targetDistance);
        hash.Add(targetSpeed);
        hash.Add(targetCourse);
        hash.Add(targetClosure);
        hash.Add(targetAspect);
        return hash.ToHashCode();
    }
}

[Serializable]
public struct FireSolutionState : IEquatable<FireSolutionState>
{
    [SerializeField] private string fireSolutionPhase;
    [SerializeField] private float fireSolutionAccuracy;
    [SerializeField] private float fireSolutionDispersion;
    [SerializeField] private float fireSolutionTimeToImpact;
    [SerializeField] private string fireSolutionLastValue;

    public string FireSolutionPhase => fireSolutionPhase;
    public float FireSolutionAccuracy => fireSolutionAccuracy;
    public float FireSolutionDispersion => fireSolutionDispersion;
    public float FireSolutionTimeToImpact => fireSolutionTimeToImpact;
    public string FireSolutionLastValue => fireSolutionLastValue;

    public FireSolutionState(
        string fireSolutionPhase,
        float fireSolutionAccuracy,
        float fireSolutionDispersion,
        float fireSolutionTimeToImpact,
        string fireSolutionLastValue)
    {
        this.fireSolutionPhase = fireSolutionPhase ?? string.Empty;
        this.fireSolutionAccuracy = fireSolutionAccuracy;
        this.fireSolutionDispersion = fireSolutionDispersion;
        this.fireSolutionTimeToImpact = fireSolutionTimeToImpact;
        this.fireSolutionLastValue = fireSolutionLastValue ?? string.Empty;
    }

    public bool Equals(FireSolutionState other)
    {
        return string.Equals(fireSolutionPhase, other.fireSolutionPhase, StringComparison.Ordinal)
            && Mathf.Approximately(fireSolutionAccuracy, other.fireSolutionAccuracy)
            && Mathf.Approximately(fireSolutionDispersion, other.fireSolutionDispersion)
            && Mathf.Approximately(fireSolutionTimeToImpact, other.fireSolutionTimeToImpact)
            && string.Equals(fireSolutionLastValue, other.fireSolutionLastValue, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is FireSolutionState other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(fireSolutionPhase, StringComparer.Ordinal);
        hash.Add(fireSolutionAccuracy);
        hash.Add(fireSolutionDispersion);
        hash.Add(fireSolutionTimeToImpact);
        hash.Add(fireSolutionLastValue, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}

[Serializable]
public struct GunStatusState : IEquatable<GunStatusState>
{
    [SerializeField] private TurretLoadStatus[] turretStatus;
    [SerializeField] private string gunStatusShellTypeLoaded;
    [SerializeField] private string gunStatusShellTypeCharge;
    [SerializeField] private int gunStatusNextSalvo;

    public IReadOnlyList<TurretLoadStatus> TurretStatus => turretStatus ?? Array.Empty<TurretLoadStatus>();
    public string GunStatusShellTypeLoaded => gunStatusShellTypeLoaded;
    public string GunStatusShellTypeCharge => gunStatusShellTypeCharge;
    public int GunStatusNextSalvo => gunStatusNextSalvo;

    public GunStatusState(
        TurretLoadStatus[] turretStatus,
        string gunStatusShellTypeLoaded,
        string gunStatusShellTypeCharge,
        int gunStatusNextSalvo)
    {
        this.turretStatus = CopyTurretStatuses(turretStatus);
        this.gunStatusShellTypeLoaded = gunStatusShellTypeLoaded ?? string.Empty;
        this.gunStatusShellTypeCharge = gunStatusShellTypeCharge ?? string.Empty;
        this.gunStatusNextSalvo = gunStatusNextSalvo;
    }

    public GunStatusState WithCopiedTurretStatus()
    {
        return new GunStatusState(
            turretStatus,
            gunStatusShellTypeLoaded,
            gunStatusShellTypeCharge,
            gunStatusNextSalvo
        );
    }

    public bool Equals(GunStatusState other)
    {
        return string.Equals(gunStatusShellTypeLoaded, other.gunStatusShellTypeLoaded, StringComparison.Ordinal)
            && string.Equals(gunStatusShellTypeCharge, other.gunStatusShellTypeCharge, StringComparison.Ordinal)
            && gunStatusNextSalvo == other.gunStatusNextSalvo
            && HaveEqualTurretStatuses(turretStatus, other.turretStatus);
    }

    public override bool Equals(object obj)
    {
        return obj is GunStatusState other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(gunStatusShellTypeLoaded, StringComparer.Ordinal);
        hash.Add(gunStatusShellTypeCharge, StringComparer.Ordinal);
        hash.Add(gunStatusNextSalvo);

        if (turretStatus != null)
        {
            for (int i = 0; i < turretStatus.Length; i++)
            {
                hash.Add((int)turretStatus[i]);
            }
        }

        return hash.ToHashCode();
    }

    internal static TurretLoadStatus[] CopyTurretStatuses(TurretLoadStatus[] source)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<TurretLoadStatus>();

        TurretLoadStatus[] copy = new TurretLoadStatus[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    internal static bool HaveEqualTurretStatuses(
        TurretLoadStatus[] left,
        TurretLoadStatus[] right)
    {
        int leftLength = left?.Length ?? 0;
        int rightLength = right?.Length ?? 0;

        if (leftLength != rightLength)
            return false;

        for (int i = 0; i < leftLength; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }
}

[Serializable]
public struct FireControlState : IEquatable<FireControlState>
{
    [SerializeField] private string fireControlShellType;
    [SerializeField] private string fireControlShellTypeCharge;
    [SerializeField] private FireControlActivityStatus fireControlFireStatus;
    [SerializeField] private int fireControlSalvoNumber;
    [SerializeField] private SalvoProgressStatus fireControlSalvoStatus;

    public string FireControlShellType => fireControlShellType;
    public string FireControlShellTypeCharge => fireControlShellTypeCharge;
    public FireControlActivityStatus FireControlFireStatus => fireControlFireStatus;
    public int FireControlSalvoNumber => fireControlSalvoNumber;
    public SalvoProgressStatus FireControlSalvoStatus => fireControlSalvoStatus;

    public FireControlState(
        string fireControlShellType,
        string fireControlShellTypeCharge,
        FireControlActivityStatus fireControlFireStatus,
        int fireControlSalvoNumber,
        SalvoProgressStatus fireControlSalvoStatus)
    {
        this.fireControlShellType = fireControlShellType ?? string.Empty;
        this.fireControlShellTypeCharge = fireControlShellTypeCharge ?? string.Empty;
        this.fireControlFireStatus = fireControlFireStatus;
        this.fireControlSalvoNumber = fireControlSalvoNumber;
        this.fireControlSalvoStatus = fireControlSalvoStatus;
    }

    public bool Equals(FireControlState other)
    {
        return string.Equals(fireControlShellType, other.fireControlShellType, StringComparison.Ordinal)
            && string.Equals(fireControlShellTypeCharge, other.fireControlShellTypeCharge, StringComparison.Ordinal)
            && fireControlFireStatus == other.fireControlFireStatus
            && fireControlSalvoNumber == other.fireControlSalvoNumber
            && fireControlSalvoStatus == other.fireControlSalvoStatus;
    }

    public override bool Equals(object obj)
    {
        return obj is FireControlState other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(fireControlShellType, StringComparer.Ordinal);
        hash.Add(fireControlShellTypeCharge, StringComparer.Ordinal);
        hash.Add((int)fireControlFireStatus);
        hash.Add(fireControlSalvoNumber);
        hash.Add((int)fireControlSalvoStatus);
        return hash.ToHashCode();
    }
}

public class ArtilleryStatuses : MonoBehaviour
{
    [Header("Target Info")]
    [SerializeField] private TargetInfoState targetInfo;

    [Header("Fire Solution")]
    [SerializeField] private FireSolutionState fireSolution;

    [Header("Gun Status")]
    [SerializeField] private GunStatusState gunStatus;

    [Header("Fire Control")]
    [SerializeField] private FireControlState fireControl;

    public TargetInfoState TargetInfo => targetInfo;
    public FireSolutionState FireSolution => fireSolution;
    public GunStatusState GunStatus => gunStatus.WithCopiedTurretStatus();
    public FireControlState FireControl => fireControl;

    public event Action<TargetInfoState> OnTargetInfoChanged;
    public event Action<FireSolutionState> OnFireSolutionChanged;
    public event Action<GunStatusState> OnGunStatusChanged;
    public event Action<FireControlState> OnFireControlChanged;

    private void OnValidate()
    {
        NotifyAllChanged();
    }

    public void NotifyAllChanged()
    {
        OnTargetInfoChanged?.Invoke(targetInfo);
        OnFireSolutionChanged?.Invoke(fireSolution);
        OnGunStatusChanged?.Invoke(gunStatus.WithCopiedTurretStatus());
        OnFireControlChanged?.Invoke(fireControl);
    }

    public void SetTargetInfo(TargetInfoState value)
    {
        if (targetInfo.Equals(value))
            return;

        targetInfo = value;
        OnTargetInfoChanged?.Invoke(targetInfo);
    }

    public void SetFireSolution(FireSolutionState value)
    {
        if (fireSolution.Equals(value))
            return;

        fireSolution = value;
        OnFireSolutionChanged?.Invoke(fireSolution);
    }

    public void SetGunStatus(GunStatusState value)
    {
        GunStatusState sanitizedValue = value.WithCopiedTurretStatus();

        if (gunStatus.Equals(sanitizedValue))
            return;

        gunStatus = sanitizedValue;
        OnGunStatusChanged?.Invoke(gunStatus.WithCopiedTurretStatus());
    }

    public void SetFireControl(FireControlState value)
    {
        if (fireControl.Equals(value))
            return;

        fireControl = value;
        OnFireControlChanged?.Invoke(fireControl);
    }
}
