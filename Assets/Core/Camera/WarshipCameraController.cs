using UnityEngine;

public class WarshipCameraController : MonoBehaviour
{
    [Header("Цель")]
    [SerializeField] private Transform ship;
    [SerializeField] private Transform lookTargetOverride;
    [Tooltip("Если включено, при повороте корабля камера сохраняет мировое направление орбиты и не поворачивается вместе с корпусом.")]
    [SerializeField] private bool keepGlobalLookDirectionOnShipTurn = false;

    [Header("Габариты корабля")]
    [Tooltip("Длина корабля в единицах Unity.")]
    [SerializeField] private float shipLength = 120f;

    [Tooltip("Ширина корабля в единицах Unity.")]
    [SerializeField] private float shipBeam = 20f;

    [Tooltip("Дополнительный отступ камеры от борта на ближнем зуме.")]
    [SerializeField] private float closeSideClearance = 8f;

    [Header("Зум")]
    [Tooltip("0 = максимально близко, 1 = максимально далеко.")]
    [Range(0f, 1f)]
    [SerializeField] private float zoom01 = 0.5f;

    [Tooltip("Скорость изменения зума колесом мыши.")]
    [SerializeField] private float zoomSpeed = 0.08f;

    [Tooltip("Горизонтальная дистанция камеры на дальнем зуме.")]
    [SerializeField] private float farOrbitDistance = 180f;

    [Header("Наклон")]
    [Tooltip("Вертикальный угол камеры в градусах. 0 = горизонт, положительные значения = взгляд сверху вниз.")]
    [SerializeField] private float pitch = 22f;
    [Tooltip("Минимально допустимый вертикальный угол камеры.")]
    [SerializeField] private float minPitch = 0f;
    [Tooltip("Максимально допустимый вертикальный угол камеры.")]
    [SerializeField] private float maxPitch = 55f;

    [Tooltip("Высота точки, в которую смотрит камера, на ближнем зуме.")]
    [SerializeField] private float closeLookHeight = 7f;
    [Tooltip("Высота точки, в которую смотрит камера, на дальнем зуме.")]
    [SerializeField] private float farLookHeight = 14f;

    [Header("Мышь")]
    [Tooltip("Кнопка мыши, которую нужно удерживать для вращения камеры.")]
    [SerializeField] private int rotateMouseButton = 1;
    [Tooltip("Если включено, камера вращается только при удержании кнопки мыши.")]
    [SerializeField] private bool rotateOnlyWhenButtonHeld = true;
    [Tooltip("Скорость поворота камеры по горизонтали.")]
    [SerializeField] private float yawSpeed = 4f;
    [Tooltip("Скорость изменения вертикального угла камеры.")]
    [SerializeField] private float pitchSpeed = 2.5f;
    [Tooltip("Инвертировать движение камеры по вертикали.")]
    [SerializeField] private bool invertY = false;

    [Header("Сглаживание")]
    [Tooltip("Время сглаживания позиции камеры.")]
    [SerializeField] private float positionSmoothTime = 0.08f;
    [Tooltip("Резкость догоняющего поворота камеры.")]
    [SerializeField] private float rotationSharpness = 18f;

    [Header("Коллизии")]
    [Tooltip("Проверять препятствия между точкой взгляда и камерой.")]
    [SerializeField] private bool useCollision = true;
    [Tooltip("Слои, которые учитываются при проверке препятствий.")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [Tooltip("Радиус проверки препятствий для камеры.")]
    [SerializeField] private float collisionRadius = 0.6f;
    [Tooltip("Небольшой отступ от препятствия после коррекции позиции камеры.")]
    [SerializeField] private float collisionOffset = 0.5f;

    [Header("Кривая смешивания")]
    [Tooltip("Форма перехода между ближней орбитой вдоль корпуса и дальним круговым вылетом.")]
    [SerializeField] private AnimationCurve closeToFarBlend =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float yaw;
    private Vector3 velocity;
    private bool initialized;

    private void LateUpdate()
    {
        if (ship == null)
            return;

        ReadInput();

        CalculateDesiredCamera(out Vector3 desiredPosition, out Quaternion desiredRotation);

        if (!initialized)
        {
            transform.SetPositionAndRotation(desiredPosition, desiredRotation);
            initialized = true;
            return;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            positionSmoothTime
        );

        float rotLerp = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotLerp);
    }

