using UnityEngine;

[DisallowMultipleComponent]
public class CannonFiringController : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Cannon cannon;

    private void Reset()
    {
        if (cannon == null)
        {
            cannon = GetComponent<Cannon>();
        }
    }

    private void OnEnable()
    {
        if (cannon == null)
        {
            cannon = GetComponent<Cannon>();
        }

        if (cannon == null)
        {
            Debug.LogWarning("CannonFiringController: не найден Cannon для подписки на фазу выстрела.", this);
            return;
        }

        cannon.OnFireStarted -= HandleFireStarted;
        cannon.OnFireStarted += HandleFireStarted;
    }

    private void OnDisable()
    {
        if (cannon == null)
            return;

        cannon.OnFireStarted -= HandleFireStarted;
    }

    private void HandleFireStarted()
    {
        Debug.Log($"CannonFiringController: орудие {cannon.CannonId} вошло в фазу Fire.", this);
    }
}
