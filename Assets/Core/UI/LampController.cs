using System;
using UnityEngine;
using UnityEngine.UI;

public enum LampState
{
    Off,
    On,
    Blinked
}

[DisallowMultipleComponent]
public class LampController : MonoBehaviour
{
    [Header("State")]
    [Tooltip("Состояние лампы, которое применяется при включении объекта.")]
    [SerializeField] private LampState initialState = LampState.Off;

    [Header("References")]
    [Tooltip("UI-изображения лампы, которые контроллер включает и выключает.")]
    [SerializeField] private Image[] lampImages = Array.Empty<Image>();

    [Header("Blink")]
    [Tooltip("Количество полных миганий в секунду. Значение 1 означает цикл включено/выключено за одну секунду.")]
    [SerializeField, Min(0f)] private float blinksPerSecond = 1f;

    [Header("Debug")]
    [Tooltip("Включает отладочные сообщения о смене состояния лампы.")]
    [SerializeField] private bool showDebugInfo = false;

    private LampState currentState;
    private float blinkTimer;
    private bool blinkVisible;

    public LampState CurrentState => currentState;

    private void OnEnable()
    {
        SetState(initialState);
    }

    private void OnValidate()
    {
        blinksPerSecond = Mathf.Max(0f, blinksPerSecond);

        if (Application.isPlaying && isActiveAndEnabled)
        {
            SetState(initialState);
        }
    }

    private void OnDisable()
    {
        blinkTimer = 0f;
        blinkVisible = false;
        ApplyVisibility(false);
    }

    private void Update()
    {
        if (currentState != LampState.Blinked)
        {
            return;
        }

        if (blinksPerSecond <= 0f)
        {
            ApplyVisibility(false);
            return;
        }

        blinkTimer += Time.deltaTime;
        float phaseDuration = 0.5f / blinksPerSecond;

        while (blinkTimer >= phaseDuration)
        {
            blinkTimer -= phaseDuration;
            blinkVisible = !blinkVisible;
            ApplyVisibility(blinkVisible);
        }
    }

    public void SetState(LampState state)
    {
        currentState = state;
        blinkTimer = 0f;

        switch (currentState)
        {
            case LampState.Off:
                blinkVisible = false;
                ApplyVisibility(false);
                break;
            case LampState.On:
                blinkVisible = true;
                ApplyVisibility(true);
                break;
            case LampState.Blinked:
                blinkVisible = blinksPerSecond > 0f;
                ApplyVisibility(blinkVisible);
                break;
            default:
                blinkVisible = false;
                ApplyVisibility(false);
                break;
        }

        if (showDebugInfo)
        {
            Debug.Log($"LampController: применено состояние '{currentState}'.", this);
        }
    }

    public void SetOn()
    {
        SetState(LampState.On);
    }

    public void SetOff()
    {
        SetState(LampState.Off);
    }

    public void SetBlinked()
    {
        SetState(LampState.Blinked);
    }

    public void SetBlinksPerSecond(float value)
    {
        blinksPerSecond = Mathf.Max(0f, value);

        if (currentState == LampState.Blinked)
        {
            SetState(LampState.Blinked);
        }
    }

    private void ApplyVisibility(bool visible)
    {
        if (lampImages == null)
        {
            return;
        }

        for (int i = 0; i < lampImages.Length; i++)
        {
            Image lampImage = lampImages[i];

            if (lampImage == null)
            {
                continue;
            }

            lampImage.enabled = visible;
        }
    }
}
