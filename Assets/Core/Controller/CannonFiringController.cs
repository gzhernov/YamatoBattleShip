using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(CannonController))]
public class CannonFiringController : MonoBehaviour
{
    [FormerlySerializedAs("cannon")]
    [Header("Links")]
    [SerializeField] private CannonController cannonController;

    private void Reset()
    {
        if (cannonController == null)
        {
            cannonController = GetComponent<CannonController>();
        }
    }

    private void OnEnable()
    {
        if (cannonController == null)
        {
            cannonController = GetComponent<CannonController>();
        }

        if (cannonController == null)
        {
            Debug.LogWarning("CannonFiringController: не найден CannonController для подписки на фазу выстрела.", this);
            return;
        }

        // cannon.OnFireStarted -= HandleFireStarted;
        // cannon.OnFireStarted += HandleFireStarted;
    }

    private void OnDisable()
    {
        if (cannonController == null)
            return;

        // cannon.OnFireStarted -= HandleFireStarted;
    }

    private void HandleFireStarted()
    {
        Debug.Log($"CannonFiringController: орудие {cannonController.CannonId} вошло в фазу Fire.", this);
    }
}
