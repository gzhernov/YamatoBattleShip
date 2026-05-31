using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TurretWithCannons : MonoBehaviour
{
    [Header("Turret Settings")]
    [SerializeField] private TurretData turretData;
    [SerializeField] private TargetSelector targetSelector;
    
    [Header("Turret Rotation")]
    [SerializeField] private bool rotateWholeTurret = true;
    [SerializeField] private Transform turretPivot;
    
    [Header("Cannons")]
    [SerializeField] private List<Cannon> cannons = new List<Cannon>();
    [SerializeField] private bool autoFindCannons = true;
    [SerializeField] private bool autoInitializeCannons = true;
    
    [Header("Firing Settings")]
    [SerializeField] private FireMode fireMode = FireMode.Alternating;
    [SerializeField] private float burstDelay = 0.1f;
    
    private Transform currentTarget;
    private bool hasTarget = false;
    private int currentCannonIndex = 0;
    private float nextShotTime = 0f;
    
    public enum FireMode
    {
        AllAtOnce,
        Alternating,
        Random,
        Sequenced
    }
    
    private void Start()
    {
        InitializeCannons();
        SubscribeToTargetSelector();
    }
    
    private void OnValidate()
    {
        // В редакторе автоматически находим и инициализируем орудия
        if (autoFindCannons)
        {
            FindAllCannons();
        }
        
        if (autoInitializeCannons && turretData != null)
        {
            InitializeCannonsFromData();
        }
    }
    
    private void FindAllCannons()
    {
        // Находим все компоненты Cannon в дочерних объектах
        var foundCannons = GetComponentsInChildren<Cannon>();
        if (foundCannons.Length > 0)
        {
            cannons.Clear();
            cannons.AddRange(foundCannons);
            Debug.Log($"Found {cannons.Count} cannons in children");
        }
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
    
    private void SubscribeToTargetSelector()
    {
        if (targetSelector != null)
        {
            targetSelector.OnTargetSelected.AddListener(OnTargetSelected);
            targetSelector.OnTargetDeselected.AddListener(OnTargetDeselected);
        }
        else
        {
            // Пытаемся найти TargetSelector на сцене
            targetSelector = FindObjectOfType<TargetSelector>();
            if (targetSelector != null)
            {
                targetSelector.OnTargetSelected.AddListener(OnTargetSelected);
                targetSelector.OnTargetDeselected.AddListener(OnTargetDeselected);
                Debug.Log("Auto-found and subscribed to TargetSelector");
            }
            else
            {
                Debug.LogWarning("TargetSelector not found. Turret will not receive targets.");
            }
        }
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
    
    private void OnTargetSelected(Transform target)
    {
        currentTarget = target;
        hasTarget = true;
        UpdateCannonsTarget();
    }
    
    private void OnTargetDeselected()
    {
        currentTarget = null;
        hasTarget = false;
        UpdateCannonsTarget();
    }
    
    private void Update()
    {
        RotateTurretToTarget();
        // HandleFiring();
    }
    
    private void UpdateCannonsTarget()
    {
        foreach (var cannon in cannons)
        {
            if (cannon != null)
            {
                cannon.SetTarget(hasTarget ? currentTarget : null);
            }
        }
    }
    
    private void RotateTurretToTarget()
    {
        if (!hasTarget || currentTarget == null || turretData == null)
            return;
        
        Vector3 directionToTarget = currentTarget.position - transform.position;
        directionToTarget.y = 0;
        
        if (directionToTarget == Vector3.zero) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        
        if (rotateWholeTurret)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turretData.TurretSpeed * Time.deltaTime
            );
        }
        else if (turretPivot != null)
        {
            turretPivot.rotation = Quaternion.RotateTowards(
                turretPivot.rotation,
                targetRotation,
                turretData.TurretSpeed * Time.deltaTime
            );
        }
    }
    
    private void HandleFiring()
    {
        if (!hasTarget || currentTarget == null)
            return;
        
        if (Time.time < nextShotTime)
            return;
        
        switch (fireMode)
        {
            case FireMode.AllAtOnce:
                FireAllCannons();
                nextShotTime = Time.time + turretData.ReloadTime;
                break;
                
            case FireMode.Alternating:
                FireNextCannon();
                break;
                
            case FireMode.Random:
                FireRandomCannon();
                break;
                
            case FireMode.Sequenced:
                FireSequencedCannons();
                break;
        }
    }
    
    private void FireAllCannons()
    {
        foreach (var cannon in cannons)
        {
            cannon?.Shoot();
        }
    }
    
    private void FireNextCannon()
    {
        if (cannons.Count == 0) return;
        
        if (currentCannonIndex >= cannons.Count)
            currentCannonIndex = 0;
        
        var cannon = cannons[currentCannonIndex];
        if (cannon != null && cannon.Shoot())
        {
            currentCannonIndex++;
            
            if (currentCannonIndex >= cannons.Count)
            {
                currentCannonIndex = 0;
                nextShotTime = Time.time + turretData.ReloadTime;
            }
            else
            {
                nextShotTime = Time.time + burstDelay;
            }
        }
        else
        {
            nextShotTime = Time.time + burstDelay * 0.5f;
        }
    }
    
    private void FireRandomCannon()
    {
        if (cannons.Count == 0) return;
        
        int randomIndex = Random.Range(0, cannons.Count);
        if (cannons[randomIndex]?.Shoot() == true)
        {
            nextShotTime = Time.time + burstDelay;
        }
        else
        {
            nextShotTime = Time.time + burstDelay * 0.5f;
        }
    }
    
    private void FireSequencedCannons()
    {
        bool anyShot = false;
        
        foreach (var cannon in cannons)
        {
            if (cannon != null && cannon.Shoot())
            {
                anyShot = true;
                break;
            }
        }
        
        if (anyShot)
            nextShotTime = Time.time + burstDelay;
        else
            nextShotTime = Time.time + turretData.ReloadTime;
    }
    
    // Public методы для управления
    
    public void AddCannon(Cannon cannon)
    {
        if (!cannons.Contains(cannon))
        {
            cannons.Add(cannon);
            if (turretData != null)
            {
                InitializeCannonFromData(cannon);
            }
        }
    }
    
    public void RemoveCannon(Cannon cannon)
    {
        cannons.Remove(cannon);
    }
    
    public void ReinitializeAllCannons()
    {
        InitializeCannonsFromData();
    }
    
    public List<Cannon> GetCannons() => cannons;
    
    public void SetFireMode(FireMode mode)
    {
        fireMode = mode;
    }
    
    public void ForceReloadAll()
    {
        foreach (var cannon in cannons)
        {
            cannon?.ForceReload();
        }
    }
}