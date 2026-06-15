using System.Collections.Generic;
using UnityEngine;

namespace World
{
[DefaultExecutionOrder(-1000)]
public class WorldManager : MonoBehaviour
{
    
    [Header("World Size")]
    [SerializeField] private float worldWidthKilometers;
    [SerializeField] private float worldHeightKilometers;
    
    [Header(" ")]
    [SerializeField] private WorldVector3D worldOrigin;

    [Header("Rebase")]
    [SerializeField] private float rebaseThreshold = 4096f;
    [SerializeField, ReadOnlyInspector] private int rebaseCount;
    [SerializeField, ReadOnlyInspector] private Vector3 lastShift;

    [Header("Visual Streaming")]
    [Tooltip("Если выключено, visualObject у WorldAgent считается активным всегда. Если включено, visualObject активируется или скрывается по дистанции до игрока в пределах Visual Active Distance.")]
    [SerializeField] private bool useDistanceBasedVisualStreaming = true;
    [Tooltip("Максимальная дистанция до игрока, в пределах которой visualObject остаётся активным при включенном Use Distance Based Visual Streaming.")]
    [SerializeField] private float visualActiveDistance = 5000f;

    [Header("Runtime")]
    [SerializeField, ReadOnlyInspector] private List<WorldAgent> registeredAgents = new List<WorldAgent>();
    [SerializeField, ReadOnlyInspector] private WorldAgent playerAgent;
    [SerializeField, ReadOnlyInspector] private Vector3 playerLocalVisualPosition;
    [SerializeField, ReadOnlyInspector] private float playerDistanceFromLocalOrigin;

    [Header("Gizmo")]
    [SerializeField] private bool showWorldOriginGizmo = true;
    [SerializeField] private Color worldOriginGizmoColor = Color.cyan;

    
    public static WorldManager Instance { get; private set; }

    public WorldVector3D WorldOrigin => worldOrigin;
    public float RebaseThreshold => rebaseThreshold;
    public int RebaseCount => rebaseCount;
    public Vector3 LastShift => lastShift;
    public IReadOnlyList<WorldAgent> RegisteredAgents => registeredAgents;
    public bool UseDistanceBasedVisualStreaming => useDistanceBasedVisualStreaming;
    public float VisualActiveDistance => visualActiveDistance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("WorldManager: в сцене найдено больше одного WorldManager.", this);
            enabled = false;
            return;
        }

