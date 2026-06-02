using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ShipSmokeVfxController : MonoBehaviour
{
    [Serializable]
    public class SmokeVfxBinding
    {
        public EngineTelegraphSector sector;

        [Tooltip("Ручная ссылка на root-объект дыма в сцене.")]
        public GameObject smokeRoot;
    }

    [Header("References")]
    [Tooltip("Необязательно. Если ссылка не задана, контроллер сначала ищет ShipStatuses у родителя, затем в сцене.")]
    [SerializeField] private ShipStatuses shipStatuses;

    [Tooltip("Необязательно. Локально переопределяет SmokeVfxConfig из ShipConfig.")]
    [SerializeField] private SmokeVfxConfig smokeVfxConfigOverride;

    [Tooltip("Список режимов дыма и соответствующих root-объектов в сцене.")]
    [SerializeField] private SmokeVfxBinding[] smokeBindings = Array.Empty<SmokeVfxBinding>();

    [Header("Runtime (Read Only)")]
    [SerializeField, ReadOnlyInspector] private EngineTelegraphSector currentSector;
    [SerializeField, ReadOnlyInspector] private string currentSmokeRootName = string.Empty;

    [Header("Debug")]
    [Tooltip("Включает предупреждения в консоли, если не удаётся найти root для текущего режима.")]
    [SerializeField] private bool logDebugInfo = true;

    private GameObject activeSmokeRoot;
    private bool isSubscribed;
    private bool hasStarted;
    private bool hasAppliedInitialState;

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToEvents();

        if (hasStarted)
        {
            ApplyCurrentSectorImmediate();
        }
    }

    private void Start()
    {
        hasStarted = true;

        if (!hasAppliedInitialState)
        {
            ApplyCurrentSectorImmediate();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        StopAllSmokeRoots();
        activeSmokeRoot = null;
        currentSmokeRootName = string.Empty;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReferences();
        }
    }

    public int SyncFromSmokeConfig()
    {
        SmokeVfxConfig activeConfig = GetActiveSmokeConfig();

        if (activeConfig == null || activeConfig.ModeEntries == null || activeConfig.ModeEntries.Length == 0)
            return 0;

        Dictionary<EngineTelegraphSector, SmokeVfxBinding> existingBindings = BuildBindingMap();
        SmokeVfxBinding[] syncedBindings = new SmokeVfxBinding[activeConfig.ModeEntries.Length];
        int syncedCount = 0;

        for (int i = 0; i < activeConfig.ModeEntries.Length; i++)
        {
            SmokeVfxModeEntry modeEntry = activeConfig.ModeEntries[i];

            if (modeEntry == null)
                continue;

            SmokeVfxBinding binding;

            if (!existingBindings.TryGetValue(modeEntry.sector, out binding))
            {
                binding = CreateDefaultBinding(modeEntry.sector);
            }

            if (binding == null)
                continue;

            binding.sector = modeEntry.sector;
            syncedBindings[syncedCount] = binding;
            syncedCount++;
        }

        if (syncedCount != syncedBindings.Length)
        {
            Array.Resize(ref syncedBindings, syncedCount);
        }

        smokeBindings = syncedBindings;
        return syncedCount;
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = GetComponentInParent<ShipStatuses>();
        }

        if (shipStatuses == null)
        {
            shipStatuses = FindFirstObjectByType<ShipStatuses>();
        }
    }

    private void SubscribeToEvents()
    {
        if (shipStatuses == null || isSubscribed)
            return;

        shipStatuses.OnEngineTelegraphChanged += OnEngineTelegraphChanged;
        isSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (shipStatuses == null || !isSubscribed)
            return;

        shipStatuses.OnEngineTelegraphChanged -= OnEngineTelegraphChanged;
        isSubscribed = false;
    }

    private void OnEngineTelegraphChanged(EngineTelegraphSector sector, EngineTelegraphSectorData sectorData)
    {
        currentSector = sector;
        ApplySector(sector);
    }

    private void ApplyCurrentSectorImmediate()
    {
        currentSector = shipStatuses != null
            ? shipStatuses.CurrentEngineTelegraphSector
            : EngineTelegraphSector.Stop;

        ApplySector(currentSector, true);
    }

    private void ApplySector(EngineTelegraphSector sector, bool forceRestart = false)
    {
        SmokeVfxBinding activeBinding = GetBindingForSector(sector);

        if (activeBinding == null && sector != EngineTelegraphSector.Stop)
        {
            activeBinding = GetBindingForSector(EngineTelegraphSector.Stop);
        }

        GameObject targetRoot = ResolveBindingRoot(activeBinding);

        if (targetRoot == null)
        {
            StopAllSmokeRoots();

            if (logDebugInfo)
            {
                Debug.LogWarning(
                    $"ShipSmokeVfxController: не найден root дыма для сектора '{sector}'. Проверьте bindings.",
                    this
                );
            }

            currentSmokeRootName = string.Empty;
            activeSmokeRoot = null;
            hasAppliedInitialState = true;
            return;
        }

        if (activeSmokeRoot != null && activeSmokeRoot != targetRoot)
        {
            StopSmokeEmission(activeSmokeRoot);
        }

        DeactivateAllOtherRoots(targetRoot);
        ActivateSmokeRoot(targetRoot);

        activeSmokeRoot = targetRoot;
        currentSmokeRootName = targetRoot.name;
        currentSector = sector;
        hasAppliedInitialState = true;
    }

    private SmokeVfxBinding GetBindingForSector(EngineTelegraphSector sector)
    {
        if (smokeBindings == null)
            return null;

        for (int i = 0; i < smokeBindings.Length; i++)
        {
            SmokeVfxBinding binding = smokeBindings[i];

            if (binding != null && binding.sector == sector)
                return binding;
        }

        return null;
    }

    private GameObject ResolveBindingRoot(SmokeVfxBinding binding)
    {
        if (binding == null)
            return null;

        return binding.smokeRoot;
    }

    private void ActivateSmokeRoot(GameObject root)
    {
        if (root == null)
            return;

        if (!root.activeSelf)
        {
            root.SetActive(true);
        }

        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];

            if (particleSystem == null)
                continue;

            if (!particleSystem.gameObject.activeSelf)
            {
                particleSystem.gameObject.SetActive(true);
            }
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];

            if (particleSystem == null)
                continue;

            particleSystem.Play(true);
        }
    }

    private void StopSmokeEmission(GameObject root)
    {
        if (root == null)
            return;

        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];

            if (particleSystem == null)
                continue;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void StopAllSmokeRoots()
    {
        if (smokeBindings == null)
            return;

        for (int i = 0; i < smokeBindings.Length; i++)
        {
            SmokeVfxBinding binding = smokeBindings[i];
            GameObject root = binding != null ? ResolveBindingRoot(binding) : null;

            if (root == null)
                continue;

            StopSmokeEmission(root);
        }
    }

    private void DeactivateAllOtherRoots(GameObject targetRoot)
    {
        if (smokeBindings == null)
            return;

        for (int i = 0; i < smokeBindings.Length; i++)
        {
            SmokeVfxBinding binding = smokeBindings[i];
            GameObject root = binding != null ? ResolveBindingRoot(binding) : null;

            if (root == null || root == targetRoot)
                continue;

            StopSmokeEmission(root);
        }
    }

    private Dictionary<EngineTelegraphSector, SmokeVfxBinding> BuildBindingMap()
    {
        Dictionary<EngineTelegraphSector, SmokeVfxBinding> existingBindings = new Dictionary<EngineTelegraphSector, SmokeVfxBinding>();

        if (smokeBindings == null)
            return existingBindings;

        for (int i = 0; i < smokeBindings.Length; i++)
        {
            SmokeVfxBinding binding = smokeBindings[i];

            if (binding == null || existingBindings.ContainsKey(binding.sector))
                continue;

            existingBindings.Add(binding.sector, binding);
        }

        return existingBindings;
    }

    private SmokeVfxConfig GetActiveSmokeConfig()
    {
        if (smokeVfxConfigOverride != null)
            return smokeVfxConfigOverride;

        if (shipStatuses == null || shipStatuses.ShipConfig == null)
            return null;

        return shipStatuses.ShipConfig.SmokeVfxConfig;
    }

    private static SmokeVfxBinding CreateDefaultBinding(EngineTelegraphSector sector)
    {
        return new SmokeVfxBinding
        {
            sector = sector
        };
    }
}