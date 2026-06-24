using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PortStarboardRadialSlider : MonoBehaviour
{
    private enum Side
    {
        Port = 0,
        Starboard = 1
    }

    [Header("Ссылки")]
    [Tooltip("Основная круговая область, по которой рассчитывается положение. На объекте должен быть Graphic с включённым Raycast Target.")]
    [SerializeField] private RectTransform clickAreaRect;

    [Tooltip("RectTransform стрелки, которая изначально направлена вертикально вверх и вращается вокруг своей оси Z.")]
    [SerializeField] private RectTransform pointerRect;

    [Header("Стартовое значение")]
    [Tooltip("Стартовое строковое значение в формате P0..P180 или S0..S180.")]
    [SerializeField] private string startValue = "S0";

    [Header("Настройки")]
    [Tooltip("Если включено, компонент пишет ошибки конфигурации и невалидного ввода в лог.")]
    [SerializeField] private bool logConfigurationErrors = true;

    [Tooltip("Скорость поворота стрелки в градусах в секунду. При значении 0 стрелка переходит в позицию мгновенно.")]
    [SerializeField, Min(0f)] private float pointerRotationSpeed = 360f;

    [Header("События")]
    [Tooltip("Событие вызывается после завершения пользовательского ввода, а также при установке значения через API.")]
    [SerializeField] private StringEvent onValueChanged;

    private PortStarboardRadialSliderInputProxy clickAreaProxy;
    private PortStarboardRadialSliderInputProxy pointerProxy;
    private bool hasInitialized;
    private bool setupIsValid;
    private bool isDragging;
    private int currentMagnitude;
    private Side currentSide = Side.Starboard;
    private float currentAngleFromUp;
    private float currentRotationZ;
    private float targetRotationZ;
    private string dragStartValue = "S0";
    private bool hasPendingDragValueChange;

    public string Value => FormatValue(currentSide, currentMagnitude);

    public event Action<string> OnValueChanged;

    [Serializable]
    public sealed class StringEvent : UnityEvent<string>
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
        if (!Application.isPlaying)
        {
            if (TryParseValue(startValue, out Side parsedSide, out int parsedMagnitude))
            {
                startValue = FormatValue(parsedSide, parsedMagnitude);
            }

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

    public void SetValue(string value)
    {
        SetValueInternal(value, true);
    }

    public void SetValueWithoutNotify(string value)
    {
        SetValueInternal(value, false);
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

        if (!hasInitialized)
        {
            if (!TryParseValue(startValue, out Side parsedSide, out int parsedMagnitude))
            {
                parsedSide = Side.Starboard;
                parsedMagnitude = 0;
            }

            currentSide = parsedSide;
            currentMagnitude = parsedMagnitude;
        }

        currentAngleFromUp = SideAndMagnitudeToAngle(currentSide, currentMagnitude);
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
            clickAreaProxy = PortStarboardRadialSliderInputProxy.GetOrAdd(clickAreaRect.gameObject);
            clickAreaProxy.Configure(this, true, clickAreaRect == pointerRect);
        }

        if (pointerRect != null)
        {
            pointerProxy = PortStarboardRadialSliderInputProxy.GetOrAdd(pointerRect.gameObject);
            pointerProxy.Configure(this, pointerRect == clickAreaRect, true);
        }
    }

    private bool ValidateSetup()
    {
        if (clickAreaRect == null)
        {
            LogConfigurationError("PortStarboardRadialSlider: не назначен RectTransform круговой области.");
            return false;
        }

        if (pointerRect == null)
        {
            LogConfigurationError("PortStarboardRadialSlider: не назначен RectTransform стрелки.");
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

    private void BeginDragSession()
    {
        isDragging = true;
        dragStartValue = Value;
        hasPendingDragValueChange = false;
    }

    private void CompleteDragSession()
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        if (hasPendingDragValueChange && !string.Equals(dragStartValue, Value, StringComparison.Ordinal))
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
        Side side = ResolveSideFromPointer(localPoint, angleFromUp);
        int magnitude = AngleToMagnitude(side, angleFromUp);
        bool hasChanged = SetCurrentValueWithoutNotify(side, magnitude);

        if (isDragging && hasChanged)
        {
            hasPendingDragValueChange = true;
        }

        return true;
    }

    private void SetValueInternal(string value, bool sendCallback)
    {
        if (!TryEnsureValidSetup())
        {
            return;
        }

        if (!TryParseValue(value, out Side parsedSide, out int parsedMagnitude))
        {
            LogConfigurationError(
                $"PortStarboardRadialSlider: значение '{value}' имеет неверный формат. Ожидается P0..P180 или S0..S180.");
            return;
        }

        bool hasChanged = SetCurrentValueWithoutNotify(parsedSide, parsedMagnitude);

        if (!sendCallback || !hasChanged)
        {
            return;
        }

        NotifyValueChanged();
    }

    private bool SetCurrentValueWithoutNotify(Side side, int magnitude)
    {
        int clampedMagnitude = Mathf.Clamp(magnitude, 0, 180);

        if (hasInitialized && currentSide == side && currentMagnitude == clampedMagnitude)
        {
            UpdatePointerVisual(false);
            return false;
        }

        currentSide = side;
        currentMagnitude = clampedMagnitude;
        currentAngleFromUp = SideAndMagnitudeToAngle(currentSide, currentMagnitude);
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

    private Side ResolveSideFromPointer(Vector2 localPoint, float angleFromUp)
    {
        if (Mathf.Abs(localPoint.x) <= 0.0001f && angleFromUp <= 0.5f)
        {
            return currentSide;
        }

        if (Mathf.Abs(localPoint.x) <= 0.0001f)
        {
            return currentSide;
        }

        return localPoint.x < 0f ? Side.Port : Side.Starboard;
    }

    private int AngleToMagnitude(Side side, float angleFromUp)
    {
        float normalizedAngle = Mathf.Repeat(angleFromUp, 360f);

        if (side == Side.Starboard)
        {
            float clampedAngle = Mathf.Clamp(normalizedAngle, 0f, 180f);
            return Mathf.Clamp(Mathf.RoundToInt(clampedAngle), 0, 180);
        }

        float portAngle = normalizedAngle > 180f ? 360f - normalizedAngle : 0f;
        return Mathf.Clamp(Mathf.RoundToInt(portAngle), 0, 180);
    }

    private float SideAndMagnitudeToAngle(Side side, int magnitude)
    {
        int clampedMagnitude = Mathf.Clamp(magnitude, 0, 180);

        if (side == Side.Starboard)
        {
            return clampedMagnitude;
        }

        if (clampedMagnitude == 0)
        {
            return 0f;
        }

        return 360f - clampedMagnitude;
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
        string value = Value;
        OnValueChanged?.Invoke(value);
        onValueChanged?.Invoke(value);
    }

    private bool TryParseValue(string value, out Side side, out int magnitude)
    {
        side = Side.Starboard;
        magnitude = 0;

        if (string.IsNullOrEmpty(value) || value.Length < 2)
        {
            return false;
        }

        char prefix = char.ToUpperInvariant(value[0]);

        if (prefix == 'P')
        {
            side = Side.Port;
        }
        else if (prefix == 'S')
        {
            side = Side.Starboard;
        }
        else
        {
            return false;
        }

        string magnitudeText = value.Substring(1);

        if (!int.TryParse(magnitudeText, NumberStyles.None, CultureInfo.InvariantCulture, out magnitude))
        {
            return false;
        }

        return magnitude >= 0 && magnitude <= 180;
    }

    private string FormatValue(Side side, int magnitude)
    {
        return $"{(side == Side.Port ? "P" : "S")}{Mathf.Clamp(magnitude, 0, 180)}";
    }

    private bool ValidateRaycastTarget(RectTransform targetRect, string targetName)
    {
        if (targetRect == null)
        {
            LogConfigurationError($"PortStarboardRadialSlider: не назначен RectTransform для {targetName}.");
            return false;
        }

        Graphic graphic = targetRect.GetComponent<Graphic>();

        if (graphic == null)
        {
            LogConfigurationError(
                $"PortStarboardRadialSlider: на объекте {targetName} должен быть компонент Graphic для получения кликов.");
            return false;
        }

        if (!graphic.raycastTarget)
        {
            LogConfigurationError(
                $"PortStarboardRadialSlider: у компонента Graphic на объекте {targetName} должен быть включён Raycast Target.");
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
internal sealed class PortStarboardRadialSliderInputProxy : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler
{
    [SerializeField] private bool handlesClickAreaInput;
    [SerializeField] private bool handlesPointerInput;

    private PortStarboardRadialSlider owner;

    internal static PortStarboardRadialSliderInputProxy GetOrAdd(GameObject target)
    {
        PortStarboardRadialSliderInputProxy proxy = target.GetComponent<PortStarboardRadialSliderInputProxy>();

        if (proxy == null)
        {
            proxy = target.AddComponent<PortStarboardRadialSliderInputProxy>();
        }

        return proxy;
    }

    internal void Configure(PortStarboardRadialSlider radialSlider, bool handleClickAreaInput, bool handlePointerInput)
    {
        owner = radialSlider;
        handlesClickAreaInput = handleClickAreaInput;
        handlesPointerInput = handlePointerInput;
    }

    internal void ClearOwner(PortStarboardRadialSlider radialSlider)
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
