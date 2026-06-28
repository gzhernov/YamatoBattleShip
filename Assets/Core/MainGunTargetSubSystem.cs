using UnityEngine;

[DisallowMultipleComponent]
public class MainGunTargetSubSystem : MonoBehaviour
{
    [Header("РќР°РІРµРґРµРЅРёРµ")]
    [Tooltip("Р¦РµР»РµРІРѕР№ bearing РґР»СЏ РіР»Р°РІРЅРѕР№ С†РµР»Рё РІ РіСЂР°РґСѓСЃР°С… 0..360.")]
    [SerializeField] private float targetBearing;

    [Tooltip("Р¦РµР»РµРІР°СЏ РґРёСЃС‚Р°РЅС†РёСЏ РґРѕ РіР»Р°РІРЅРѕР№ С†РµР»Рё.")]
    [SerializeField] private float targetDistanse;

    private void OnValidate()
    {
        targetBearing = NormalizeBearing(targetBearing);
        targetDistanse = Mathf.Max(0f, targetDistanse);
    }

    public void SetTargetBearing(float bearing)
    {
        targetBearing = NormalizeBearing(bearing);
    }

    public void SetTargetDistanse(float distance)
    {
        targetDistanse = Mathf.Max(0f, distance);
    }

    public float GetTargetBearing()
    {
        return targetBearing;
    }

    public float GetTargetDistanse()
    {
        return targetDistanse;
    }

    private static float NormalizeBearing(float bearing)
    {
        return Mathf.Repeat(bearing, 360f);
    }
}
