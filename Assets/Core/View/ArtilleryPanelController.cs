using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ArtilleryPanelController : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Контроллер платформы, в который делегируется команда залпа.")]
    [SerializeField] private PlatformController platformController;

    [Tooltip("Кнопка, по нажатию на которую отправляется команда залпа.")]
    [SerializeField] private Button fireButton;

    [Tooltip("Кнопка, по нажатию на которую включается или выключается автоматический залповый огонь.")]
    [SerializeField] private Button autoFireButton;

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
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        ValidateReferences();
    }

    public void InvokeFire()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        platformController.TryStartSalvo();
    }

    public void InvokeAutoFire()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        if (platformController.IsAutoSalvoFireActive())
        {
            platformController.StopAutoSalvoFire();
            return;
        }

        platformController.StartAutoSalvoFire();
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

        if (platformController == null)
        {
            LogConfigurationError("ArtilleryPanelController: не назначена ссылка на PlatformController.");
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

        return hasAllReferences;
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
