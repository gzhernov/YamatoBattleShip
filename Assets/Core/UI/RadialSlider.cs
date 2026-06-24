using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RadialSlider : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Основная круговая область, по которой рассчитывается значение. На объекте должен быть Graphic с включённым Raycast Target.")]
    [SerializeField] private RectTransform clickAreaRect;

    [Tooltip("RectTransform стрелки, которая изначально направлена вертикально вверх и вращается вокруг своей оси Z.")]
    [SerializeField] private RectTransform pointerRect;

    [Header("Диапазон значений")]
    [Tooltip("Минимальное значение контрола. Этому значению соответствует верхнее положение стрелки.")]
    [SerializeField] private float minValue = 0f;

    [Tooltip("Максимальное значение контрола. Полный круг соответствует переходу от минимального значения к максимальному.")]
    [SerializeField] private float maxValue = 100f;

    [Tooltip("Стартовое значение, которое применяется при запуске.")]
    [SerializeField] private float startValue;

    [Header("Настройки")]
    [Tooltip("Если включено, компонент пишет ошибки конфигурации в лог.")]
    [SerializeField] private bool logConfigurationErrors = true;

    [Tooltip("Если включено, контрол остаётся доступным для чтения и программной установки значения, но игнорирует пользовательский ввод.")]
    [SerializeField] private bool readOnly;

    [Tooltip("Скорость поворота стрелки в градусах в секунду. При значении 0 стрелка переходит в позицию мгновенно.")]
    [SerializeField, Min(0f)] private float pointerRotationSpeed = 360f;

    [Header("События")]
    [Tooltip("Событие вызывается после завершения пользовательского ввода, а также при установке значения через API.")]
    [SerializeField] private FloatEvent onValueChanged;

    private RadialSliderInputProxy clickAreaProxy;
    private RadialSliderInputProxy pointerProxy;
    private bool hasInitialized;
    private bool setupIsValid;
    private bool isDragging;
    private float currentValue;
    private float currentAngleFromUp;
    private float currentRotationZ;
    private float targetRotationZ;
    private float dragStartValue;
    private bool hasPendingDragValueChange;

    public float Value => currentValue;
    public float MinValue => minValue;
    public float MaxValue => maxValue;
    public bool ReadOnly => readOnly;

    public event Action<float> OnValueChanged;

    [Serializable]
    public sealed class FloatEvent : UnityEvent<float>
    {
    }

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
        UpdatePointerVisual(true);
    }

    private void OnDisable()
    {
        isDragging = false;
        hasPendingDragValueChange = false;

        if (clickAreaProxy != null)
        {
            clickAreaProxy.ClearOwner(this);
        }

        if (pointerProxy != null)
        {
            pointerProxy.ClearOwner(this);
        }
    }

    private void OnValidate()
    {
        startValue = ClampValueToRange(startValue);

        if (!Application.isPlaying)
        {
            Initialize(true, false);
        }
    }

    private void Update()
    {
        if (!setupIsValid || pointerRect == null)
        {
            return;
        }

        if (pointerRotationSpeed <= 0f)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(currentRotationZ, targetRotationZ)) > 0.001f)
            {
                currentRotationZ = targetRotationZ;
                ApplyPointerRotation(currentRotationZ);
            }

            return;
        }

        float nextRotationZ = Mathf.MoveTowardsAngle(
            currentRotationZ,
            targetRotationZ,
            pointerRotationSpeed * Time.deltaTime);

        if (Mathf.Abs(Mathf.DeltaAngle(currentRotationZ, nextRotationZ)) <= 0.001f)
        {
            return;
        }

        currentRotationZ = nextRotationZ;
        ApplyPointerRotation(currentRotationZ);
    }

    public void SetValue(float value)
    {
        SetValueInternal(value, true);
    }

    public void SetValueWithoutNotify(float value)
    {
        SetValueInternal(value, false);
    }

    public float GetNormalizedValue()
    {
        if (!HasValidRange())
        {
            return 0f;
        }

        return ValueToNormalized(currentValue);
    }

    public void SetNormalizedValue(float normalizedValue)
    {
        SetNormalizedValueInternal(normalizedValue, true);
    }

    public void SetNormalizedValueWithoutNotify(float normalizedValue)
    {
        SetNormalizedValueInternal(normalizedValue, false);
    }

    public void SetReadOnly(bool value)
    {
        readOnly = value;

        if (readOnly)
        {
            CompleteDragSession();
        }
    }

    internal void HandleClickAreaPointerDown(PointerEventData eventData)
    {
        if (!IsReadyForInput())
        {
            return;
        }

        if (!TryUpdateValueFromPointer(eventData.position, eventData.pressEventCamera))
        {
            return;
        }

        NotifyValueChanged();
    }

    internal void HandlePointerPointerDown(PointerEventData eventData)
    {
        if (!IsReadyForInput())
        {
            return;
        }

        BeginDragSession();
        TryUpdateValueFromPointer(eventData.position, eventData.pressEventCamera);
    }

    internal void HandlePointerBeginDrag(PointerEventData eventData)
    {
        if (!IsReadyForInput())
        {
            return;
        }

        BeginDragSession();
        TryUpdateValueFromPointer(eventData.position, eventData.pressEventCamera);
    }

    internal void HandlePointerDrag(PointerEventData eventData)
    {
        if (!IsReadyForInput() || !isDragging)
        {
            return;
        }

        TryUpdateValueFromPointer(eventData.position, eventData.pressEventCamera);
    }

    internal void HandlePointerUp(PointerEventData eventData)
    {
        CompleteDragSession();
    }

    private void Initialize(bool forceVisualRefresh, bool createInputProxies)
    {
        setupIsValid = ValidateSetup();

        if (!setupIsValid)
        {
            return;
        }

        currentValue = ClampValueToRange(hasInitialized ? currentValue : startValue);
        currentAngleFromUp = ValueToAngle(currentValue);
        targetRotationZ = ConvertAngleFromUpToRotationZ(currentAngleFromUp);

        if (createInputProxies)
        {
            EnsureInputProxies();
        }

        if (forceVisualRefresh || !hasInitialized)
        {
            currentRotationZ = targetRotationZ;
        }
        else if (pointerRect != null)
        {
            currentRotationZ = pointerRect.localEulerAngles.z;
        }

        UpdatePointerVisual(forceVisualRefresh || !hasInitialized);
        hasInitialized = true;
    }

    private void EnsureInputProxies()
    {
        if (!setupIsValid)
        {
            return;
        }

        if (clickAreaRect != null)
        {
            clickAreaProxy = RadialSliderInputProxy.GetOrAdd(clickAreaRect.gameObject);
            clickAreaProxy.Configure(this, true, clickAreaRect == pointerRect);
        }

        if (pointerRect != null)
        {
            pointerProxy = RadialSliderInputProxy.GetOrAdd(pointerRect.gameObject);
            pointerProxy.Configure(this, pointerRect == clickAreaRect, true);
        }
    }

    private bool ValidateSetup()
    {
        if (clickAreaRect == null)
        {
            LogConfigurationError("RadialSlider: не назначен RectTransform круговой области.");
            return false;
        }

        if (pointerRect == null)
        {
            LogConfigurationError("RadialSlider: не назначен RectTransform стрелки.");
            return false;
        }

        if (!HasValidRange())
        {
            LogConfigurationError("RadialSlider: максимальное значение должно быть больше минимального.");
            return false;
        }

        if (!ValidateRaycastTarget(clickAreaRect, "круговой области"))
        {
            return false;
        }

        if (!ValidateRaycastTarget(pointerRect, "стрелки"))
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
        if (readOnly)
        {
            return false;
        }

        if (!hasInitialized)
        {
            Initialize(false, true);
        }

        return TryEnsureValidSetup();
    }

    private void BeginDragSession()
    {
        isDragging = true;
        dragStartValue = currentValue;
        hasPendingDragValueChange = false;
    }

    private void CompleteDragSession()
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        if (hasPendingDragValueChange && !Mathf.Approximately(dragStartValue, currentValue))
        {
            NotifyValueChanged();
        }

        hasPendingDragValueChange = false;
    }

    private bool TryUpdateValueFromPointer(Vector2 screenPoint, Camera eventCamera)
    {
        if (clickAreaRect == null)
        {
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                clickAreaRect,
                screenPoint,
                eventCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        if (localPoint.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        float angleFromUp = GetAngleFromUp(localPoint);
        float value = AngleToValue(angleFromUp);
        bool hasChanged = SetCurrentValueWithoutNotify(value);

        if (isDragging && hasChanged)
        {
            hasPendingDragValueChange = true;
        }

        return true;
    }

    private void SetNormalizedValueInternal(float normalizedValue, bool sendCallback)
    {
        if (!TryEnsureValidSetup())
        {
            return;
        }

        float clampedNormalizedValue = Mathf.Clamp01(normalizedValue);
        float value = Mathf.Lerp(minValue, maxValue, clampedNormalizedValue);
        SetValueInternal(value, sendCallback);
    }

    private void SetValueInternal(float value, bool sendCallback)
    {
        if (!TryEnsureValidSetup())
        {
            return;
        }

        bool hasChanged = SetCurrentValueWithoutNotify(value);

        if (!sendCallback || !hasChanged)
        {
            return;
        }

        NotifyValueChanged();
    }

    private bool SetCurrentValueWithoutNotify(float value)
    {
        float clampedValue = ClampValueToRange(value);

        if (hasInitialized && Mathf.Approximately(currentValue, clampedValue))
        {
            UpdatePointerVisual(false);
            return false;
        }

        currentValue = clampedValue;
        currentAngleFromUp = ValueToAngle(currentValue);
        targetRotationZ = ConvertAngleFromUpToRotationZ(currentAngleFromUp);
        hasInitialized = true;
        UpdatePointerVisual(pointerRotationSpeed <= 0f);
        return true;
    }

    private void UpdatePointerVisual(bool forceRefresh)
    {
        if (!setupIsValid)
        {
            return;
        }

        if (forceRefresh)
        {
            currentRotationZ = targetRotationZ;
            ApplyPointerRotation(currentRotationZ);
            return;
        }

        if (pointerRotationSpeed <= 0f)
        {
            currentRotationZ = targetRotationZ;
            ApplyPointerRotation(currentRotationZ);
        }
    }

    private float ClampValueToRange(float value)
    {
        if (!HasValidRange())
        {
            return minValue;
        }

        return Mathf.Clamp(value, minValue, maxValue);
    }

    private float ValueToNormalized(float value)
    {
        return Mathf.InverseLerp(minValue, maxValue, ClampValueToRange(value));
    }

    private float ValueToAngle(float value)
    {
        float normalizedValue = ValueToNormalized(value);
        return normalizedValue * 360f;
    }

    private float AngleToValue(float angleFromUp)
    {
        float normalizedAngle = Mathf.Repeat(angleFromUp, 360f) / 360f;
        return Mathf.Lerp(minValue, maxValue, normalizedAngle);
    }

    private float GetAngleFromUp(Vector2 localPoint)
    {
        return Mathf.Repeat(Mathf.Atan2(localPoint.x, localPoint.y) * Mathf.Rad2Deg, 360f);
    }

    private float ConvertAngleFromUpToRotationZ(float angleFromUp)
    {
        return -angleFromUp;
    }

    private void ApplyPointerRotation(float rotationZ)
    {
        if (pointerRect == null)
        {
            return;
        }

        Vector3 localEulerAngles = pointerRect.localEulerAngles;
        localEulerAngles.z = rotationZ;
        pointerRect.localEulerAngles = localEulerAngles;
    }

    private void NotifyValueChanged()
    {
        OnValueChanged?.Invoke(currentValue);
        onValueChanged?.Invoke(currentValue);
    }

    private bool ValidateRaycastTarget(RectTransform targetRect, string targetName)
    {
        if (targetRect == null)
        {
            LogConfigurationError($"RadialSlider: не назначен RectTransform для {targetName}.");
            return false;
        }

        Graphic graphic = targetRect.GetComponent<Graphic>();

        if (graphic == null)
        {
            LogConfigurationError(
                $"RadialSlider: на объекте {targetName} должен быть компонент Graphic для получения кликов.");
            return false;
        }

        if (!graphic.raycastTarget)
        {
            LogConfigurationError(
                $"RadialSlider: у компонента Graphic на объекте {targetName} должен быть включён Raycast Target.");
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
internal sealed class RadialSliderInputProxy : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler
{
    [SerializeField] private bool handlesClickAreaInput;
    [SerializeField] private bool handlesPointerInput;

    private RadialSlider owner;

    internal static RadialSliderInputProxy GetOrAdd(GameObject target)
    {
        RadialSliderInputProxy proxy = target.GetComponent<RadialSliderInputProxy>();

        if (proxy == null)
        {
            proxy = target.AddComponent<RadialSliderInputProxy>();
        }

        return proxy;
    }

    internal void Configure(RadialSlider radialSlider, bool handleClickAreaInput, bool handlePointerInput)
    {
        owner = radialSlider;
        handlesClickAreaInput = handleClickAreaInput;
        handlesPointerInput = handlePointerInput;
    }

    internal void ClearOwner(RadialSlider radialSlider)
    {
        if (owner == radialSlider)
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

        if (handlesPointerInput)
        {
            owner.HandlePointerPointerDown(eventData);
            return;
        }

        if (handlesClickAreaInput)
        {
            owner.HandleClickAreaPointerDown(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (owner == null || !handlesPointerInput)
        {
            return;
        }

        owner.HandlePointerUp(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (owner == null || !handlesPointerInput)
        {
            return;
        }

        owner.HandlePointerBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (owner == null || !handlesPointerInput)
        {
            return;
        }

        owner.HandlePointerDrag(eventData);
    }
}
