using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HorizontalCompassView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Камера, направление взгляда которой отображает компас. Если не назначена, используется Camera.main.")]
    [SerializeField] private Camera sourceCamera;

    [Tooltip("RawImage с текстурой горизонтальной шкалы 0-360.")]
    [SerializeField] private RawImage compassScaleImage;

    [Header("Шкала")]
    [Tooltip("Сколько градусов шкалы видно по ширине элемента.")]
    [SerializeField, Min(1f)] private float visibleDegrees = 120f;

    [Tooltip("Поправка нуля шкалы в градусах. Используй, если север на текстуре смещен.")]
    [SerializeField] private float zeroOffsetDegrees;

    [Tooltip("Инвертировать направление прокрутки шкалы.")]
    [SerializeField] private bool invertDirection;

    [Tooltip("Включать повтор текстуры по X в рантайме для бесшовного перехода 0/360.")]
    [SerializeField] private bool forceTextureRepeat = true;

    private Texture cachedTexture;

    private void Awake()
    {
        ApplyTextureRepeatMode();
    }

    private void OnEnable()
    {
        ApplyTextureRepeatMode();
        UpdateCompass();
    }

    private void LateUpdate()
    {
        UpdateCompass();
    }

    private void OnValidate()
    {
        visibleDegrees = Mathf.Max(1f, visibleDegrees);
    }

    private void UpdateCompass()
    {
        if (sourceCamera == null || compassScaleImage == null)
        {
            Debug.LogError("HorizontalCompassView: назначь sourceCamera и compassScaleImage в инспекторе.", this);
            return;
        }

        ApplyTextureRepeatMode();

        float headingDegrees = GetCameraHeadingDegrees(sourceCamera.transform);
        float adjustedHeading = Mathf.Repeat(headingDegrees + zeroOffsetDegrees, 360f);

        if (invertDirection)
        {
            adjustedHeading = Mathf.Repeat(360f - adjustedHeading, 360f);
        }

        float visibleFraction = Mathf.Max(visibleDegrees, 1f) / 360f;
        float centerU = adjustedHeading / 360f;

        compassScaleImage.uvRect = new Rect(
            centerU - visibleFraction * 0.5f,
            0f,
            visibleFraction,
            1f
        );
    }

    private void ApplyTextureRepeatMode()
    {
        if (!forceTextureRepeat || compassScaleImage == null)
        {
            return;
        }

        Texture texture = compassScaleImage.texture;

        if (texture == null || texture == cachedTexture)
        {
            return;
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        cachedTexture = texture;
    }

    private static float GetCameraHeadingDegrees(Transform cameraTransform)
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);

        if (flatForward.sqrMagnitude <= 0.0001f)
        {
            return 0f;
        }

        flatForward.Normalize();

        float headingDegrees = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
        return Mathf.Repeat(headingDegrees, 360f);
    }
}
