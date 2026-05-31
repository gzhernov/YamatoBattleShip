using UnityEngine;

public class WarshipCameraController1 : MonoBehaviour
{
    private struct CapsuleOrbitSample
    {
        public Vector2 Position;
        public Vector2 LookDirection;
    }

    [Header("Target")]
    [SerializeField] private Transform ship;
    [SerializeField] private Transform lookTargetOverride;
    [Tooltip("Если включено, при повороте корабля камера сохраняет мировой азимут вместо вращения вместе с корпусом.")]
    [SerializeField] private bool keepGlobalLookDirectionOnShipTurn = false;

    [Header("Ship Shape")]
    [Tooltip("Длина корабля в юнитах Unity.")]
    [SerializeField] private float shipLength = 120f;

    [Tooltip("Ширина корабля в юнитах Unity.")]
    [SerializeField] private float shipBeam = 20f;

    [Tooltip("Отступ камеры от борта на ближнем зуме.")]
    [SerializeField] private float closeSideClearance = 8f;

    [Header("Zoom")]
    [Tooltip("0 = максимально близко, 1 = максимально далеко.")]
    [Range(0f, 1f)]
    [SerializeField] private float zoom01 = 0.5f;

    [SerializeField] private float zoomSpeed = 0.08f;

    [Tooltip("Горизонтальная дистанция камеры на дальнем зуме.")]
    [SerializeField] private float farOrbitDistance = 180f;

    [Header("Height / Pitch")]
    [SerializeField] private float pitch = 22f;
    [SerializeField] private float minPitch = 5f;
    [SerializeField] private float maxPitch = 55f;

    [SerializeField] private float closeMinHeight = 7f;
    [SerializeField] private float closeMaxHeight = 24f;

    [SerializeField] private float farMinHeight = 28f;
    [SerializeField] private float farMaxHeight = 95f;

    [SerializeField] private float closeLookHeight = 7f;
    [SerializeField] private float farLookHeight = 14f;

    [Header("Mouse")]
    [SerializeField] private int rotateMouseButton = 1; // 1 = правая кнопка мыши
    [SerializeField] private bool rotateOnlyWhenButtonHeld = true;
    [SerializeField] private float yawSpeed = 4f;
    [SerializeField] private float pitchSpeed = 2.5f;
    [SerializeField] private bool invertY = false;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.08f;
    [SerializeField] private float rotationSharpness = 18f;

    [Header("Collision")]
    [SerializeField] private bool useCollision = true;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.6f;
    [SerializeField] private float collisionOffset = 0.5f;

    [Header("Blend Curve")]
    [Tooltip("Форма перехода: 0 = ближняя капсула, 1 = дальний круг.")]
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

        // Колесо вверх обычно значит приблизить камеру.
        zoom01 -= scroll * zoomSpeed;
        zoom01 = Mathf.Clamp01(zoom01);

