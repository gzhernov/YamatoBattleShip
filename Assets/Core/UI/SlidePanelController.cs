using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SlidePanelController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private RectTransform panelRect;

    [Header("Позиции")]
    [SerializeField] private Vector2 openedPosition;
    [SerializeField] private Vector2 closedPosition;

    [Header("Анимация")]
    [SerializeField, Min(0f)] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool ignoreClicksWhileAnimating = true;

    [Header("Состояние")]
    [SerializeField] private bool startOpened;

    private Coroutine animationCoroutine;
    private bool isOpen;
    private bool isAnimating;
    private bool hasAppliedInitialState;

    private void OnEnable()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        toggleButton.onClick.RemoveListener(TogglePanel);
        toggleButton.onClick.AddListener(TogglePanel);

        if (!hasAppliedInitialState)
        {
            SetPanelState(startOpened, true);
            hasAppliedInitialState = true;
        }
    }

    private void OnDisable()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(TogglePanel);
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        isAnimating = false;
    }

    public void TogglePanel()
    {
        if (isAnimating && ignoreClicksWhileAnimating)
        {
            return;
        }

        SetPanelState(!isOpen, false);
    }

    public void OpenPanel()
    {
        SetPanelState(true, false);
    }

    public void ClosePanel()
    {
        SetPanelState(false, false);
    }

    public void SetPanelState(bool opened, bool instant)
    {
        if (!ValidateSetup())
        {
            return;
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        Vector2 targetPosition = opened ? openedPosition : closedPosition;

        if (instant || animationDuration <= 0f)
        {
            ApplyPosition(targetPosition);
            isOpen = opened;
            isAnimating = false;
            return;
        }

        animationCoroutine = StartCoroutine(AnimatePanel(targetPosition, opened));
    }

    private IEnumerator AnimatePanel(Vector2 targetPosition, bool targetIsOpen)
    {
        isAnimating = true;

        Vector2 startPosition = panelRect.anchoredPosition;
        float elapsedTime = 0f;

        // Двигаем панель между двумя заранее заданными позициями.
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float normalizedTime = Mathf.Clamp01(elapsedTime / animationDuration);
            float curveValue = animationCurve != null
                ? animationCurve.Evaluate(normalizedTime)
                : normalizedTime;

            ApplyPosition(Vector2.LerpUnclamped(startPosition, targetPosition, curveValue));
            yield return null;
        }

        ApplyPosition(targetPosition);
        isOpen = targetIsOpen;
        isAnimating = false;
        animationCoroutine = null;
    }

    private void ApplyPosition(Vector2 anchoredPosition)
    {
        panelRect.anchoredPosition = anchoredPosition;
    }

    private bool ValidateSetup()
    {
        if (toggleButton == null)
        {
            Debug.LogError("SlidePanelController: Кнопка переключения не назначена.", this);
            return false;
        }

        if (panelRect == null)
        {
            Debug.LogError("SlidePanelController: RectTransform панели не назначен.", this);
            return false;
        }

        return true;
    }
}
