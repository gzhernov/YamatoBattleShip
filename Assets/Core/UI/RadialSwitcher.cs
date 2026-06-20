using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RadialSwitcher : MonoBehaviour
{
    public enum SwitchMode
    {
        SequentialLoop = 0,
        ClickToPosition = 1
    }

    [Serializable]
    public struct RadialPosition
    {
        [Tooltip("Уникальный идентификатор положения, который будет отправлен во внешние события.")]
        public string id;

        [Tooltip("Угол положения в градусах относительно вертикали вверх: 0 = вверх, положительные значения = вправо, отрицательные = влево.")]
        public float angleFromUp;
    }

    [Header("Ссылки")]
    [Tooltip("Кликабельная область радиального переключателя. На объекте должен быть Graphic с включённым Raycast Target.")]
    [SerializeField] private RectTransform clickAreaRect;

    [Tooltip("RectTransform стрелки, который будет поворачиваться между рабочими положениями.")]
    [SerializeField] private RectTransform pointerRect;

    [Header("Положения")]
    [Tooltip("Список рабочих положений переключателя.")]
    [SerializeField] private RadialPosition[] positions = Array.Empty<RadialPosition>();

    [Tooltip("Стартовый ID положения. Если пусто или не найдено, будет использован стартовый индекс.")]
    [SerializeField] private string startPositionId = string.Empty;

    [Tooltip("Стартовый индекс положения, который используется как запасной вариант.")]
    [SerializeField] private int startPositionIndex;

    [Header("Поведение")]
    [Tooltip("Режим обработки клика по радиальному переключателю.")]
    [SerializeField] private SwitchMode switchMode = SwitchMode.SequentialLoop;

    [Tooltip("Если включено, правый клик в циклическом режиме будет переключать на предыдущее положение.")]
    [SerializeField] private bool rightClickMovesBackward = true;

    [Tooltip("Если включено, стрелка будет сразу синхронизироваться при включении компонента.")]
    [SerializeField] private bool refreshOnEnable = true;

    [Tooltip("Если включено, стрелка будет поворачиваться плавно.")]
    [SerializeField] private bool smoothMovement = true;

    [Tooltip("Скорость плавного поворота стрелки.")]
    [SerializeField, Min(0f)] private float smoothSpeed = 15f;

    [Header("Звук")]
    [Tooltip("Источник звука для щелчка переключателя. Если не назначен, будет использован AudioSource с этого объекта.")]
    [SerializeField] private AudioSource switchAudioSource;

    [Tooltip("Общий звуковой эффект, который проигрывается при успешной смене положения.")]
    [SerializeField] private AudioClip switchAudioClip;

    [Tooltip("Громкость общего звукового эффекта.")]
    [SerializeField, Range(0f, 1f)] private float switchAudioVolume = 1f;

    [Header("События")]
    [Tooltip("Событие вызывается при успешной смене положения и передаёт ID нового положения.")]
    [SerializeField] private StringEvent onPositionChanged;

    [Header("Отладка")]
    [Tooltip("Если включено, компонент будет писать подробную информацию в лог.")]
    [SerializeField] private bool logDebugInfo = false;

    private RadialSwitcherInputProxy clickAreaProxy;
    private RadialSwitcherInputProxy pointerProxy;
    private bool hasInitialized;
    private bool setupIsValid;
    private int currentIndex = -1;
    private float currentRotationZ;
    private float targetRotationZ;

    public string CurrentPositionId => IsValidIndex(currentIndex) ? positions[currentIndex].id : string.Empty;
    public int CurrentIndex => currentIndex;

    public event Action<string> OnPositionChanged;

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

        if (refreshOnEnable)
        {
            ApplyStartPosition(true);
        }
        else
        {
            SyncCurrentRotationFromVisual();
        }
    }

    private void OnDisable()
    {
        if (clickAreaProxy != null)
        {
            clickAreaProxy.ClearOwner(this);
        }

        if (pointerProxy != null)
        {
            pointerProxy.ClearOwner(this);
        }
    }

    private void Update()
    {
        if (!smoothMovement || pointerRect == null)
        {
            return;
        }

        currentRotationZ = Mathf.LerpAngle(currentRotationZ, targetRotationZ, Time.deltaTime * smoothSpeed);
        SetPointerRotation(currentRotationZ);
    }

    private void OnValidate()
    {
        startPositionIndex = Mathf.Max(0, startPositionIndex);

        if (switchAudioSource == null)
        {
            switchAudioSource = GetComponent<AudioSource>();
        }

        if (!Application.isPlaying)
        {
            Initialize(true, false);
        }
    }

    public void SetPositionById(string positionId)
    {
        int index = FindPositionIndexById(positionId);

        if (index < 0)
        {
            LogDebug($"RadialSwitcher: положение с ID '{positionId}' не найдено.");
            return;
        }

        SetPositionByIndexInternal(index, true, false);
    }

    public void SetPositionWithoutNotify(string positionId)
    {
        int index = FindPositionIndexById(positionId);

        if (index < 0)
        {
            LogDebug($"RadialSwitcher: положение с ID '{positionId}' не найдено.");
            return;
        }

        SetPositionByIndexInternal(index, false, false);
    }

    public void SetPositionByIndex(int index)
    {
        SetPositionByIndexInternal(index, true, false);
    }

    public void SetPositionWithoutNotify(int index)
    {
        SetPositionByIndexInternal(index, false, false);
    }

    internal void HandlePointerDown(PointerEventData eventData)
    {
        if (!IsReadyForInput())
        {
            return;
        }

        if (switchMode == SwitchMode.SequentialLoop)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (!rightClickMovesBackward)
                {
                    return;
                }

                StepPosition(-1);
                return;
            }

            StepPosition(1);
            return;
        }

        TrySelectPositionFromPointer(eventData);
    }

    private void Initialize(bool forceVisualRefresh, bool createInputProxies)
    {
        setupIsValid = ValidateSetup();

        if (!setupIsValid)
        {
            return;
        }

        if (switchAudioSource == null)
        {
            switchAudioSource = GetComponent<AudioSource>();
        }

        if (currentIndex < 0 || !IsValidIndex(currentIndex))
        {
            currentIndex = ResolveStartPositionIndex();
        }

        targetRotationZ = ConvertAngleFromUpToRotationZ(positions[currentIndex].angleFromUp);

        if (forceVisualRefresh || !hasInitialized || !smoothMovement)
        {
            currentRotationZ = targetRotationZ;
            SetPointerRotation(currentRotationZ);
        }
        else
        {
            SyncCurrentRotationFromVisual();
        }

        if (createInputProxies)
        {
            EnsureInputProxies();
        }

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
            clickAreaProxy = RadialSwitcherInputProxy.GetOrAdd(clickAreaRect.gameObject);
            clickAreaProxy.SetOwner(this);
        }

        if (pointerRect != null && pointerRect != clickAreaRect)
        {
            Graphic pointerGraphic = pointerRect.GetComponent<Graphic>();

            if (pointerGraphic != null && pointerGraphic.raycastTarget)
            {
                pointerProxy = RadialSwitcherInputProxy.GetOrAdd(pointerRect.gameObject);
                pointerProxy.SetOwner(this);
            }
        }
    }

    private bool ValidateSetup()
    {
        if (clickAreaRect == null)
        {
            Debug.LogError("RadialSwitcher: не назначена кликабельная область.", this);
            return false;
        }

        if (pointerRect == null)
        {
            Debug.LogError("RadialSwitcher: не назначен RectTransform стрелки.", this);
            return false;
        }

        if (!ValidateRaycastTarget(clickAreaRect, "кликабельной области"))
        {
            return false;
        }

        if (positions == null || positions.Length == 0)
        {
            Debug.LogError("RadialSwitcher: список положений пуст.", this);
            return false;
        }

        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < positions.Length; i++)
        {
            string positionId = positions[i].id;

            if (string.IsNullOrWhiteSpace(positionId))
            {
                Debug.LogError($"RadialSwitcher: у положения с индексом {i} не задан ID.", this);
                return false;
            }

            if (!uniqueIds.Add(positionId))
            {
                Debug.LogError($"RadialSwitcher: найден дублирующийся ID положения '{positionId}'.", this);
                return false;
            }
        }

        int resolvedStartIndex = ResolveStartPositionIndex();

        if (!IsValidIndex(resolvedStartIndex))
        {
            Debug.LogError("RadialSwitcher: не удалось определить корректное стартовое положение.", this);
            return false;
        }

        return true;
    }

    private bool ValidateRaycastTarget(RectTransform targetRect, string targetName)
    {
        Graphic graphic = targetRect.GetComponent<Graphic>();

        if (graphic == null)
        {
            Debug.LogError(
                $"RadialSwitcher: на объекте {targetName} должен быть компонент Graphic для получения кликов.",
                targetRect);
            return false;
        }

        if (!graphic.raycastTarget)
        {
            Debug.LogError(
                $"RadialSwitcher: у Graphic на объекте {targetName} должен быть включён Raycast Target.",
                graphic);
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

    private void ApplyStartPosition(bool instant)
    {
        int startIndex = ResolveStartPositionIndex();
        SetPositionByIndexInternal(startIndex, false, instant);
    }

    private int ResolveStartPositionIndex()
    {
        int idIndex = FindPositionIndexById(startPositionId);

        if (idIndex >= 0)
        {
            return idIndex;
        }

        if (positions == null || positions.Length == 0)
        {
            return -1;
        }

        return Mathf.Clamp(startPositionIndex, 0, positions.Length - 1);
    }

    private int FindPositionIndexById(string positionId)
    {
        if (string.IsNullOrWhiteSpace(positionId) || positions == null)
        {
            return -1;
        }

        for (int i = 0; i < positions.Length; i++)
        {
            if (string.Equals(positions[i].id, positionId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void StepPosition(int direction)
    {
        if (positions == null || positions.Length == 0)
        {
            return;
        }

        int targetIndex;

        if (currentIndex < 0)
        {
            targetIndex = ResolveStartPositionIndex();
        }
        else
        {
            targetIndex = (currentIndex + direction + positions.Length) % positions.Length;
        }

        SetPositionByIndexInternal(targetIndex, true, false);
    }

    private void TrySelectPositionFromPointer(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                clickAreaRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float angleFromUp = GetAngleFromUp(localPoint);
        int nearestIndex = FindNearestPositionIndex(angleFromUp);

        if (nearestIndex < 0)
        {
            return;
        }

        SetPositionByIndexInternal(nearestIndex, true, false);
    }

    private int FindNearestPositionIndex(float angleFromUp)
    {
        if (positions == null || positions.Length == 0)
        {
            return -1;
        }

        int nearestIndex = -1;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < positions.Length; i++)
        {
            float delta = Mathf.Abs(Mathf.DeltaAngle(angleFromUp, positions[i].angleFromUp));

            if (delta < nearestDistance)
            {
                nearestDistance = delta;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private float GetAngleFromUp(Vector2 localPoint)
    {
        if (localPoint.sqrMagnitude <= Mathf.Epsilon)
        {
            return IsValidIndex(currentIndex) ? positions[currentIndex].angleFromUp : 0f;
        }

        return Mathf.Atan2(localPoint.x, localPoint.y) * Mathf.Rad2Deg;
    }

    private void SetPositionByIndexInternal(int index, bool sendCallback, bool instant)
    {
        if (!TryEnsureValidSetup() || !IsValidIndex(index))
        {
            return;
        }

        bool hasChanged = currentIndex != index;
        currentIndex = index;

        RadialPosition position = positions[currentIndex];
        targetRotationZ = ConvertAngleFromUpToRotationZ(position.angleFromUp);

        if (instant || !smoothMovement)
        {
            currentRotationZ = targetRotationZ;
            SetPointerRotation(currentRotationZ);
        }
        else if (!hasInitialized)
        {
            SyncCurrentRotationFromVisual();
        }

        if (!hasChanged)
        {
            return;
        }

        PlaySwitchSound();

        if (logDebugInfo)
        {
            Debug.Log(
                $"RadialSwitcher: выбрано положение '{position.id}' с углом {position.angleFromUp:F1}.",
                this);
        }

        if (!sendCallback)
        {
            return;
        }

        OnPositionChanged?.Invoke(position.id);
        onPositionChanged?.Invoke(position.id);
    }

    private void PlaySwitchSound()
    {
        if (switchAudioClip == null)
        {
            return;
        }

        if (switchAudioSource == null)
        {
            switchAudioSource = GetComponent<AudioSource>();
        }

        if (switchAudioSource == null)
        {
            return;
        }

        switchAudioSource.PlayOneShot(switchAudioClip, switchAudioVolume);
    }

    private void SyncCurrentRotationFromVisual()
    {
        if (pointerRect == null)
        {
            return;
        }

        currentRotationZ = pointerRect.localEulerAngles.z;
        targetRotationZ = ConvertAngleFromUpToRotationZ(IsValidIndex(currentIndex) ? positions[currentIndex].angleFromUp : 0f);
    }

    private static float ConvertAngleFromUpToRotationZ(float angleFromUp)
    {
        return -angleFromUp;
    }

    private void SetPointerRotation(float rotationZ)
    {
        if (pointerRect == null)
        {
            return;
        }

        Vector3 localEulerAngles = pointerRect.localEulerAngles;
        localEulerAngles.z = rotationZ;
        pointerRect.localEulerAngles = localEulerAngles;
    }

    private bool IsValidIndex(int index)
    {
        return positions != null && index >= 0 && index < positions.Length;
    }

    private void LogDebug(string message)
    {
        if (logDebugInfo)
        {
            Debug.Log(message, this);
        }
    }
}

[DisallowMultipleComponent]
internal sealed class RadialSwitcherInputProxy : MonoBehaviour, IPointerDownHandler
{
    private RadialSwitcher owner;

    internal static RadialSwitcherInputProxy GetOrAdd(GameObject target)
    {
        RadialSwitcherInputProxy proxy = target.GetComponent<RadialSwitcherInputProxy>();

        if (proxy == null)
        {
            proxy = target.AddComponent<RadialSwitcherInputProxy>();
        }

        return proxy;
    }

    internal void SetOwner(RadialSwitcher radialSwitcher)
    {
        owner = radialSwitcher;
    }

    internal void ClearOwner(RadialSwitcher radialSwitcher)
    {
        if (owner == radialSwitcher)
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

        owner.HandlePointerDown(eventData);
    }
}
