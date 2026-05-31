using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("Turret Settings")]
    [SerializeField] private TurretData turretData;
    [SerializeField] private Transform turretPivot;
    [SerializeField] private Transform cannonPivot;
    
    [Header("Target Settings")]
    [SerializeField] private TargetSelector targetSelector;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private Transform currentTarget;
    private bool hasTarget = false;
    private float currentCannonAngle = 0f;
    
    private void Start()
    {
        // Подписываемся на события выбора цели
        if (targetSelector != null)
        {
            targetSelector.OnTargetSelected.AddListener(OnTargetSelected);
            targetSelector.OnTargetDeselected.AddListener(OnTargetDeselected);
        }
        else
        {
            Debug.LogError("TargetSelector not assigned to TurretController!", this);
        }
        
        if (turretData == null)
            Debug.LogError("TurretData not assigned!", this);
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (targetSelector != null)
        {
            targetSelector.OnTargetSelected.RemoveListener(OnTargetSelected);
            targetSelector.OnTargetDeselected.RemoveListener(OnTargetDeselected);
        }
    }
    
    private void Update()
    {
        AimAtTarget();
    }
    
    private void OnTargetSelected(Transform target)
    {
        currentTarget = target;
        hasTarget = true;
        
        if (showDebugInfo)
            Debug.Log($"<color=green>Turret acquired target: {target.name}</color>");
    }
    
    private void OnTargetDeselected()
    {
        currentTarget = null;
        hasTarget = false;
        
        if (showDebugInfo)
            Debug.Log("<color=yellow>Turret lost target</color>");
    }
    
    private void AimAtTarget()
    {
        if (!hasTarget || currentTarget == null || turretData == null)
            return;
        
        RotateTurret();
        RotateCannon();
    }
    
    private void RotateTurret()
    {
        if (turretPivot == null) return;
        
        Vector3 directionToTarget = currentTarget.position - transform.position;
        directionToTarget.y = 0;
        
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            turretPivot.rotation = Quaternion.RotateTowards(
                turretPivot.rotation, 
                targetRotation, 
                turretData.TurretSpeed * Time.deltaTime
            );
        }
    }
    
    private void RotateCannon()
    {
        if (cannonPivot == null) return;
        
        Vector3 toTarget = currentTarget.position - cannonPivot.position;
        float distance = toTarget.magnitude;
        
        // Расчет угла возвышения на основе кривой
        float normalizedDistance = Mathf.Clamp01(distance / turretData.MaxRange);
        float curveValue = turretData.TragectoryCurve.Evaluate(normalizedDistance);
        float targetAngle = curveValue * turretData.MaxAngle;
        targetAngle = Mathf.Clamp(targetAngle, -turretData.MaxAngle, turretData.MaxAngle);
        
        // Плавное движение орудия
        currentCannonAngle = Mathf.MoveTowards(
            currentCannonAngle, 
            targetAngle, 
            turretData.CannonSpeed * Time.deltaTime
        );
        
        cannonPivot.localRotation = Quaternion.Euler(-currentCannonAngle, 0, 0);
    }
    
    public bool HasTarget() => hasTarget;
    public Transform GetCurrentTarget() => currentTarget;
}