using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TargetPanelView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Контроллер огня главного калибра, в который делегируется команда наведения.")]
    [SerializeField] private FireControlController fireControlController;

    [Tooltip("Подсистема главной цели, из которой читаются bearing и дистанция.")]
    [SerializeField] private MainGunTargetSubSystem mainGunTargetSubSystem;

    [Tooltip("Кнопка, по нажатию на которую отправляется команда наведения.")]
    [SerializeField] private Button aimButton;

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

    public void InvokeAim()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        float bearing = mainGunTargetSubSystem.GetTargetBearing();
        float elevation = mainGunTargetSubSystem.GetTargetDistanse();
        fireControlController.Aim(bearing, elevation);
    }

    private void Subscribe()
    {
        if (isSubscribed || aimButton == null)
        {
            return;
        }

        aimButton.onClick.AddListener(InvokeAim);
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || aimButton == null)
        {
            return;
        }

        aimButton.onClick.RemoveListener(InvokeAim);
        isSubscribed = false;
    }

    private bool HasRequiredReferences()
    {
        bool hasAllReferences = true;

        if (fireControlController == null)
        {
            LogConfigurationError("TargetPanelView: не назначена ссылка на FireControlController.");
            hasAllReferences = false;
        }

        if (mainGunTargetSubSystem == null)
        {
            LogConfigurationError("TargetPanelView: не назначена ссылка на MainGunTargetSubSystem.");
            hasAllReferences = false;
        }

        if (aimButton == null)
        {
            LogConfigurationError("TargetPanelView: не назначена ссылка на кнопку Aim.");
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
