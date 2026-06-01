public enum MusicState
{
    Begin = 0,
    Normal = 1,
    Battle = 2,
    Tension = 3,
    HighTension = 4,
    Victory = 5,
    Defeat = 6,
    Joy = 7,
    NavigationMap = 8,
    InBase = 9,
    UnderDepthCharges = 10
}

public static class MusicStateUtility
{
    public static string GetCfgSectionName(MusicState state)
    {
        switch (state)
        {
            case MusicState.Begin:
                return "BEGIN";
            case MusicState.Normal:
                return "NORMAL";
            case MusicState.Battle:
                return "BATTLE";
            case MusicState.Tension:
                return "TENSION";
            case MusicState.HighTension:
                return "HIGH TENSION";
            case MusicState.Victory:
                return "VICTORY";
            case MusicState.Defeat:
                return "DEFEAT";
            case MusicState.Joy:
                return "JOY";
            case MusicState.NavigationMap:
                return "NAVIGATION MAP";
            case MusicState.InBase:
                return "IN BASE";
            case MusicState.UnderDepthCharges:
                return "UNDER DEPTH CHARGES";
            default:
                return state.ToString().ToUpperInvariant();
        }
    }

    public static bool TryParseCfgSectionName(string value, out MusicState state)
    {
        string normalized = Normalize(value);

        switch (normalized)
        {
            case "BEGIN":
                state = MusicState.Begin;
                return true;
            case "NORMAL":
                state = MusicState.Normal;
                return true;
            case "BATTLE":
                state = MusicState.Battle;
                return true;
            case "TENSION":
                state = MusicState.Tension;
                return true;
            case "HIGHTENSION":
                state = MusicState.HighTension;
                return true;
            case "VICTORY":
                state = MusicState.Victory;
                return true;
            case "DEFEAT":
                state = MusicState.Defeat;
                return true;
            case "JOY":
                state = MusicState.Joy;
                return true;
            case "NAVIGATIONMAP":
                state = MusicState.NavigationMap;
                return true;
            case "INBASE":
                state = MusicState.InBase;
                return true;
            case "UNDERDEPTHCHARGES":
                state = MusicState.UnderDepthCharges;
                return true;
            default:
                state = default;
                return false;
        }
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .Trim()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToUpperInvariant();
    }
}
