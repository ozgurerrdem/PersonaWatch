using System.ComponentModel;

namespace PersonaWatch.Infrastructure.Providers.Reporter;

public enum Platforms
{
    [Description("Instagram")]
    Instagram,
    [Description("Tiktok")]
    Tiktok,
    [Description("Facebook")]
    Facebook,
    [Description("X")]
    X
}