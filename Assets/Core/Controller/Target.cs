using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField] private string targetId;
    [SerializeField] private float priority = 1f;
    
    public string TargetId => targetId;
    public float Priority => priority;
    public Transform TargetTransform => transform;
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}