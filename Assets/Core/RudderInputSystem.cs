using System;
using UnityEngine;

public class RudderInputSystem : MonoBehaviour
{
    [Header("Rudder Settings")]
    [SerializeField] private int defaultPosition = 2;
    [SerializeField] private int totalPositions = 5;

    [Header("Keyboard")]
    [SerializeField] private KeyCode leftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode rightKey = KeyCode.RightArrow;

    [Header("Debug")]
    [SerializeField] private bool logChanges = true;

    private int currentPosition;

    public int CurrentPosition => currentPosition;
    public int TotalPositions => totalPositions;

    public event Action<int> OnRudderPositionChanged;

    private void Awake()
    {
        totalPositions = Mathf.Max(2, totalPositions);
        currentPosition = Mathf.Clamp(defaultPosition, 0, totalPositions - 1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(leftKey))
        {
            MoveLeft();
        }

        if (Input.GetKeyDown(rightKey))
        {
            MoveRight();
        }
    }

    public void MoveLeft()
    {
        SetPosition(currentPosition - 1);
    }

    public void MoveRight()
    {
        SetPosition(currentPosition + 1);
    }

    public void SetPosition(int newPosition)
    {
        int clampedPosition = Mathf.Clamp(newPosition, 0, totalPositions - 1);

        if (currentPosition == clampedPosition)
            return;

        currentPosition = clampedPosition;

        if (logChanges)
        {
            Debug.Log($"Руль изменил положение: {currentPosition}");
        }

        OnRudderPositionChanged?.Invoke(currentPosition);
    }

    public void ResetToCenter()
    {
        int center = Mathf.FloorToInt((totalPositions - 1) / 2f);
        SetPosition(center);
    }

    public float GetNormalizedValue()
    {
        return (float)currentPosition / (totalPositions - 1);
    }

    public float GetSignedValue()
    {
        if (totalPositions <= 1)
            return 0f;

        return Mathf.Lerp(-1f, 1f, GetNormalizedValue());
    }
}