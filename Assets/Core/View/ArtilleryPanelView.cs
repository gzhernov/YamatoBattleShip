using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ArtilleryPanelController : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Контроллер огня главного калибра, в который делегируются команды залпа.")]
    [SerializeField] private FireControlController fireControlController;

    [Tooltip("Кнопка, по нажатию на которую отправляется команда залпа.")]
    [SerializeField] private Button fireButton;

    [Tooltip("Кнопка, по нажатию на которую включается или выключается автоматический залповый огонь.")]
    [SerializeField] private Button autoFireButton;

    [Tooltip("Лампа, которая мигает, пока активен автоматический цикл залпового огня.")]
    [SerializeField] private LampController salvoLamp;

    [Header("Настройки")]
    [Tooltip("Если включено, компонент пишет ошибки конфигурации в лог.")]
    [SerializeField] private bool logConfigurationErrors = true;

    private bool isSubscribed;

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        ValidateReferences();
        Subscribe();
        UpdateSalvoLamp();
    }

    private void OnDisable()
    {
        Unsubscribe();
        SetSalvoLampState(LampState.Off);
    }

    private void OnValidate()
    {
        ValidateReferences();
    }

    private void Update()
    {
        UpdateSalvoLamp();
    }

    public void InvokeFire()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        fireControlController.StartSalvoFire();
    }

    public void InvokeAutoFire()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        if (fireControlController.IsAutoSalvoFireActive())
        {
            fireControlController.StopAutoSalvoFire();
            return;
        }

        fireControlController.StartAutoSalvoFire();
    }

    private void Subscribe()
    {
        if (isSubscribed || !HasRequiredReferences())
        {
            return;
        }

        fireButton.onClick.AddListener(InvokeFire);
        autoFireButton.onClick.AddListener(InvokeAutoFire);
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (fireButton != null)
        {
            fireButton.onClick.RemoveListener(InvokeFire);
        }

        if (autoFireButton != null)
        {
            autoFireButton.onClick.RemoveListener(InvokeAutoFire);
        }

        isSubscribed = false;
    }

    private bool HasRequiredReferences()
    {
        bool hasAllReferences = true;

        if (fireControlController == null)
        {
            LogConfigurationError("ArtilleryPanelController: не назначена ссылка на FireControlController.");
            hasAllReferences = false;
        }

        if (fireButton == null)
        {
            LogConfigurationError("ArtilleryPanelController: не назначена ссылка на кнопку Fire.");
            hasAllReferences = false;
        }

        if (autoFireButton == null)
        {
            LogConfigurationError("ArtilleryPanelController: не назначена ссылка на кнопку автоогня.");
            hasAllReferences = false;
        }

        if (salvoLamp == null)
        {
            LogConfigurationError("ArtilleryPanelController: не назначена ссылка на лампу залпа.");
            hasAllReferences = false;
        }

        return hasAllReferences;
    }

    private void UpdateSalvoLamp()
    {
        if (fireControlController == null || salvoLamp == null)
        {
            return;
        }

        LampState targetState = fireControlController.IsSalvoFireLoopActive()
            ? LampState.Blinked
            : LampState.Off;

        SetSalvoLampState(targetState);
    }

    private void SetSalvoLampState(LampState targetState)
    {
        if (salvoLamp == null || salvoLamp.CurrentState == targetState)
        {
            return;
        }

        salvoLamp.SetState(targetState);
    }

    private void ValidateReferences()
    {
        HasRequiredReferences();
    }

    private void LogConfigurationError(string message)
    {
        if (logConfigurationErrors)
        {
            Debug.LogError(message, this);
        }
    }
}
