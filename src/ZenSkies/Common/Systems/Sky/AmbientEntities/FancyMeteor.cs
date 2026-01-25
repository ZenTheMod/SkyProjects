using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Skies;
using Terraria.Utilities;
using ZenSkies.Common.Systems.Sky.Space;

namespace ZenSkies.Common.Systems.Sky;

public sealed class FancyMeteor(Player player, FastRandom random)
    : AmbientSky.MeteorSkyEntity(player, random)
{
    private static readonly Vector4 start_color = new(.28f, 0.2f, 1f, 1f);
    private static readonly Vector4 end_color = new(.9f, 0.2f, 0.1f, 1f);

    [OnLoad]
    private static void Load()
    {
        IL_AmbientSky.Spawn += Spawn_UseFancyMeteor;
    }

    private static void Spawn_UseFancyMeteor(ILContext il)
    {
        ILCursor c = new(il);

        int playerIndex = -1;
        int randomIndex = -1;

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdarg(out playerIndex),
            i => i.MatchLdloc(out randomIndex),
            i => i.MatchNewobj<AmbientSky.MeteorSkyEntity>()
        );

        c.EmitPop();

        c.EmitLdarg(playerIndex);
        c.EmitLdloc(randomIndex);

        c.EmitDelegate((Player player, FastRandom random) => new FancyMeteor(player, random));
    }

    public override void Draw(SpriteBatch spriteBatch, float depthScale, float minDepth, float maxDepth)
    {
        Depth = 5.5f;

        if (Depth <= minDepth ||
            Depth > maxDepth ||
            !SkyEffects.Meteor.IsReady)
            return;

        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix);

        float alpha = Utils.Remap(StarSystem.StarAlpha, 0f, 1f, 0.3f, 0.55f);

        SkyEffects.Meteor.StartColor = start_color * alpha;
        SkyEffects.Meteor.EndColor = end_color * alpha;

        SkyEffects.Meteor.Time = Main.GlobalTimeWrappedHourly * 0.3f;

        SkyEffects.Meteor.Scale = 5f;

        SkyEffects.Meteor.Apply();

        Texture2D noise = MiscTextures.LoopingNoise;

        Vector2 position = GetDrawPositionByDepth() - Main.Camera.UnscaledPosition;

        Vector2 origin = new Vector2(.085f, 0.5f) * noise.Size();

        Vector2 scale = new Vector2(2.5f, 0.18f) * (depthScale / Depth);

        spriteBatch.Draw(noise, position, null, Color.White, Rotation + MathHelper.PiOver2, origin, scale, Effects, 0f);

        spriteBatch.Restart(in snapshot);
    }
}
