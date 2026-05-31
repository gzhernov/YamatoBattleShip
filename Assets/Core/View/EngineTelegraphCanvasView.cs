using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EngineTelegraphCanvasView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;
    [SerializeField] private RectTransform markersRoot;

    [Header("Sprites")]
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite unselectedSprite;

    [Header("Behavior")]
    [SerializeField] private bool refreshOnEnable = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private readonly Dictionary<EngineTelegraphSector, Image> markerImages = new();

    private void OnEnable()
    {
        ResolveReferences();

        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        CacheMarkerImages();
        shipStatuses.OnEngineTelegraphChanged += OnEngineTelegraphChanged;

        if (refreshOnEnable)
        {
            ApplyCurrentSector();
        }
    }

    private void OnDisable()
    {
        if (shipStatuses != null)
        {
            shipStatuses.OnEngineTelegraphChanged -= OnEngineTelegraphChanged;
        }
    }

    private void OnValidate()
    {
        if (markersRoot == null)
        {
            markersRoot = GetComponent<RectTransform>();
        }
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = FindFirstObjectByType<ShipStatuses>();
        }

        if (markersRoot == null)
        {
            markersRoot = GetComponent<RectTransform>();
        }
    }

    private bool ValidateSetup()
    {
        if (shipStatuses == null)
        {
            Debug.LogError("EngineTelegraphCanvasView: ShipStatuses not found.", this);
            return false;
        }

        if (markersRoot == null)
        {
            Debug.LogError("EngineTelegraphCanvasView: Markers root is not assigned.", this);
            return false;
        }

        if (selectedSprite == null)
        {
            Debug.LogError("EngineTelegraphCanvasView: Selected sprite is not assigned.", this);
            return false;
        }

        if (unselectedSprite == null)
        {
            Debug.LogError("EngineTelegraphCanvasView: Unselected sprite is not assigned.", this);
            return false;
        }

        return true;
    }

    private void CacheMarkerImages()
    {
        markerImages.Clear();

        Image[] images = markersRoot.GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image == null)
            {
                continue;
            }

            if (!Enum.TryParse(image.gameObject.name, true, out EngineTelegraphSector sector))
            {
                continue;
            }

            if (markerImages.ContainsKey(sector))
            {
                Debug.LogWarning(
                    $"EngineTelegraphCanvasView: Duplicate marker for sector '{sector}' on '{image.gameObject.name}'.",
                    image
                );
                continue;
            }

            markerImages.Add(sector, image);
        }

        if (showDebugInfo)
        {
            Debug.Log(
                $"EngineTelegraphCanvasView: Cached {markerImages.Count} marker images.",
                this
            );
        }
    }

    private void OnEngineTelegraphChanged(
        EngineTelegraphSector sector,
        EngineTelegraphSectorData sectorData
    )
    {
        ApplySector(sector);
    }

    private void ApplyCurrentSector()
    {
        ApplySector(shipStatuses.CurrentEngineTelegraphSector);
    }

    private void ApplySector(EngineTelegraphSector currentSector)
    {
        foreach (KeyValuePair<EngineTelegraphSector, Image> markerEntry in markerImages)
        {
            Image markerImage = markerEntry.Value;

            if (markerImage == null)
            {
                continue;
            }

            markerImage.sprite = markerEntry.Key == currentSector
                ? selectedSprite
                : unselectedSprite;
        }

        if (showDebugInfo)
        {
            Debug.Log(
                $"EngineTelegraphCanvasView: Applied sector '{currentSector}'.",
                this
            );
        }
    }
}