    private void ReadInput()
    {
        bool canRotate = !rotateOnlyWhenButtonHeld || Input.GetMouseButton(rotateMouseButton);

        if (canRotate)
        {
            float mouseX = Input.GetAxisRaw("Mouse X");
            float mouseY = Input.GetAxisRaw("Mouse Y");

            yaw += mouseX * yawSpeed;

            float ySign = invertY ? 1f : -1f;
            pitch += mouseY * pitchSpeed * ySign;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        float scroll = Input.mouseScrollDelta.y;
        zoom01 -= scroll * zoomSpeed;
        zoom01 = Mathf.Clamp01(zoom01);

        yaw = Mathf.Repeat(yaw, 360f);
    }

    private void CalculateDesiredCamera(out Vector3 desiredPosition, out Quaternion desiredRotation)
    {
        float yawRad = yaw * Mathf.Deg2Rad;
        float pitchRad = pitch * Mathf.Deg2Rad;

        // yaw = 0 удерживает камеру за кормой, если нос корабля направлен вдоль +Z.
        Vector2 dir2 = new Vector2(Mathf.Sin(yawRad), -Mathf.Cos(yawRad)).normalized;
        float blend = closeToFarBlend.Evaluate(zoom01);
        float lookHeight = Mathf.Lerp(closeLookHeight, farLookHeight, blend);

        Vector2 close2 = GetCapsuleOrbitPoint(dir2);
        Vector2 far2 = dir2 * farOrbitDistance;

        Quaternion shipYawOnly = Quaternion.Euler(0f, ship.eulerAngles.y, 0f);
        Vector3 lookPosition = lookTargetOverride != null
            ? lookTargetOverride.position
            : ship.position + new Vector3(0f, lookHeight, 0f);

        Vector3 closeHorizontalLocal = new Vector3(close2.x, 0f, close2.y);
        Vector3 farHorizontalLocal = new Vector3(far2.x, 0f, far2.y);
        Vector3 horizontalLocal = Vector3.Lerp(closeHorizontalLocal, farHorizontalLocal, blend);

        // Pitch теперь задает реальный угол взгляда относительно горизонта.
        float verticalOffset = horizontalLocal.magnitude * Mathf.Tan(pitchRad);
        Vector3 orbitLocal = horizontalLocal + Vector3.up * verticalOffset;

        Vector3 orbitOffset = keepGlobalLookDirectionOnShipTurn
            ? orbitLocal
            : shipYawOnly * orbitLocal;

        desiredPosition = lookPosition + orbitOffset;

        if (useCollision)
        {
            desiredPosition = ResolveCameraCollision(lookPosition, desiredPosition);
        }

        Vector3 lookDirection = lookPosition - desiredPosition;

        if (lookDirection.sqrMagnitude < 0.001f)
            lookDirection = ship.forward;

        desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private Vector2 GetCapsuleOrbitPoint(Vector2 direction)
    {
        direction.Normalize();

        float radius = shipBeam * 0.5f + closeSideClearance;
        float capCenterZ = Mathf.Max(0f, shipLength * 0.5f - shipBeam * 0.5f);

        if (Mathf.Abs(direction.x) > 0.0001f)
        {
            float tSide = radius / Mathf.Abs(direction.x);
            float z = direction.y * tSide;

            if (Mathf.Abs(z) <= capCenterZ)
            {
                return direction * tSide;
            }
        }

        float sign = direction.y >= 0f ? 1f : -1f;
        Vector2 circleCenter = new Vector2(0f, sign * capCenterZ);
        float dot = Vector2.Dot(direction, circleCenter);
        float c = circleCenter.sqrMagnitude - radius * radius;
        float discriminant = dot * dot - c;

        float t = dot + Mathf.Sqrt(Mathf.Max(0f, discriminant));
        return direction * t;
    }

    private Vector3 ResolveCameraCollision(Vector3 lookPosition, Vector3 desiredPosition)
    {
        Vector3 toCamera = desiredPosition - lookPosition;
        float distance = toCamera.magnitude;

        if (distance <= 0.01f)
            return desiredPosition;

        Vector3 direction = toCamera / distance;

        if (Physics.SphereCast(
                lookPosition,
                collisionRadius,
                direction,
                out RaycastHit hit,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            float correctedDistance = Mathf.Max(0.5f, hit.distance - collisionOffset);
            return lookPosition + direction * correctedDistance;
        }

        return desiredPosition;
    }

    public void SetShip(Transform newShip)
    {
        ship = newShip;
        initialized = false;
    }

    public void SetZoom(float value)
    {
        zoom01 = Mathf.Clamp01(value);
    }
}
