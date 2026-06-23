using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TurretController : MonoBehaviour
{
    [Header("Управление башней")]
    [SerializeField] private TurretWithCannons turret;
    [SerializeField] private Button aimButton;
    [SerializeField] private Slider bearingSlider;
    [SerializeField] private Slider elevationSlider;
    [SerializeField] private TMP_Text bearingValueText;
    [SerializeField] private TMP_Text elevationValueText;

    private bool listenersRegistered;

    private void Awake()
    {
        ValidateReferences();
        RefreshValueTexts();
    }

    private void OnEnable()
    {
        if (!HasRequiredUiReferences())
        {
            return;
        }

        if (!listenersRegistered)
        {
            aimButton.onClick.AddListener(OnAimButtonClicked);
            bearingSlider.onValueChanged.AddListener(OnBearingValueChanged);
            elevationSlider.onValueChanged.AddListener(OnElevationValueChanged);
            listenersRegistered = true;
        }

        RefreshValueTexts();
    }

    private void OnDisable()
    {
        if (!listenersRegistered)
        {
            return;
        }

        aimButton.onClick.RemoveListener(OnAimButtonClicked);
        bearingSlider.onValueChanged.RemoveListener(OnBearingValueChanged);
        elevationSlider.onValueChanged.RemoveListener(OnElevationValueChanged);
        listenersRegistered = false;
    }

    private void OnAimButtonClicked()
    {
        if (turret == null)
        {
            Debug.LogError("Не назначена ссылка на TurretWithCannons для кнопки Aim.", this);
            return;
        }

        if (!HasRequiredUiReferences())
        {
            Debug.LogError("Не все ссылки UI назначены для контроллера башни.", this);
            return;
        }

        turret.Aim(bearingSlider.value, elevationSlider.value);
    }

    private void OnBearingValueChanged(float value)
    {
        UpdateValueText(bearingValueText, value);
    }

    private void OnElevationValueChanged(float value)
    {
        UpdateValueText(elevationValueText, value);
    }

    private void RefreshValueTexts()
    {
        if (bearingSlider != null)
        {
            UpdateValueText(bearingValueText, bearingSlider.value);
        }

        if (elevationSlider != null)
        {
            UpdateValueText(elevationValueText, elevationSlider.value);
        }
    }

    private void UpdateValueText(TMP_Text targetText, float value)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = value.ToString("0.0");
    }

    private void ValidateReferences()
    {
        if (turret == null)
        {
            Debug.LogError("Не назначена ссылка на TurretWithCannons.", this);
        }

        if (!HasRequiredUiReferences())
        {
            Debug.LogError("Не назначены все обязательные ссылки UI контроллера башни.", this);
        }
    }

    private bool HasRequiredUiReferences()
    {
        return aimButton != null
            && bearingSlider != null
            && elevationSlider != null
            && bearingValueText != null
            && elevationValueText != null;
    }
}
