using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Game/TurretData")]
public class TurretData : ScriptableObject
{
    [Header("Damage Settings")]
    [SerializeField] private float damage = 10f;

    [Header("Movement Settings")]
    [SerializeField] private float cannonSpeed = 50f;
    [SerializeField] private float turretSpeed = 30f;

    [Header("Shooting Settings")]
    [SerializeField] private float reloadTime = 1f;
    [SerializeField] private int maxRange = 1;
    [SerializeField] private float maxAngle = 1f;

    [Header("Ballistics")]
    [SerializeField] private AnimationCurve tragectoryCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // Public Properties
    public float Damage => damage;
    public float CannonSpeed => cannonSpeed;
    public float TurretSpeed => turretSpeed;
    public float ReloadTime => reloadTime;
    public int MaxRange => maxRange;
    public float MaxAngle => maxAngle;
    public AnimationCurve TragectoryCurve => tragectoryCurve;
}