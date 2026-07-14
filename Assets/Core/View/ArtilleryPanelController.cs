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

    private void Subscribe()
    {
        if (isSubscribed || fireButton == null)
        {
            return;
        }

        fireButton.onClick.AddListener(InvokeFire);
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || fireButton == null)
        {
            return;
        }

        fireButton.onClick.RemoveListener(InvokeFire);
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
