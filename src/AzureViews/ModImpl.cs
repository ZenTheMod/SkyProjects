using Terraria.ModLoader;

namespace AzureViews;

public sealed class ModImpl : Mod
{
    public static bool CanDrawSky => !ModLoader.isLoading;
}
