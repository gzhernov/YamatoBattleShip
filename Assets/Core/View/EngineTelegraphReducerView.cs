using UnityEngine;

[DisallowMultipleComponent]
public class EngineTelegraphReducerView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Слайдер редуктора, изменения которого нужно передавать в ShipStatuses.")]
    [SerializeField] private UniversalIntSlider slider;
    [SerializeField] private ShipStatuses shipStatuses;

    [Header("Поведение")]
    [Tooltip("Если включено, при активации компонента слайдер будет синхронизирован с текущим значением редуктора.")]
    [SerializeField] private bool refreshOnEnable = true;

    [Header("Отладка")]
    [SerializeField] private bool showDebugInfo = false;

    private void OnEnable()
    {
        ResolveReferences();

        if (!ValidateSetup())
        {
            return;
        }

        slider.OnValueChanged -= HandleSliderValueChanged;
        slider.OnValueChanged += HandleSliderValueChanged;

        if (refreshOnEnable)
        {
            slider.SetNormalizedValueWithoutNotify(
                shipStatuses.EngineTelegraphReducerPercent / 100f
            );

            if (showDebugInfo)
            {
                Debug.Log(
                    $"EngineTelegraphReducerView: синхронизация редуктора -> шаг {slider.Value}, {shipStatuses.EngineTelegraphReducerPercent}%",
                    this
                );
            }
        }
    }

    private void OnDisable()
    {
        if (slider == null)
        {
            return;
        }

        slider.OnValueChanged -= HandleSliderValueChanged;
    }

    public void HandleSliderValueChanged(int newValue)
    {
        if (shipStatuses == null || slider == null)
            return;

        int reducerPercent = Mathf.RoundToInt(slider.GetNormalizedValue() * 100f);
        shipStatuses.SetEngineTelegraphReducerPercent(reducerPercent);

        if (showDebugInfo)
        {
            Debug.Log(
                $"EngineTelegraphReducerView: новое значение редуктора -> шаг {newValue}, {reducerPercent}%",
                this
            );
        }
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = FindFirstObjectByType<ShipStatuses>();
        }
    }

    private bool ValidateSetup()
    {
        if (slider == null)
        {
            Debug.LogError("EngineTelegraphReducerView: не назначен UniversalIntSlider.", this);
            return false;
        }

        if (shipStatuses == null)
        {
            Debug.LogError("EngineTelegraphReducerView: ShipStatuses не найден.", this);
            return false;
        }

        if (slider.MaxValue <= 0)
        {
            Debug.LogError("EngineTelegraphReducerView: у слайдера maxValue должен быть больше 0.", this);
            return false;
        }

        if (slider.MinValue < 0)
        {
            Debug.LogError("EngineTelegraphReducerView: у слайдера minValue не должен быть отрицательным.", this);
            return false;
        }

        if (slider.MinValue > 1)
        {
            Debug.LogError(
                "EngineTelegraphReducerView: для редуктора ожидается шкала вида 0..N или 1..N.",
                this
            );
            return false;
        }

        return true;
    }
}
