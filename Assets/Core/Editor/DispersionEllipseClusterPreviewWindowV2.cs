using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DispersionEllipseClusterPreviewWindowV2 : EditorWindow
{
    private const string WindowTitle = "Dispersion Cluster Preview V2";
    private const float MetersPerKilometer = 1000f;
    private const float MinDistanceKm = 0f;
    private const float MaxDistanceKm = 50f;
    private const int PreviewHeight = 380;
    private const int EllipseSegments = 72;

    private const string ProfileGuidKey = "BattleShip.DispersionClusterPreviewV2.ProfileGuid";
    private const string DistanceKmKey = "BattleShip.DispersionClusterPreviewV2.DistanceKm";
    private const string SalvoCountKey = "BattleShip.DispersionClusterPreviewV2.SalvoCount";
    private const string TurretsCountKey = "BattleShip.DispersionClusterPreviewV2.TurretsCount";
    private const string GunsPerTurretKey = "BattleShip.DispersionClusterPreviewV2.GunsPerTurret";
    private const string GaussianSpreadFactorKey = "BattleShip.DispersionClusterPreviewV2.GaussianSpreadFactor";
    private const string SalvoSpreadFactorKey = "BattleShip.DispersionClusterPreviewV2.SalvoSpreadFactor";
    private const string TurretSpreadFactorKey = "BattleShip.DispersionClusterPreviewV2.TurretSpreadFactor";
    private const string GunClusterSpreadFactorKey = "BattleShip.DispersionClusterPreviewV2.GunClusterSpreadFactor";
    private const string ShowGridKey = "BattleShip.DispersionClusterPreviewV2.ShowGrid";
    private const string ShowAxesKey = "BattleShip.DispersionClusterPreviewV2.ShowAxes";
    private const string ShowTargetEllipseKey = "BattleShip.DispersionClusterPreviewV2.ShowTargetEllipse";
    private const string TargetLengthMetersKey = "BattleShip.DispersionClusterPreviewV2.TargetLengthMeters";
    private const string TargetWidthMetersKey = "BattleShip.DispersionClusterPreviewV2.TargetWidthMeters";

    private const float DefaultDistanceKm = 0.75f;
    private const int DefaultSalvoCount = 1;
    private const int DefaultTurretsCount = 3;
    private const int DefaultGunsPerTurret = 3;
    private const float DefaultGaussianSpreadFactor = 0.35f;
    private const float DefaultSalvoSpreadFactor = 0.35f;
    private const float DefaultTurretSpreadFactor = 0.16f;
    private const float DefaultGunClusterSpreadFactor = 0.04f;
    private const float DefaultTargetLengthMeters = 180f;
    private const float DefaultTargetWidthMeters = 28f;

    private DispersionEllipseProfile profile;
    private float distanceKm;
    private int salvoCount;
    private int turretsCount;
    private int gunsPerTurret;
    private float gaussianSpreadFactor;
    private float salvoSpreadFactor;
    private float turretSpreadFactor;
    private float gunClusterSpreadFactor;
    private bool showGrid;
    private bool showAxes;
    private bool showTargetEllipse;
    private float targetLengthMeters;
    private float targetWidthMeters;
    private int regenerationVersion;

    private readonly List<Vector2> previewPoints = new List<Vector2>();
    private PreviewState lastPreviewState;
    private bool hasPreviewState;

    [MenuItem("Tools/BattleShip/Dispersion Cluster Preview V2")]
    public static void OpenWindow()
    {
        DispersionEllipseClusterPreviewWindowV2 window = GetWindow<DispersionEllipseClusterPreviewWindowV2>();
        window.titleContent = new GUIContent(WindowTitle);
        window.minSize = new Vector2(440f, 560f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent(WindowTitle);
        LoadPrefs();
        RebuildPreviewIfNeeded(force: true);
    }

    private void OnDisable()
    {
        SavePrefs();
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        DrawToolbar();
        EditorGUILayout.Space(6f);
        DrawControls();
        EditorGUILayout.Space(8f);
        DrawPreviewArea();
        EditorGUILayout.Space(6f);
        DrawLegend();

        if (EditorGUI.EndChangeCheck())
        {
            ClampInputs();
            SavePrefs();
            RebuildPreviewIfNeeded(force: false);
            Repaint();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label(WindowTitle, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Use Selected", EditorStyles.toolbarButton, GUILayout.Width(92f)))
            {
                profile = Selection.activeObject as DispersionEllipseProfile;
                SavePrefs();
                RebuildPreviewIfNeeded(force: true);
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Regenerate", EditorStyles.toolbarButton, GUILayout.Width(88f)))
            {
                regenerationVersion++;
                RebuildPreviewIfNeeded(force: true);
                GUI.FocusControl(null);
            }
        }
    }

    private void DrawControls()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            profile = (DispersionEllipseProfile)EditorGUILayout.ObjectField(
                "Dispersion Profile",
                profile,
                typeof(DispersionEllipseProfile),
                false
            );

            distanceKm = EditorGUILayout.Slider("Distance (km)", distanceKm, MinDistanceKm, MaxDistanceKm);
            salvoCount = EditorGUILayout.IntField("Salvo Count", salvoCount);
            turretsCount = EditorGUILayout.IntField("Turrets Count", turretsCount);
            gunsPerTurret = EditorGUILayout.IntField("Guns Per Turret", gunsPerTurret);
            gaussianSpreadFactor = EditorGUILayout.Slider("Gaussian Spread", gaussianSpreadFactor, 0.05f, 1f);
            salvoSpreadFactor = EditorGUILayout.Slider("Salvo Spread", salvoSpreadFactor, 0f, 1f);
            turretSpreadFactor = EditorGUILayout.Slider("Turret Spread", turretSpreadFactor, 0f, 1f);
            gunClusterSpreadFactor = EditorGUILayout.Slider("Gun Cluster Spread", gunClusterSpreadFactor, 0f, 0.5f);
            showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);
            showAxes = EditorGUILayout.Toggle("Show Axes", showAxes);
            showTargetEllipse = EditorGUILayout.Toggle("Show Target Ellipse", showTargetEllipse);
            targetLengthMeters = EditorGUILayout.FloatField("Target Length (m)", targetLengthMeters);
            targetWidthMeters = EditorGUILayout.FloatField("Target Width (m)", targetWidthMeters);
        }
    }

    private void DrawPreviewArea()
    {
        Rect previewRect = GUILayoutUtility.GetRect(
            GUIContent.none,
            GUIStyle.none,
            GUILayout.Height(PreviewHeight),
            GUILayout.ExpandWidth(true)
        );

        EditorGUI.DrawRect(previewRect, new Color(0.14f, 0.14f, 0.14f, 1f));

        if (profile == null)
        {
            EditorGUI.HelpBox(previewRect, "Assign a DispersionEllipseProfile to preview the ellipse.", MessageType.Info);
            return;
        }

        if (profile.Points == null || profile.Points.Count == 0)
        {
            EditorGUI.HelpBox(previewRect, "The selected profile has no dispersion points.", MessageType.Warning);
            return;
        }

        DispersionEllipse ellipse = profile.Evaluate(distanceKm * MetersPerKilometer);
        if (ellipse.lateralSpread <= 0f || ellipse.longitudinalSpread <= 0f)
        {
            EditorGUI.HelpBox(previewRect, "The evaluated ellipse has zero spread at this distance.", MessageType.Warning);
            return;
        }

        RebuildPreviewIfNeeded(force: false);
        DrawPreview(previewRect, ellipse);
    }

    private void DrawLegend()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (profile == null)
            {
                EditorGUILayout.LabelField("Select a dispersion profile to begin.");
                return;
            }

            DispersionEllipse ellipse = profile.Evaluate(distanceKm * MetersPerKilometer);
            EditorGUILayout.LabelField("Evaluated Ellipse", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Longitudinal: {ellipse.longitudinalSpread:0.##} m");
            EditorGUILayout.LabelField($"Lateral: {ellipse.lateralSpread:0.##} m");
            EditorGUILayout.LabelField($"Salvos: {salvoCount}");
            EditorGUILayout.LabelField($"Shells per salvo: {turretsCount * gunsPerTurret}");
            EditorGUILayout.LabelField($"Preview Points: {previewPoints.Count}");
        }
    }

    private void DrawPreview(Rect rect, DispersionEllipse ellipse)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        Handles.BeginGUI();

        Vector2 center = rect.center;
        float maxDimension = Mathf.Max(Mathf.Max(ellipse.lateralSpread, ellipse.longitudinalSpread), 1f);
        float targetSize = Mathf.Min(rect.width, rect.height) * 0.72f;
        float scale = targetSize / maxDimension;
        float radiusX = ellipse.lateralSpread * 0.5f * scale;
        float radiusY = ellipse.longitudinalSpread * 0.5f * scale;

        if (showGrid)
        {
            DrawGrid(rect);
        }

        Vector3[] ellipsePoints = BuildEllipsePoints(center, radiusX, radiusY, EllipseSegments);

        Handles.color = new Color(0.28f, 0.57f, 1f, 0.12f);
        Handles.DrawAAConvexPolygon(ellipsePoints);

        Handles.color = new Color(0.28f, 0.57f, 1f, 1f);
        Handles.DrawAAPolyLine(2.5f, ellipsePoints);

        if (showAxes)
        {
            DrawAxes(rect, center);
        }

        if (showTargetEllipse)
        {
            DrawTargetEllipse(center, scale);
        }

        DrawClusterPoints(center, scale);
        DrawPreviewLabels(rect, center, ellipse);

        Handles.EndGUI();
    }

    private void DrawClusterPoints(Vector2 center, float scale)
    {
        Handles.color = new Color(1f, 0.39f, 0.3f, 0.95f);

        for (int i = 0; i < previewPoints.Count; i++)
        {
            Vector2 point = previewPoints[i];
            Vector3 guiPoint = new Vector3(
                center.x + (point.x * scale),
                center.y - (point.y * scale),
                0f
            );

            Handles.DrawSolidDisc(guiPoint, Vector3.forward, 2.6f);
        }
    }

    private static void DrawAxes(Rect rect, Vector2 center)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.45f);
        Handles.DrawDottedLine(
            new Vector3(rect.xMin + 18f, center.y, 0f),
            new Vector3(rect.xMax - 18f, center.y, 0f),
            4f
        );
        Handles.DrawDottedLine(
            new Vector3(center.x, rect.yMin + 18f, 0f),
            new Vector3(center.x, rect.yMax - 18f, 0f),
            4f
        );
        Handles.DrawSolidDisc(center, Vector3.forward, 2.5f);
    }

    private void DrawTargetEllipse(Vector2 center, float scale)
    {
        float targetRadiusX = targetWidthMeters * 0.5f * scale;
        float targetRadiusY = targetLengthMeters * 0.5f * scale;
        Vector3[] targetEllipsePoints = BuildEllipsePoints(center, targetRadiusX, targetRadiusY, EllipseSegments);

        Handles.color = new Color(1f, 0.86f, 0.22f, 0.10f);
        Handles.DrawAAConvexPolygon(targetEllipsePoints);

        Handles.color = new Color(1f, 0.86f, 0.22f, 0.95f);
        Handles.DrawAAPolyLine(2.2f, targetEllipsePoints);
    }

    private static void DrawPreviewLabels(Rect rect, Vector2 center, DispersionEllipse ellipse)
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        GUIStyle valueStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft
        };

        Rect fireRect = new Rect(center.x - 72f, rect.yMin + 8f, 144f, 18f);
        GUI.Label(fireRect, "Fire Direction", titleStyle);

        Rect valueRect = new Rect(rect.xMax - 182f, rect.yMin + 4f, 172f, 34f);
        GUI.Label(
            valueRect,
            $"Longitudinal: {ellipse.longitudinalSpread:0.##} m\nLateral: {ellipse.lateralSpread:0.##} m",
            valueStyle
        );
    }

    private static void DrawGrid(Rect rect)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.045f);

        const float spacing = 40f;
        for (float x = rect.xMin; x <= rect.xMax; x += spacing)
        {
            Handles.DrawLine(new Vector3(x, rect.yMin, 0f), new Vector3(x, rect.yMax, 0f));
        }

        for (float y = rect.yMin; y <= rect.yMax; y += spacing)
        {
            Handles.DrawLine(new Vector3(rect.xMin, y, 0f), new Vector3(rect.xMax, y, 0f));
        }
    }

    private void RebuildPreviewIfNeeded(bool force)
    {
        ClampInputs();

        PreviewState nextState = new PreviewState(
            profile,
            distanceKm,
            salvoCount,
            turretsCount,
            gunsPerTurret,
            gaussianSpreadFactor,
            salvoSpreadFactor,
            turretSpreadFactor,
            gunClusterSpreadFactor,
            regenerationVersion
        );

        if (!force && hasPreviewState && lastPreviewState.Equals(nextState))
            return;

        previewPoints.Clear();
        hasPreviewState = true;
        lastPreviewState = nextState;

        if (profile == null || profile.Points == null || profile.Points.Count == 0)
            return;

        DispersionEllipse ellipse = profile.Evaluate(distanceKm * MetersPerKilometer);
        if (ellipse.lateralSpread <= 0f || ellipse.longitudinalSpread <= 0f)
            return;

        NavalSalvoDistributionGenerator.GeneratePoints(
            previewPoints,
            ellipse.lateralSpread,
            ellipse.longitudinalSpread,
            salvoCount,
            turretsCount,
            gunsPerTurret,
            gaussianSpreadFactor,
            salvoSpreadFactor,
            turretSpreadFactor,
            gunClusterSpreadFactor,
            Mathf.Max(1, regenerationVersion)
        );
    }

    private static Vector3[] BuildEllipsePoints(Vector2 center, float radiusX, float radiusY, int segments)
    {
        Vector3[] points = new Vector3[segments + 1];

        for (int i = 0; i < segments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segments;
            float x = center.x + (Mathf.Cos(angle) * radiusX);
            float y = center.y + (Mathf.Sin(angle) * radiusY);
            points[i] = new Vector3(x, y, 0f);
        }

        points[segments] = points[0];
        return points;
    }

    private void ClampInputs()
    {
        distanceKm = Mathf.Clamp(distanceKm, MinDistanceKm, MaxDistanceKm);
        salvoCount = Mathf.Max(1, salvoCount);
        turretsCount = Mathf.Max(1, turretsCount);
        gunsPerTurret = Mathf.Max(1, gunsPerTurret);
        gaussianSpreadFactor = Mathf.Clamp(
            gaussianSpreadFactor,
            EllipseClusterDistributionGenerator.MinGaussianSpreadFactor,
            EllipseClusterDistributionGenerator.MaxGaussianSpreadFactor
        );
        salvoSpreadFactor = Mathf.Clamp(
            salvoSpreadFactor,
            NavalSalvoDistributionGenerator.MinSpreadFactor,
            NavalSalvoDistributionGenerator.MaxSpreadFactor
        );
        turretSpreadFactor = Mathf.Clamp(
            turretSpreadFactor,
            NavalSalvoDistributionGenerator.MinSpreadFactor,
            NavalSalvoDistributionGenerator.MaxSpreadFactor
        );
        gunClusterSpreadFactor = Mathf.Clamp(
            gunClusterSpreadFactor,
            NavalSalvoDistributionGenerator.MinSpreadFactor,
            NavalSalvoDistributionGenerator.MaxGunClusterSpreadFactor
        );
        targetLengthMeters = Mathf.Max(EllipseClusterDistributionGenerator.MinEllipseSize, targetLengthMeters);
        targetWidthMeters = Mathf.Max(EllipseClusterDistributionGenerator.MinEllipseSize, targetWidthMeters);
    }

    private void SavePrefs()
    {
        EditorPrefs.SetFloat(DistanceKmKey, distanceKm);
        EditorPrefs.SetInt(SalvoCountKey, salvoCount);
        EditorPrefs.SetInt(TurretsCountKey, turretsCount);
        EditorPrefs.SetInt(GunsPerTurretKey, gunsPerTurret);
        EditorPrefs.SetFloat(GaussianSpreadFactorKey, gaussianSpreadFactor);
        EditorPrefs.SetFloat(SalvoSpreadFactorKey, salvoSpreadFactor);
        EditorPrefs.SetFloat(TurretSpreadFactorKey, turretSpreadFactor);
        EditorPrefs.SetFloat(GunClusterSpreadFactorKey, gunClusterSpreadFactor);
        EditorPrefs.SetBool(ShowGridKey, showGrid);
        EditorPrefs.SetBool(ShowAxesKey, showAxes);
        EditorPrefs.SetBool(ShowTargetEllipseKey, showTargetEllipse);
        EditorPrefs.SetFloat(TargetLengthMetersKey, targetLengthMeters);
        EditorPrefs.SetFloat(TargetWidthMetersKey, targetWidthMeters);

        string profileGuid = string.Empty;
        if (profile != null)
        {
            string profilePath = AssetDatabase.GetAssetPath(profile);
            if (!string.IsNullOrEmpty(profilePath))
            {
                profileGuid = AssetDatabase.AssetPathToGUID(profilePath);
            }
        }

        EditorPrefs.SetString(ProfileGuidKey, profileGuid);
    }

    private void LoadPrefs()
    {
        distanceKm = EditorPrefs.GetFloat(DistanceKmKey, DefaultDistanceKm);
        salvoCount = EditorPrefs.GetInt(SalvoCountKey, DefaultSalvoCount);
        turretsCount = EditorPrefs.GetInt(TurretsCountKey, DefaultTurretsCount);
        gunsPerTurret = EditorPrefs.GetInt(GunsPerTurretKey, DefaultGunsPerTurret);
        gaussianSpreadFactor = EditorPrefs.GetFloat(GaussianSpreadFactorKey, DefaultGaussianSpreadFactor);
        salvoSpreadFactor = EditorPrefs.GetFloat(SalvoSpreadFactorKey, DefaultSalvoSpreadFactor);
        turretSpreadFactor = EditorPrefs.GetFloat(TurretSpreadFactorKey, DefaultTurretSpreadFactor);
        gunClusterSpreadFactor = EditorPrefs.GetFloat(GunClusterSpreadFactorKey, DefaultGunClusterSpreadFactor);
        showGrid = EditorPrefs.GetBool(ShowGridKey, true);
        showAxes = EditorPrefs.GetBool(ShowAxesKey, true);
        showTargetEllipse = EditorPrefs.GetBool(ShowTargetEllipseKey, true);
        targetLengthMeters = EditorPrefs.GetFloat(TargetLengthMetersKey, DefaultTargetLengthMeters);
        targetWidthMeters = EditorPrefs.GetFloat(TargetWidthMetersKey, DefaultTargetWidthMeters);

        string profileGuid = EditorPrefs.GetString(ProfileGuidKey, string.Empty);
        if (!string.IsNullOrEmpty(profileGuid))
        {
            string profilePath = AssetDatabase.GUIDToAssetPath(profileGuid);
            if (!string.IsNullOrEmpty(profilePath))
            {
                profile = AssetDatabase.LoadAssetAtPath<DispersionEllipseProfile>(profilePath);
            }
        }

        ClampInputs();
    }

    private readonly struct PreviewState : IEquatable<PreviewState>
    {
        private readonly DispersionEllipseProfile profile;
        private readonly float distanceKm;
        private readonly int salvoCount;
        private readonly int turretsCount;
        private readonly int gunsPerTurret;
        private readonly float gaussianSpreadFactor;
        private readonly float salvoSpreadFactor;
        private readonly float turretSpreadFactor;
        private readonly float gunClusterSpreadFactor;
        private readonly int regenerationVersion;

        public PreviewState(
            DispersionEllipseProfile profile,
            float distanceKm,
            int salvoCount,
            int turretsCount,
            int gunsPerTurret,
            float gaussianSpreadFactor,
            float salvoSpreadFactor,
            float turretSpreadFactor,
            float gunClusterSpreadFactor,
            int regenerationVersion)
        {
            this.profile = profile;
            this.distanceKm = distanceKm;
            this.salvoCount = salvoCount;
            this.turretsCount = turretsCount;
            this.gunsPerTurret = gunsPerTurret;
            this.gaussianSpreadFactor = gaussianSpreadFactor;
            this.salvoSpreadFactor = salvoSpreadFactor;
            this.turretSpreadFactor = turretSpreadFactor;
            this.gunClusterSpreadFactor = gunClusterSpreadFactor;
            this.regenerationVersion = regenerationVersion;
        }

        public bool Equals(PreviewState other)
        {
            return profile == other.profile
                && Mathf.Approximately(distanceKm, other.distanceKm)
                && salvoCount == other.salvoCount
                && turretsCount == other.turretsCount
                && gunsPerTurret == other.gunsPerTurret
                && Mathf.Approximately(gaussianSpreadFactor, other.gaussianSpreadFactor)
                && Mathf.Approximately(salvoSpreadFactor, other.salvoSpreadFactor)
                && Mathf.Approximately(turretSpreadFactor, other.turretSpreadFactor)
                && Mathf.Approximately(gunClusterSpreadFactor, other.gunClusterSpreadFactor)
                && regenerationVersion == other.regenerationVersion;
        }

        public override bool Equals(object obj)
        {
            return obj is PreviewState other && Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(profile);
            hash.Add(distanceKm);
            hash.Add(salvoCount);
            hash.Add(turretsCount);
            hash.Add(gunsPerTurret);
            hash.Add(gaussianSpreadFactor);
            hash.Add(salvoSpreadFactor);
            hash.Add(turretSpreadFactor);
            hash.Add(gunClusterSpreadFactor);
            hash.Add(regenerationVersion);
            return hash.ToHashCode();
        }
    }
}
