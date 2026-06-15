using UnityEngine;

namespace World
{
[DefaultExecutionOrder(-500)]
public class WorldAgent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldManager worldManager;
    
    [Header("World State")]
    [SerializeField] private WorldTransform worldTransform = new WorldTransform();
    [SerializeField] private bool initAgentTransform;
    [SerializeField] private bool isPlayer;

    [Header("Visual")]
    [SerializeField] private GameObject visualSource;
    [SerializeField, HideInInspector] private GameObject visualPrefab;
    [SerializeField, HideInInspector] private Transform sceneVisualRoot;
    [SerializeField, ReadOnlyInspector] private GameObject visualObject;

    [Header("Gizmo")]
    [SerializeField] private bool showAgentGizmo = true;
    [SerializeField] private bool drawAgentGizmoOnlyWhenSelected = false;
    [Tooltip("Иконка для Scene gizmo. Должна лежать в папке Assets/Gizmos, иначе Unity не сможет отрисовать её через DrawIcon.")]
    [SerializeField] private Texture2D gizmoIcon;

    
    private bool ownsVisualObject;
    private bool registeredInWorldManager;

    public WorldTransform WorldTransform => worldTransform;
    public bool IsPlayer => isPlayer;
    public GameObject VisualSource => visualSource;
    public GameObject VisualObject => visualObject;
    public WorldManager WorldManager => worldManager;

    private void Reset()
    {
        EnsureWorldTransformExists();
        MigrateLegacyVisualSourceIfNeeded();
    }

    private void Awake()
    {
        EnsureWorldTransformExists();
        MigrateLegacyVisualSourceIfNeeded();
        InitializeWorldTransformFromCurrentTransformIfNeeded();
        ResolveWorldManager();
    }

    private void OnEnable()
    {
        EnsureWorldTransformExists();
        ResolveWorldManager();
        RegisterInWorldManager();
        ResetRootObjectPositionToZero();
        RefreshVisualObjectLifecycle();
        SyncVisualObject();
    }

    private void Update()
    {
        RefreshVisualObjectLifecycle();
        SyncVisualObject();
    }

    private void OnDisable()
    {
        UnregisterFromWorldManager();
        DestroyVisualObject();
    }

    private void OnDestroy()
    {
        UnregisterFromWorldManager();

        if (ownsVisualObject && visualObject != null)
        {
            Destroy(visualObject);
            visualObject = null;
            ownsVisualObject = false;
        }
    }

    private void OnValidate()
    {
        EnsureWorldTransformExists();
        MigrateLegacyVisualSourceIfNeeded();

        if (HasValidSceneVisualSource())
        {
            ownsVisualObject = false;
            visualObject = visualSource;
            return;
        }

        if (!Application.isPlaying && !ownsVisualObject)
        {
            visualObject = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showAgentGizmo || drawAgentGizmoOnlyWhenSelected)
            return;
        
        DrawAgentGizmoIcon();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showAgentGizmo || !drawAgentGizmoOnlyWhenSelected)
            return;

