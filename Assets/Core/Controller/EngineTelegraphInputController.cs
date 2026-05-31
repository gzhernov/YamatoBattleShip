using UnityEngine;

public class EngineTelegraphInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;

    [Header("Keyboard Controls")]
    [SerializeField] private KeyCode increaseKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode decreaseKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode increaseKeyAlt = KeyCode.W;
    [SerializeField] private KeyCode decreaseKeyAlt = KeyCode.S;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (shipStatuses == null)
            return;

        HandleKeyboardInput();
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = FindFirstObjectByType<ShipStatuses>();
        }

        if (shipStatuses == null)
        {
            Debug.LogError("EngineTelegraphInputController: ShipStatuses РЅРµ РЅР°Р№РґРµРЅ.");
        }
        else if (showDebugInfo)
        {
            Debug.Log("EngineTelegraphInputController: keyboard input initialized.");
        }
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(increaseKey) || Input.GetKeyDown(increaseKeyAlt))
        {
            shipStatuses.IncreaseEngineTelegraphSector();
        }
        else if (Input.GetKeyDown(decreaseKey) || Input.GetKeyDown(decreaseKeyAlt))
        {
            shipStatuses.DecreaseEngineTelegraphSector();
        }
    }
}
