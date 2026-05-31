using UnityEngine;
using UnityEngine.Events;

public class Cannon : MonoBehaviour
{
    [Header("Cannon Settings")]
    [SerializeField] private string cannonId = "Cannon";
    [SerializeField] private Transform cannonPivot; // Точка поворота орудия
    [SerializeField] private Transform firePoint; // Точка вылета снаряда
    
    [Header("Cannon Parameters")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cannonSpeed = 30f; // Скорость наведения
    [SerializeField] private float reloadTime = 1f;
    [SerializeField] private AnimationCurve angleCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float maxRange = 100f;
    
    [Header("Current State")]
    [SerializeField] private bool isReady = true;
    [SerializeField] private float currentAngle = 0f;
    [SerializeField] private float currentReloadTime = 0f;
    
    [Header("Events")]
    public UnityEvent OnShoot;
    public UnityEvent OnReloadStart;
    public UnityEvent OnReloadEnd;
    public UnityEvent<float> OnAngleChanged;
    
    private Transform currentTarget;
    private bool hasTarget = false;
    
    public string CannonId => cannonId;
    public bool IsReady => isReady && currentReloadTime <= 0;
    public float CurrentAngle => currentAngle;
    public Transform FirePoint => firePoint;
    
    private void Update()
    {
        // UpdateReload();
        AimAtTarget();
    }
    
    public void InitializeFromData(TurretData data)
    {
        damage = data.Damage;
        cannonSpeed = data.CannonSpeed;
        reloadTime = data.ReloadTime;
        angleCurve = data.TragectoryCurve;
        maxAngle = data.MaxAngle;
        maxRange = data.MaxRange;
    }
    
    private void UpdateReload()
    {
        if (currentReloadTime > 0)
        {
            currentReloadTime -= Time.deltaTime;
            if (currentReloadTime <= 0)
            {
                isReady = true;
                OnReloadEnd?.Invoke();
            }
        }
    }
    
    private void AimAtTarget()
    {
        if (!hasTarget || currentTarget == null || cannonPivot == null)
            return;
        
        Vector3 toTarget = currentTarget.position - cannonPivot.position;
        float distance = toTarget.magnitude;
        
        // Расчет угла возвышения
        float targetAngle = CalculateElevationAngle(distance);
        
        // Плавное движение орудия
        currentAngle = Mathf.MoveTowards(
            currentAngle,
            targetAngle,
            cannonSpeed * Time.deltaTime
        );
        
        // Применяем поворот
        cannonPivot.localRotation = Quaternion.Euler(-currentAngle, 0, 0);
        OnAngleChanged?.Invoke(currentAngle);
    }
    
    private float CalculateElevationAngle(float distance)
    {
        // float normalizedDistance = Mathf.Clamp01(distance / maxRange);
        float curveValue = angleCurve.Evaluate(distance);
        // return curveValue * maxAngle;
        return curveValue;
    }
    
    public void SetTarget(Transform target)
    {
        currentTarget = target;
        hasTarget = target != null;
    }
    
    public bool Shoot()
    {
        if (!IsReady || !hasTarget || currentTarget == null)
            return false;
        
        // Проверяем, наведено ли орудие достаточно точно
        float targetAngle = CalculateElevationAngle(Vector3.Distance(currentTarget.position, cannonPivot.position));
        if (Mathf.Abs(currentAngle - targetAngle) > 1f)
            return false; // Орудие еще не наведено
        
        // Производим выстрел
        isReady = false;
        currentReloadTime = reloadTime;
        OnReloadStart?.Invoke();
        OnShoot?.Invoke();
        
        Debug.Log($"<color=red>Cannon {cannonId} fired! Damage: {damage}</color>");
        return true;
    }
    
    public void ForceReload()
    {
        currentReloadTime = reloadTime;
        isReady = false;
        OnReloadStart?.Invoke();
    }
}