        DrawAgentGizmoIcon();
    }

    public void CreateVisualObjectIfNeeded()
    {
        if (visualObject != null)
        {
            if (!ownsVisualObject && ShouldToggleSceneVisualActiveState() && !visualObject.activeSelf)
            {
                visualObject.SetActive(true);
            }

            return;
        }

        if (HasValidSceneVisualSource())
        {
            if (visualSource == null)
                return;

            visualObject = visualSource;

            if (ShouldToggleSceneVisualActiveState() && !visualObject.activeSelf)
            {
                visualObject.SetActive(true);
            }

            ownsVisualObject = false;
            return;
        }

        if (visualSource == null)
            return;

        GameObject createdVisualObject = Instantiate(visualSource, transform);
        createdVisualObject.name = $"{visualSource.name}_Visual";
        visualObject = createdVisualObject;
        ownsVisualObject = true;
    }

    public void DestroyVisualObject()
    {
        if (visualObject == null)
            return;

        if (ownsVisualObject)
        {
            Destroy(visualObject);
            visualObject = null;
            ownsVisualObject = false;
            return;
        }

        if (ShouldToggleSceneVisualActiveState() && visualObject.activeSelf)
        {
            visualObject.SetActive(false);
        }
        ownsVisualObject = false;
    }

    public void SetWorldPosition(WorldVector3D position)
    {
        worldTransform.SetPosition(position);
        SyncVisualObject();
    }

    public void SetWorldRotation(Quaternion rotation)
    {
        worldTransform.Rotation = rotation;
        SyncVisualObject();
    }

    public void SetWorldScale(Vector3 scale)
    {
        worldTransform.Scale = scale;
        SyncVisualObject();
    }

    private void EnsureWorldTransformExists()
    {
        if (worldTransform == null)
        {
            worldTransform = new WorldTransform();
        }
    }

    private void InitializeWorldTransformFromCurrentTransformIfNeeded()
    {
        if (!Application.isPlaying || !initAgentTransform || worldTransform == null)
            return;

        worldTransform.SetFromUnityTransform(transform);
    }

    private void ResetRootObjectPositionToZero()
    {
        transform.localPosition = Vector3.zero;
    }

    private void ResolveWorldManager()
    {
        if (worldManager != null)
            return;

        if (WorldManager.Instance != null)
        {
            worldManager = WorldManager.Instance;
            return;
        }

        worldManager = FindFirstObjectByType<WorldManager>();
    }

    private void RegisterInWorldManager()
    {
        if (registeredInWorldManager || worldManager == null)
            return;

        registeredInWorldManager = worldManager.RegisterAgent(this);
    }

    private void UnregisterFromWorldManager()
    {
        if (!registeredInWorldManager || worldManager == null)
            return;

        worldManager.UnregisterAgent(this);
        registeredInWorldManager = false;
    }

    private void SyncVisualObject()
    {
        if (visualObject == null || worldTransform == null || worldManager == null)
            return;

        Vector3 localScenePosition = worldManager.WorldToLocalPosition(worldTransform);
        Transform visualTransform = visualObject.transform;

        visualTransform.SetPositionAndRotation(localScenePosition, worldTransform.Rotation);
        ApplyWorldScale(visualTransform, worldTransform.Scale);
    }

    private static void ApplyWorldScale(Transform targetTransform, Vector3 desiredWorldScale)
    {
        Transform parentTransform = targetTransform.parent;

        if (parentTransform == null)
        {
            targetTransform.localScale = desiredWorldScale;
            return;
        }

        Vector3 parentLossyScale = parentTransform.lossyScale;

        targetTransform.localScale = new Vector3(
            DivideScaleComponent(desiredWorldScale.x, parentLossyScale.x),
            DivideScaleComponent(desiredWorldScale.y, parentLossyScale.y),
            DivideScaleComponent(desiredWorldScale.z, parentLossyScale.z)
        );
    }

    private void RefreshVisualObjectLifecycle()
    {
        if (!HasVisualSource())
            return;

        if (ShouldVisualObjectBeActive())
        {
            CreateVisualObjectIfNeeded();
            return;
        }

        DestroyVisualObject();
    }

    private bool HasVisualSource()
    {
        if (visualSource == null)
            return false;

        if (IsPrefabVisualSource())
            return true;

        return HasValidSceneVisualSource();
    }

    private void MigrateLegacyVisualSourceIfNeeded()
    {
        if (visualSource != null)
            return;

        if (sceneVisualRoot != null)
        {
            visualSource = sceneVisualRoot.gameObject;
            return;
        }

        if (visualPrefab != null)
        {
            visualSource = visualPrefab;
        }
    }

    private bool IsPrefabVisualSource()
    {
        return visualSource != null && !visualSource.scene.IsValid();
    }

    private bool IsSceneVisualSource()
    {
        return visualSource != null && visualSource.scene.IsValid();
    }

    private bool HasValidSceneVisualSource()
    {
        return IsSceneVisualSource() && visualSource.transform.parent == transform;
    }

    private bool ShouldVisualObjectBeActive()
    {
        if (isPlayer)
            return true;

        ResolveWorldManager();

        if (worldManager == null || worldTransform == null)
            return true;

        if (!worldManager.UseDistanceBasedVisualStreaming)
            return true;

        if (!worldManager.TryGetPlayerWorldPosition(out WorldVector3D playerWorldPosition))
            return true;

        double deltaX = worldTransform.Position.X - playerWorldPosition.X;
        double deltaZ = worldTransform.Position.Z - playerWorldPosition.Z;
        double squaredDistance = deltaX * deltaX + deltaZ * deltaZ;
        double activeDistance = worldManager.VisualActiveDistance;

        return squaredDistance <= activeDistance * activeDistance;
    }

    private bool ShouldToggleSceneVisualActiveState()
    {
        return worldManager != null && worldManager.UseDistanceBasedVisualStreaming;
    }

    private static float DivideScaleComponent(float desiredValue, float parentValue)
    {
        if (Mathf.Approximately(parentValue, 0f))
            return desiredValue;

        return desiredValue / parentValue;
    }

    private void DrawAgentGizmoIcon()
    {
        if (!TryGetAgentGizmoPosition(out Vector3 gizmoPosition))
            return;

        Gizmos.DrawIcon(
            gizmoPosition,
            GetAgentGizmoIconName(),
            true
        );
    }

    private string GetAgentGizmoIconName()
    {
        if (gizmoIcon != null)
            return gizmoIcon.name;

        return isPlayer
            ? "WorldAgentPlayer"
            : "WorldAgent";
    }

    private bool TryGetAgentGizmoPosition(out Vector3 gizmoPosition)
    {
        if (visualObject != null)
        {
            gizmoPosition = visualObject.transform.position;
            return true;
        }

        ResolveWorldManager();

        if (worldTransform != null && worldManager != null)
        {
            gizmoPosition = worldManager.WorldToLocalPosition(worldTransform);
            return true;
        }

        gizmoPosition = transform.position;
        return true;
    }
}
}
