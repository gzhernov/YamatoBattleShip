using UnityEngine;

[DisallowMultipleComponent]
public class RudderView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;
    [SerializeField] private RectTransform trackRect;
    [SerializeField] private RectTransform indicatorRect;

    [Header("Behavior")]
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private bool smoothMovement = true;
    [SerializeField, Min(0f)] private float smoothSpeed = 15f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private float currentAnchoredX;
    private float targetAnchoredX;
    private bool hasResolvedInitialPosition;

    private void OnEnable()
    {
        hasResolvedInitialPosition = false;
        ResolveReferences();

        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        shipStatuses.OnTargetRudderValueChanged += OnTargetRudderValueChanged;

        if (refreshOnEnable)
        {
            ApplyCurrentStatus(true);
        }
        else
        {
            CacheCurrentIndicatorPosition();
        }
    }

    private void OnDisable()
    {
        if (shipStatuses != null)
        {
            shipStatuses.OnTargetRudderValueChanged -= OnTargetRudderValueChanged;
        }
    }

    private void Update()
    {
        if (!smoothMovement || indicatorRect == null)
        {
            return;
        }

        if (!hasResolvedInitialPosition)
        {
            CacheCurrentIndicatorPosition();
        }

        currentAnchoredX = Mathf.Lerp(
            currentAnchoredX,
            targetAnchoredX,
            Time.deltaTime * smoothSpeed
        );

        SetIndicatorAnchoredX(currentAnchoredX);
    }

    private void OnValidate()
    {
        if (trackRect == null)
        {
            trackRect = GetComponent<RectTransform>();
        }
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = FindFirstObjectByType<ShipStatuses>();
        }

        if (trackRect == null)
        {
            trackRect = GetComponent<RectTransform>();
        }
    }

    private bool ValidateSetup()
    {
        if (shipStatuses == null)
        {
            Debug.LogError("RudderView: ShipStatuses not found.", this);
            return false;
        }

        if (trackRect == null)
        {
            Debug.LogError("RudderView: Track RectTransform is not assigned.", this);
            return false;
        }

        if (indicatorRect == null)
        {
            Debug.LogError("RudderView: Indicator RectTransform is not assigned.", this);
            return false;
        }

        if (!(indicatorRect.parent is RectTransform))
        {
            Debug.LogError("RudderView: Indicator parent must be a RectTransform.", indicatorRect);
            return false;
        }

        return true;
    }

    private void ApplyCurrentStatus(bool instant)
    {
        UpdateView(shipStatuses.TargetRudderSignedValue, instant);
    }

    private void OnTargetRudderValueChanged(float signedValue)
    {
        UpdateView(signedValue, false);
    }

    private void UpdateView(float signedValue, bool instant)
    {
        targetAnchoredX = GetAnchoredXFromSignedValue(signedValue);

        if (instant || !smoothMovement)
        {
            currentAnchoredX = targetAnchoredX;
            SetIndicatorAnchoredX(currentAnchoredX);
        }
        else if (!hasResolvedInitialPosition)
        {
            CacheCurrentIndicatorPosition();
        }

        if (showDebugInfo)
        {
            Debug.Log(
                $"RudderView: rudder command {signedValue:F2}, target anchored X {targetAnchoredX:F1}.",
                this
            );
        }
    }

    private float GetAnchoredXFromSignedValue(float signedValue)
    {
        float normalizedValue = Mathf.InverseLerp(-1f, 1f, Mathf.Clamp(signedValue, -1f, 1f));
        RectTransform indicatorParentRect = (RectTransform)indicatorRect.parent;
        Rect trackWorldRect = GetWorldRect(trackRect);

        Vector3 leftWorldPoint = new Vector3(trackWorldRect.xMin, trackWorldRect.center.y, 0f);
        Vector3 rightWorldPoint = new Vector3(trackWorldRect.xMax, trackWorldRect.center.y, 0f);

        Vector3 leftParentLocalPoint = indicatorParentRect.InverseTransformPoint(leftWorldPoint);
        Vector3 rightParentLocalPoint = indicatorParentRect.InverseTransformPoint(rightWorldPoint);

        return Mathf.Lerp(leftParentLocalPoint.x, rightParentLocalPoint.x, normalizedValue);
    }

    private void CacheCurrentIndicatorPosition()
    {
        if (indicatorRect == null)
        {
            return;
        }

        currentAnchoredX = indicatorRect.anchoredPosition.x;
        targetAnchoredX = currentAnchoredX;
        hasResolvedInitialPosition = true;
    }

    private void SetIndicatorAnchoredX(float anchoredX)
    {
        if (indicatorRect == null)
        {
            return;
        }

        Vector2 anchoredPosition = indicatorRect.anchoredPosition;
        anchoredPosition.x = anchoredX;
        indicatorRect.anchoredPosition = anchoredPosition;
        hasResolvedInitialPosition = true;
    }

    private static Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        float minX = corners[0].x;
        float minY = corners[0].y;
        float maxX = corners[2].x;
        float maxY = corners[2].y;

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }
}
