namespace MosaicShell.Core.Scale;

/// <summary>
/// User zoom only. Avalonia already applies per-monitor DPI in DIPs —
/// do not multiply OS DPI into layout transforms.
/// </summary>
public sealed class ScaleContract
{
    public double UserScale { get; private set; } = 1.0;

    /// <summary>Alias for UserScale (layout / tile overlay zoom).</summary>
    public double UiScale => UserScale;

    public void SetUserScale(double userScale)
    {
        if (userScale < 0.75 || userScale > 2.0)
            throw new ArgumentOutOfRangeException(nameof(userScale), "UserScale must be between 0.75 and 2.0.");
        UserScale = Math.Round(userScale, 4);
    }

    public void ResetUserScale() => UserScale = 1.0;

    public static ScaleContract FromSettings(ScaleSettings settings)
    {
        var c = new ScaleContract();
        c.SetUserScale(settings.UserScale > 0 ? settings.UserScale : 1.0);
        return c;
    }

    public ScaleSettings ToSettings() => new()
    {
        UserScale = UserScale
    };
}
