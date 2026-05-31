using UnityEngine;

public class RudderInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;

    [Header("Keyboard")]
    [SerializeField] private bool enableGlobalKeyboard = true;
    [SerializeField] private KeyCode leftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode rightKey = KeyCode.RightArrow;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private void Awake()
    {
        if (shipStatuses == null)
        {
            shipStatuses = FindFirstObjectByType<ShipStatuses>();
        }

    }

    private void Update()
    {
        if (!enableGlobalKeyboard)
            return;

        if (shipStatuses == null)
            return;

        if (Input.GetKeyDown(leftKey))
        {
            shipStatuses.MoveRudderLeft();

            if (showDebugInfo)
            {
                Debug.Log("RudderInputController: руль влево");
            }
        }
        else if (Input.GetKeyDown(rightKey))
        {
            shipStatuses.MoveRudderRight();

            if (showDebugInfo)
            {
                Debug.Log("RudderInputController: руль вправо");
            }
        }
    }

}