        yaw = Mathf.Repeat(yaw, 360f);
    }

    private void CalculateDesiredCamera(out Vector3 desiredPosition, out Quaternion desiredRotation)
    {
        float yawRad = yaw * Mathf.Deg2Rad;

        // Локальная горизонтальная сторона, где находится камера.
        // yaw = 0 -> камера за кораблём, если нос корабля смотрит в +Z.
        Vector2 dir2 = new Vector2(Mathf.Sin(yawRad), -Mathf.Cos(yawRad)).normalized;

        float pitch01 = Mathf.InverseLerp(minPitch, maxPitch, pitch);
        float blend = closeToFarBlend.Evaluate(zoom01);

        CapsuleOrbitSample closeSample = GetCapsuleOrbitSample(yaw);
        Vector2 close2 = closeSample.Position;
        Vector2 far2 = dir2 * farOrbitDistance;

        float closeHeight = Mathf.Lerp(closeMinHeight, closeMaxHeight, pitch01);
        float farHeight = Mathf.Lerp(farMinHeight, farMaxHeight, pitch01);

        Vector3 closeLocal = new Vector3(close2.x, closeHeight, close2.y);
        Vector3 farLocal = new Vector3(far2.x, farHeight, far2.y);
        Vector3 finalLocal = Vector3.Lerp(closeLocal, farLocal, blend);

        // Игнорируем крен/дифферент корабля, чтобы камера не заваливалась вместе с волнами.
        Quaternion shipYawOnly = Quaternion.Euler(0f, ship.eulerAngles.y, 0f);

        Vector3 orbitOffset = keepGlobalLookDirectionOnShipTurn
            ? finalLocal
            : shipYawOnly * finalLocal;

        desiredPosition = ship.position + orbitOffset;

        float lookHeight = Mathf.Lerp(closeLookHeight, farLookHeight, blend);

        if (useCollision)
        {
            Vector3 collisionLookPosition = lookTargetOverride != null
                ? lookTargetOverride.position
                : ship.position + (keepGlobalLookDirectionOnShipTurn
                    ? new Vector3(0f, lookHeight, 0f)
                    : shipYawOnly * new Vector3(0f, lookHeight, 0f));

            desiredPosition = ResolveCameraCollision(collisionLookPosition, desiredPosition);
        }

        Vector3 lookDirection = lookTargetOverride != null
            ? lookTargetOverride.position - desiredPosition
            : GetImplicitLookDirection(desiredPosition, closeSample, shipYawOnly, lookHeight, blend);

        if (lookDirection.sqrMagnitude < 0.001f)
            lookDirection = ship.forward;

        desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private Vector3 GetImplicitLookDirection(
        Vector3 cameraPosition,
        CapsuleOrbitSample closeOrbitSample,
        Quaternion shipYawOnly,
        float lookHeight,
        float blend)
    {
        Vector3 shipLookPosition = ship.position + (keepGlobalLookDirectionOnShipTurn
            ? new Vector3(0f, lookHeight, 0f)
            : shipYawOnly * new Vector3(0f, lookHeight, 0f));

        Vector3 centerLookDirection = shipLookPosition - cameraPosition;
        Vector3 centerHorizontal = Vector3.ProjectOnPlane(centerLookDirection, Vector3.up);

        Vector3 closeHorizontal = GetCloseHorizontalLookDirection(closeOrbitSample, shipYawOnly);
        float closeWeight = 1f - blend;

        Vector3 blendedHorizontal = centerHorizontal.sqrMagnitude > 0.001f
            ? Vector3.Slerp(centerHorizontal.normalized, closeHorizontal, closeWeight)
            : closeHorizontal;

        float horizontalDistance = Mathf.Max(0.001f, centerHorizontal.magnitude);
        float verticalOffset = shipLookPosition.y - cameraPosition.y;

        return blendedHorizontal.normalized * horizontalDistance + Vector3.up * verticalOffset;
    }

    private Vector3 GetCloseHorizontalLookDirection(CapsuleOrbitSample closeOrbitSample, Quaternion shipYawOnly)
    {
        Vector2 localHorizontal = closeOrbitSample.LookDirection;
        Vector3 localDirection = new Vector3(localHorizontal.x, 0f, localHorizontal.y);
        return keepGlobalLookDirectionOnShipTurn
            ? localDirection.normalized
            : (shipYawOnly * localDirection).normalized;
    }

    private CapsuleOrbitSample GetCapsuleOrbitSample(float yawDegrees)
    {
        float radius = shipBeam * 0.5f + closeSideClearance;
        float capCenterZ = Mathf.Max(0f, shipLength * 0.5f - shipBeam * 0.5f);

        float quadrantAngle = Mathf.Repeat(yawDegrees, 360f);
        int quadrantIndex = Mathf.FloorToInt(quadrantAngle / 90f);
        float quadrantT = (quadrantAngle - quadrantIndex * 90f) / 90f;

        float sideHalfLength = capCenterZ;
        float arcQuarterLength = radius * Mathf.PI * 0.5f;
        float quadrantLength = sideHalfLength + arcQuarterLength;
        float distance = quadrantT * quadrantLength;

        switch (quadrantIndex)
        {
            case 0:
                return SampleSternToStarboard(distance, arcQuarterLength, sideHalfLength, radius, capCenterZ);
            case 1:
                return SampleStarboardToBow(distance, arcQuarterLength, sideHalfLength, radius, capCenterZ);
            case 2:
                return SampleBowToPort(distance, arcQuarterLength, sideHalfLength, radius, capCenterZ);
            default:
                return SamplePortToStern(distance, arcQuarterLength, sideHalfLength, radius, capCenterZ);
        }
    }

    private CapsuleOrbitSample SampleSternToStarboard(
        float distance,
        float arcQuarterLength,
        float sideHalfLength,
        float radius,
        float capCenterZ)
    {
        if (distance <= arcQuarterLength)
        {
            float angle = -Mathf.PI * 0.5f + distance / radius;
            return CreateArcSample(new Vector2(0f, -capCenterZ), angle, radius);
        }

        float sideDistance = Mathf.Min(sideHalfLength, distance - arcQuarterLength);
        return CreateSideSample(radius, -capCenterZ + sideDistance, new Vector2(-1f, 0f));
    }

    private CapsuleOrbitSample SampleStarboardToBow(
        float distance,
        float arcQuarterLength,
        float sideHalfLength,
        float radius,
        float capCenterZ)
    {
        if (distance <= sideHalfLength)
        {
            return CreateSideSample(radius, distance, new Vector2(-1f, 0f));
        }

        float arcDistance = Mathf.Min(arcQuarterLength, distance - sideHalfLength);
        float angle = arcDistance / radius;
        return CreateArcSample(new Vector2(0f, capCenterZ), angle, radius);
    }

    private CapsuleOrbitSample SampleBowToPort(
        float distance,
        float arcQuarterLength,
        float sideHalfLength,
        float radius,
        float capCenterZ)
    {
        if (distance <= arcQuarterLength)
        {
            float angle = Mathf.PI * 0.5f + distance / radius;
            return CreateArcSample(new Vector2(0f, capCenterZ), angle, radius);
        }

        float sideDistance = Mathf.Min(sideHalfLength, distance - arcQuarterLength);
        return CreateSideSample(-radius, capCenterZ - sideDistance, new Vector2(1f, 0f));
    }

    private CapsuleOrbitSample SamplePortToStern(
        float distance,
        float arcQuarterLength,
        float sideHalfLength,
        float radius,
        float capCenterZ)
    {
        if (distance <= sideHalfLength)
        {
            return CreateSideSample(-radius, -distance, new Vector2(1f, 0f));
        }

        float arcDistance = Mathf.Min(arcQuarterLength, distance - sideHalfLength);
        float angle = Mathf.PI + arcDistance / radius;
        return CreateArcSample(new Vector2(0f, -capCenterZ), angle, radius);
    }

    private CapsuleOrbitSample CreateSideSample(float x, float z, Vector2 lookDirection)
    {
        return new CapsuleOrbitSample
        {
            Position = new Vector2(x, z),
            LookDirection = lookDirection
        };
    }

    private CapsuleOrbitSample CreateArcSample(Vector2 center, float angle, float radius)
    {
        Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        return new CapsuleOrbitSample
        {
            Position = center + radial * radius,
            LookDirection = (-radial).normalized
        };
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