        Instance = this;
        visualActiveDistance = Mathf.Max(0f, visualActiveDistance);
        CleanupMissingAgents();
    }

    private void OnValidate()
    {
        visualActiveDistance = Mathf.Max(0f, visualActiveDistance);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        RefreshPlayerAgent();
        UpdatePlayerLocalVisualState();
        TryPerformRebase();
    }

    private void OnDrawGizmos()
    {
        if (!showWorldOriginGizmo)
            return;

        DrawWorldOriginGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showWorldOriginGizmo)
            return;

        DrawWorldOriginGizmo();
    }

    public bool RegisterAgent(WorldAgent agent)
    {
        if (agent == null)
            return false;

        CleanupMissingAgents();

        if (registeredAgents.Contains(agent))
            return true;

        if (agent.IsPlayer && HasOtherPlayerAgent(agent))
        {
            Debug.LogError(
                "WorldManager: в сцене обнаружен второй WorldAgent с isPlayer = true.",
                agent
            );
            return false;
        }

        registeredAgents.Add(agent);
        return true;
    }

    public void UnregisterAgent(WorldAgent agent)
    {
        if (agent == null)
            return;

        registeredAgents.Remove(agent);
    }

    public Vector3 WorldToLocalPosition(WorldTransform worldTransform)
    {
        if (worldTransform == null)
            return Vector3.zero;

        return worldTransform.GetLocalUnityPosition(worldOrigin);
    }

    public void SetWorldOrigin(double x, double y, double z)
    {
        worldOrigin = new WorldVector3D(x, y, z);
    }

    public void SetWorldOrigin(WorldVector3D newWorldOrigin)
    {
        worldOrigin = newWorldOrigin;
    }

    public void ApplyWorldOriginShift(Vector3 shift)
    {
        worldOrigin += shift;
        lastShift = shift;
        rebaseCount++;
    }

    public bool TryGetPlayerAgent(out WorldAgent playerAgent)
    {
        CleanupMissingAgents();

        RefreshPlayerAgent();

        playerAgent = this.playerAgent;
        return playerAgent != null;
    }

    public bool TryGetPlayerLocalVisualPosition(out Vector3 localVisualPosition)
    {
        RefreshPlayerAgent();
        UpdatePlayerLocalVisualState();

        if (playerAgent == null || playerAgent.VisualObject == null)
        {
            localVisualPosition = Vector3.zero;
            return false;
        }

        localVisualPosition = playerLocalVisualPosition;
        return true;
    }

    public float GetPlayerDistanceFromLocalOrigin()
    {
        RefreshPlayerAgent();
        UpdatePlayerLocalVisualState();
        return playerDistanceFromLocalOrigin;
    }

    public bool TryGetPlayerWorldPosition(out WorldVector3D playerWorldPosition)
    {
        RefreshPlayerAgent();

        if (playerAgent == null || playerAgent.WorldTransform == null)
        {
            playerWorldPosition = default;
            return false;
        }

        playerWorldPosition = playerAgent.WorldTransform.Position;
        return true;
    }

    private bool HasOtherPlayerAgent(WorldAgent candidate)
    {
        for (int i = 0; i < registeredAgents.Count; i++)
        {
            WorldAgent registeredAgent = registeredAgents[i];

            if (registeredAgent == null || registeredAgent == candidate)
                continue;

            if (registeredAgent.IsPlayer)
                return true;
        }

        return false;
    }

    private void CleanupMissingAgents()
    {
        registeredAgents.RemoveAll(agent => agent == null);
    }

    private void RefreshPlayerAgent()
    {
        if (playerAgent != null && registeredAgents.Contains(playerAgent) && playerAgent.IsPlayer)
            return;

        playerAgent = null;

        for (int i = 0; i < registeredAgents.Count; i++)
        {
            WorldAgent agent = registeredAgents[i];

            if (agent != null && agent.IsPlayer)
            {
                playerAgent = agent;
                return;
            }
        }
    }

    private void UpdatePlayerLocalVisualState()
    {
        if (playerAgent == null || playerAgent.VisualObject == null)
        {
            playerLocalVisualPosition = Vector3.zero;
            playerDistanceFromLocalOrigin = 0f;
            return;
        }

        playerLocalVisualPosition = playerAgent.VisualObject.transform.position;
        playerDistanceFromLocalOrigin = new Vector2(
            playerLocalVisualPosition.x,
            playerLocalVisualPosition.z
        ).magnitude;
    }

    private void TryPerformRebase()
    {
        if (playerAgent == null || playerAgent.VisualObject == null)
            return;

        if (playerDistanceFromLocalOrigin < rebaseThreshold)
            return;

        Vector3 horizontalShift = new Vector3(
            playerLocalVisualPosition.x,
            0f,
            playerLocalVisualPosition.z
        );

        if (horizontalShift.sqrMagnitude <= 0f)
            return;

        ApplyWorldOriginShift(horizontalShift);
        UpdatePlayerLocalVisualState();
    }

    private void DrawWorldOriginGizmo()
    {
        Vector3 gizmoPosition = transform.position;
        float circleRadius = Mathf.Max(0.01f, rebaseThreshold);
        float axisLength = circleRadius;

        Gizmos.color = worldOriginGizmoColor;
        DrawWireCircleXZ(gizmoPosition, circleRadius, 96);
        Gizmos.DrawLine(gizmoPosition + Vector3.left * axisLength, gizmoPosition + Vector3.right * axisLength);
        Gizmos.DrawLine(gizmoPosition + Vector3.back * axisLength, gizmoPosition + Vector3.forward * axisLength);
        Gizmos.DrawLine(gizmoPosition + Vector3.down * axisLength, gizmoPosition + Vector3.up * axisLength);
    }

    private static void DrawWireCircleXZ(Vector3 center, float radius, int segments)
    {
        int safeSegments = Mathf.Max(8, segments);
        Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= safeSegments; i++)
        {
            float angle = i / (float)safeSegments * Mathf.PI * 2f;
            Vector3 nextPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
}
