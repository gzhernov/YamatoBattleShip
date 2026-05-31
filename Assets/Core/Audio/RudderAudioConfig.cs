using UnityEngine;

[CreateAssetMenu(
    fileName = "RudderAudioConfig",
    menuName = "Ship/Rudder Audio Config",
    order = 3
)]
public class RudderAudioConfig : ScriptableObject
{
    [Header("Switch")]
    [SerializeField] private AudioClip switchClip;

    [Range(0f, 1f)]
    [SerializeField] private float switchVolume = 1f;

    [Min(0f)]
    [SerializeField] private float pitchRandomization = 0f;

    [SerializeField] private bool logValidationWarnings = true;

    public AudioClip SwitchClip => switchClip;
    public float SwitchVolume => switchVolume;
    public float PitchRandomization => pitchRandomization;

    public void Validate(UnityEngine.Object context)
    {
        if (!logValidationWarnings)
            return;

        if (switchClip == null)
        {
            Debug.LogWarning($"RudderAudioConfig '{name}': switch clip is not assigned.", context);
        }
    }

    private void OnValidate()
    {
        switchVolume = Mathf.Clamp01(switchVolume);
        pitchRandomization = Mathf.Max(0f, pitchRandomization);
        Validate(this);
    }
}
