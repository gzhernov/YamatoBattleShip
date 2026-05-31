using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EngineTelegraphRadialCanvasView : MonoBehaviour
{
    [Serializable]
    private struct SectorAngleMapping
    {
        public EngineTelegraphSector sector;
        [Tooltip("Абсолютный угол телеграфа: 0 = вверх, положительные значения = вправо, отрицательные = влево.")]
        public float angle;
    }

    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;
    [SerializeField] private RectTransform needleRect;

    [Header("Sector Angles")]
    [SerializeField] private SectorAngleMapping[] sectorAngles = Array.Empty<SectorAngleMapping>();

    [Header("Behavior")]
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private bool smoothMovement = true;
    [SerializeField, Min(0f)] private float smoothSpeed = 15f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private float currentCanvasRotationZ;
    private float targetCanvasRotationZ;

    private void OnEnable()
    {
        ResolveReferences();

        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        shipStatuses.OnEngineTelegraphChanged += OnEngineTelegraphChanged;

        if (refreshOnEnable)
        {
            ApplySector(shipStatuses.CurrentEngineTelegraphSector, true);
        }
    }

    private void OnDisable()
    {
        if (shipStatuses != null)
        {
            shipStatuses.OnEngineTelegraphChanged -= OnEngineTelegraphChanged;
        }
    }

    private void Update()
    {
        if (!smoothMovement || needleRect == null)
        {
            return;
        }

        currentCanvasRotationZ = Mathf.LerpAngle(
            currentCanvasRotationZ,
            targetCanvasRotationZ,
            Time.deltaTime * smoothSpeed
        );
        SetNeedleCanvasRotation(currentCanvasRotationZ);
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
        if (shipStatuses == null)
        {
            Debug.LogError("EngineTelegraphRadialCanvasView: ShipStatuses not found.", this);
            return false;
        }

        if (needleRect == null)
        {
            Debug.LogError("EngineTelegraphRadialCanvasView: Needle RectTransform is not assigned.", this);
            return false;
        }

        return true;
    }

    private void OnEngineTelegraphChanged(
        EngineTelegraphSector sector,
        EngineTelegraphSectorData sectorData
    )
    {
        ApplySector(sector, false);
    }

    private void ApplySector(EngineTelegraphSector sector, bool instant)
    {
        if (!TryGetAngle(sector, out float telegraphAngle))
        {
            Debug.LogWarning(
                $"EngineTelegraphRadialCanvasView: No angle mapping configured for sector '{sector}'.",
                this
            );
            return;
        }

        targetCanvasRotationZ = ConvertTelegraphAngleToCanvasRotation(telegraphAngle);

        if (instant || !smoothMovement)
        {
            currentCanvasRotationZ = targetCanvasRotationZ;
            SetNeedleCanvasRotation(currentCanvasRotationZ);
        }

        if (showDebugInfo)
        {
            Debug.Log(
                $"EngineTelegraphRadialCanvasView: Applied sector '{sector}' with telegraph angle {telegraphAngle:F1} and canvas rotation {targetCanvasRotationZ:F1}.",
                this
            );
        }
    }

    private bool TryGetAngle(EngineTelegraphSector sector, out float angle)
    {
        if (sectorAngles != null)
        {
            for (int i = 0; i < sectorAngles.Length; i++)
            {
                if (sectorAngles[i].sector != sector)
                {
                    continue;
                }

                angle = sectorAngles[i].angle;
                return true;
            }
        }

        angle = 0f;
        return false;
    }

    private float ConvertTelegraphAngleToCanvasRotation(float telegraphAngle)
    {
        return -telegraphAngle;
    }

    private void SetNeedleCanvasRotation(float canvasRotationZ)
    {
        if (needleRect == null)
        {
            return;
        }

        Vector3 localEulerAngles = needleRect.localEulerAngles;
        localEulerAngles.z = canvasRotationZ;
        needleRect.localEulerAngles = localEulerAngles;
    }
}
