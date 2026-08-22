namespace MosaicShell.Core.Scale;

/// <summary>
/// Host-agnostic scale contract: UiScale = DpiScale × UserScale.
/// Design space is logical pixels at 96 DPI / 100%.
/// </summary>
public sealed class ScaleContract
{
    public double DpiScale { get; private set; } = 1.0;
    public double UserScale { get; private set; } = 1.0;

    public double UiScale => Math.Round(DpiScale * UserScale, 4);

    public void SetDpiScale(double dpiScale)
    {
        if (dpiScale <= 0) throw new ArgumentOutOfRangeException(nameof(dpiScale));
        DpiScale = Math.Round(dpiScale, 4);
    }

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
        c.SetDpiScale(settings.DpiScale > 0 ? settings.DpiScale : DpiProbe.GetDpiScale());
        c.SetUserScale(settings.UserScale > 0 ? settings.UserScale : 1.0);
        return c;
    }

    public ScaleSettings ToSettings() => new()
    {
        DpiScale = DpiScale,
        UserScale = UserScale
    };
}
