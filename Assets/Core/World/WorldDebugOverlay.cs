using System.Text;
using UnityEngine;

namespace World
{
[DefaultExecutionOrder(1000)]
public class WorldDebugOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldManager worldManager;

    [Header("Display")]
    [SerializeField] private bool showOverlay = true;
    [SerializeField] private Vector2 overlayPosition = new Vector2(16f, 16f);
    [SerializeField] private Vector2 overlaySize = new Vector2(460f, 220f);

    private readonly StringBuilder stringBuilder = new StringBuilder(512);

    private void Awake()
    {
        ResolveWorldManager();
    }

    private void OnEnable()
    {
        ResolveWorldManager();
    }

    private void OnGUI()
    {
        if (!showOverlay)
            return;

        ResolveWorldManager();

        Rect area = new Rect(
            overlayPosition.x,
            overlayPosition.y,
            overlaySize.x,
            overlaySize.y
        );

        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label(BuildOverlayText());
        GUILayout.EndArea();
    }

    private void ResolveWorldManager()
    {
        if (worldManager != null)
            return;

        worldManager = WorldManager.Instance != null
            ? WorldManager.Instance
            : FindFirstObjectByType<WorldManager>();
    }

    private string BuildOverlayText()
    {
        stringBuilder.Clear();
        stringBuilder.AppendLine("World Debug Overlay");

        if (worldManager == null)
        {
            stringBuilder.AppendLine("WorldManager: не найден");
            return stringBuilder.ToString();
        }

        stringBuilder.AppendLine($"WorldOrigin: {worldManager.WorldOrigin}");
        stringBuilder.AppendLine($"Rebase Threshold: {worldManager.RebaseThreshold:F2}");
        stringBuilder.AppendLine($"Rebase Count: {worldManager.RebaseCount}");
        stringBuilder.AppendLine($"Last Shift: {worldManager.LastShift}");
        stringBuilder.AppendLine($"Agents: {worldManager.RegisteredAgents.Count}");

        if (!worldManager.TryGetPlayerAgent(out WorldAgent playerAgent) || playerAgent == null)
        {
            stringBuilder.AppendLine("Player Agent: не найден");
            return stringBuilder.ToString();
        }

        WorldTransform playerWorldTransform = playerAgent.WorldTransform;
        Vector3 playerLocalPosition = worldManager.WorldToLocalPosition(playerWorldTransform);
        Vector3 playerVisualPosition = playerAgent.VisualObject != null
            ? playerAgent.VisualObject.transform.position
            : Vector3.zero;
        float playerDistanceFromLocalOrigin = worldManager.GetPlayerDistanceFromLocalOrigin();

        stringBuilder.AppendLine($"Player World: {playerWorldTransform.Position}");
        stringBuilder.AppendLine($"Player Local: {playerLocalPosition}");
        stringBuilder.AppendLine($"Player Visual: {playerVisualPosition}");
        stringBuilder.AppendLine($"Player Distance To Local Origin: {playerDistanceFromLocalOrigin:F2}");

        return stringBuilder.ToString();
    }
}
}
