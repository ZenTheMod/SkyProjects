using Daybreak.Common.Features.Hooks;
using System;
using System.Diagnostics;
using System.Reflection;
using Terraria.ModLoader;
using ZenSkies.Common.Systems.Sky;
using ZenSkies.Core.Utils;

namespace ZenSkies.Common.Systems.Compat;

[ExtendsFromMod("DarkSurface")]
[Autoload(Side = ModSide.Client)]
public static class DarkSurfaceCompat
{
    public static bool IsEnabled { get; private set; }

    [ModSystemHooks.PostSetupContent]
    private static void PostSetupContent()
    {
        if (!ModLoader.TryGetMod("DarkSurface", out Mod darkSurface))
        {
            return;
        }

        IsEnabled = true;

        Assembly darkAsm = darkSurface.Code;

        Type? darkSurfaceSystem = darkAsm.GetType("DarkSurface.DarkSurfaceSystem");

        Debug.Assert(darkSurfaceSystem is not null);

        ModSystem system = (ModSystem)Utilities.GetInstance(darkSurfaceSystem);

        SkyColoration.ModifyInMenu += system.ModifySunLightColor;
    }
}
