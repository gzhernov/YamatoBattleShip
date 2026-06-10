using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UniversalIntSlider : MonoBehaviour
{
    public enum SliderAxis
    {
        Horizontal = 0,
        Vertical = 1
    }

    [Header("Ссылки")]
    [Tooltip("Основной RectTransform шкалы, по которому рассчитывается позиция значения.")]
    [SerializeField] private RectTransform trackRect;

    [Tooltip("RectTransform указателя, который будет перемещаться по шкале.")]
    [SerializeField] private RectTransform handleRect;

    [Header("Диапазон")]
    [Tooltip("Минимальное целое значение шкалы.")]
    [SerializeField] private int minValue = 0;

    [Tooltip("Максимальное целое значение шкалы.")]
    [SerializeField] private int maxValue = 10;

    [Tooltip("Начальное значение, которое применяется при запуске.")]
    [SerializeField] private int startValue;

    [Header("Настройки")]
    [Tooltip("Ось, вдоль которой перемещается указатель.")]
    [SerializeField] private SliderAxis axis = SliderAxis.Horizontal;

    [Tooltip("Если включено, компонент будет писать ошибки конфигурации в лог.")]
    [SerializeField] private bool logConfigurationErrors = true;

    [Header("События")]
    [SerializeField] private UnityEvent<int> onValueChanged;

    private UniversalIntSliderInputProxy trackProxy;
    private UniversalIntSliderInputProxy handleProxy;
    private bool isDraggingHandle;
    private bool hasInitialized;
    private bool setupIsValid;
    private int currentValue;

    public int Value => currentValue;

    public event Action<int> OnValueChanged;

    private void Awake()
    {
        Initialize(false, true);
    }

    private void OnEnable()
    {
        if (!hasInitialized)
        {
            Initialize(false, true);
        }

        EnsureInputProxies();
        UpdateHandleVisual();
    }

    private void OnDisable()
    {
        isDraggingHandle = false;

        if (trackProxy != null)
        {
            trackProxy.ClearOwner(this);
        }

        if (handleProxy != null)
        {
            handleProxy.ClearOwner(this);
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateHandleVisual();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            startValue = ClampValueToRange(startValue);
            Initialize(true, false);
        }
    }

    public void SetValue(int value)
    {
        SetValueInternal(value, true);
    }

    public void SetValueWithoutNotify(int value)
    {
        SetValueInternal(value, false);
    }

    public void SetNormalizedValue(float normalizedValue)
    {
        SetNormalizedValueInternal(normalizedValue, true);
    }

    public void SetNormalizedValueWithoutNotify(float normalizedValue)
    {
        SetNormalizedValueInternal(normalizedValue, false);
    }

    public float GetNormalizedValue()
    {
        if (!HasValidRange())
        {
            return 0f;
        }

        return ValueToNormalized(currentValue);
    }

    internal void HandleTrackPointerDown(PointerEventData eventData)
    {
        if (!IsReadyForInput())
        {
            return;
        }

        UpdateValueFromPointer(eventData, true);
    }

    internal void HandleTrackDrag(PointerEventData eventData)
    {
        if (!IsReadyForInput() || !isDraggingHandle)
        {
            return;
        }

        UpdateValueFromPointer(eventData, true);
    }

    internal void HandleTrackPointerUp(PointerEventData eventData)
    {
        isDraggingHandle = false;
    }

    internal void HandleHandlePointerDown(PointerEventData eventData)
    {
        if (!IsReadyForInput())
        {
            return;
        }

        isDraggingHandle = true;
        UpdateValueFromPointer(eventData, true);
    }

    internal void HandleHandleBeginDrag(PointerEventData eventData)
    {
        if (!IsReadyForInput())
        {
            return;
        }

        isDraggingHandle = true;
        UpdateValueFromPointer(eventData, true);
    }

    internal void HandleHandleDrag(PointerEventData eventData)
    {
        if (!IsReadyForInput())
        {
            return;
        }

        isDraggingHandle = true;
        UpdateValueFromPointer(eventData, true);
    }

    internal void HandleHandlePointerUp(PointerEventData eventData)
    {
        isDraggingHandle = false;
    }

    private void Initialize(bool forceVisualRefresh, bool createInputProxies)
    {
        setupIsValid = ValidateSetup();

        if (!setupIsValid)
        {
            return;
        }

        currentValue = ClampValueToRange(hasInitialized ? currentValue : startValue);

        if (createInputProxies)
        {
            EnsureInputProxies();
        }

        UpdateHandleVisual(forceVisualRefresh);
        hasInitialized = true;
    }

    private void EnsureInputProxies()
    {
        if (!setupIsValid)
        {
            return;
        }

        if (trackRect != null)
        {
            trackProxy = UniversalIntSliderInputProxy.GetOrAdd(
                trackRect.gameObject,
                true,
                trackRect == handleRect);
            trackProxy.SetOwner(this);
        }

        if (handleRect != null)
        {
            handleProxy = UniversalIntSliderInputProxy.GetOrAdd(
                handleRect.gameObject,
                handleRect == trackRect,
                true);
            handleProxy.SetOwner(this);
        }
    }

    private bool ValidateSetup()
    {
        if (trackRect == null)
        {
            LogConfigurationError("UniversalIntSlider: Не назначен RectTransform шкалы.");
            return false;
        }

        if (handleRect == null)
        {
            LogConfigurationError("UniversalIntSlider: Не назначен RectTransform указателя.");
            return false;
        }

        if (!HasValidRange())
        {
            LogConfigurationError("UniversalIntSlider: Максимальное значение должно быть больше минимального.");
            return false;
        }

        if (!ValidateRaycastTarget(trackRect, "шкалы"))
        {
            return false;
        }

        if (!ValidateRaycastTarget(handleRect, "указателя"))
        {
            return false;
        }

        return true;
    }

    private bool HasValidRange()
    {
        return maxValue > minValue;
    }

    private bool TryEnsureValidSetup()
    {
        if (setupIsValid)
        {
            return true;
        }

        setupIsValid = ValidateSetup();
        return setupIsValid;
    }

    private bool IsReadyForInput()
    {
        if (!hasInitialized)
        {
            Initialize(false, true);
        }

        return TryEnsureValidSetup();
    }

    private void UpdateValueFromPointer(PointerEventData eventData, bool sendCallback)
    {
        RectTransform effectiveTrackRect = trackRect;

        if (effectiveTrackRect == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                effectiveTrackRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float normalized = LocalPointToNormalized(localPoint, effectiveTrackRect);
        int roundedValue = NormalizedToValue(normalized);
        SetValueInternal(roundedValue, sendCallback);
    }

    private float LocalPointToNormalized(Vector2 localPoint, RectTransform rect)
    {
        Rect localRect = rect.rect;

        if (axis == SliderAxis.Horizontal)
        {
            float width = Mathf.Max(localRect.width, 0.0001f);
            return Mathf.Clamp01((localPoint.x - localRect.xMin) / width);
        }

        float height = Mathf.Max(localRect.height, 0.0001f);
        return Mathf.Clamp01((localPoint.y - localRect.yMin) / height);
    }

    private int NormalizedToValue(float normalizedValue)
    {
        float rawValue = Mathf.Lerp(minValue, maxValue, Mathf.Clamp01(normalizedValue));
        return Mathf.Clamp(Mathf.RoundToInt(rawValue), minValue, maxValue);
    }

    private float ValueToNormalized(int value)
    {
        return Mathf.InverseLerp(minValue, maxValue, ClampValueToRange(value));
    }

    private int ClampValueToRange(int value)
    {
        if (!HasValidRange())
        {
            return minValue;
        }

        return Mathf.Clamp(value, minValue, maxValue);
    }

    private void SetNormalizedValueInternal(float normalizedValue, bool sendCallback)
    {
        if (!TryEnsureValidSetup())
        {
            return;
        }

        int roundedValue = NormalizedToValue(Mathf.Clamp01(normalizedValue));
        SetValueInternal(roundedValue, sendCallback);
    }

    private void SetValueInternal(int value, bool sendCallback)
    {
        if (!TryEnsureValidSetup())
        {
            return;
        }

        int clampedValue = ClampValueToRange(value);

        if (currentValue == clampedValue && hasInitialized)
        {
            UpdateHandleVisual();
            return;
        }

        currentValue = clampedValue;
        hasInitialized = true;
        UpdateHandleVisual();

        if (!sendCallback)
        {
            return;
        }

        OnValueChanged?.Invoke(currentValue);
        onValueChanged?.Invoke(currentValue);
    }

    private void UpdateHandleVisual()
    {
        UpdateHandleVisual(false);
    }

    private void UpdateHandleVisual(bool forceRefresh)
    {
        if (!setupIsValid || handleRect == null || trackRect == null)
        {
            return;
        }

        float normalized = ValueToNormalized(currentValue);
        Vector2 anchoredPosition = handleRect.anchoredPosition;

        if (axis == SliderAxis.Horizontal)
        {
            float targetX = Mathf.Lerp(trackRect.rect.xMin, trackRect.rect.xMax, normalized);

            if (forceRefresh || !Mathf.Approximately(anchoredPosition.x, targetX))
            {
                anchoredPosition.x = targetX;
                handleRect.anchoredPosition = anchoredPosition;
            }

            return;
        }

        float targetY = Mathf.Lerp(trackRect.rect.yMin, trackRect.rect.yMax, normalized);

        if (forceRefresh || !Mathf.Approximately(anchoredPosition.y, targetY))
        {
            anchoredPosition.y = targetY;
            handleRect.anchoredPosition = anchoredPosition;
        }
    }

    private bool ValidateRaycastTarget(RectTransform targetRect, string targetName)
    {
        if (targetRect == null)
        {
            LogConfigurationError($"UniversalIntSlider: Не назначен RectTransform для {targetName}.");
            return false;
        }

        Graphic graphic = targetRect.GetComponent<Graphic>();

        if (graphic == null)
        {
            LogConfigurationError(
                $"UniversalIntSlider: На объекте {targetName} должен быть компонент Graphic (например Image) для получения кликов.");
            return false;
        }

        if (!graphic.raycastTarget)
        {
            LogConfigurationError(
                $"UniversalIntSlider: У компонента Graphic на объекте {targetName} должен быть включен Raycast Target.");
            return false;
        }

        return true;
    }

    private void LogConfigurationError(string message)
    {
        if (logConfigurationErrors)
        {
            Debug.LogError(message, this);
        }
    }
}

[DisallowMultipleComponent]
internal sealed class UniversalIntSliderInputProxy : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler
{
    [SerializeField] private bool handlesTrackInput;
    [SerializeField] private bool handlesHandleInput;

    private UniversalIntSlider owner;

    internal static UniversalIntSliderInputProxy GetOrAdd(GameObject target, bool handleTrackInput, bool handleHandleInput)
    {
        UniversalIntSliderInputProxy proxy = target.GetComponent<UniversalIntSliderInputProxy>();

        if (proxy == null)
        {
            proxy = target.AddComponent<UniversalIntSliderInputProxy>();
        }

        proxy.handlesTrackInput = handleTrackInput;
        proxy.handlesHandleInput = handleHandleInput;
        return proxy;
    }

    internal void SetOwner(UniversalIntSlider slider)
    {
        owner = slider;
    }

    internal void ClearOwner(UniversalIntSlider slider)
    {
        if (owner == slider)
        {
            owner = null;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (owner == null)
        {
            return;
        }

        if (handlesHandleInput)
        {
            owner.HandleHandlePointerDown(eventData);
            return;
        }

        if (handlesTrackInput)
        {
            owner.HandleTrackPointerDown(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (owner == null)
        {
            return;
        }

        if (handlesHandleInput)
        {
            owner.HandleHandlePointerUp(eventData);
            return;
        }

        if (handlesTrackInput)
        {
            owner.HandleTrackPointerUp(eventData);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (owner == null)
        {
            return;
        }

        if (handlesHandleInput)
        {
            owner.HandleHandleBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (owner == null)
        {
            return;
        }

        if (handlesHandleInput)
        {
            owner.HandleHandleDrag(eventData);
            return;
        }

        if (handlesTrackInput)
        {
            owner.HandleTrackDrag(eventData);
        }
    }
}
