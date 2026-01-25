using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using ZenSkies.Common.Config;
using ZenSkies.Common.Systems.Compat;
using ZenSkies.Core;
using ZenSkies.Core.Particles;
using ZenSkies.Core.Rendering;

namespace ZenSkies.Common.Systems.Weather;

[Autoload(Side = ModSide.Client)]
public static partial class WindSystem
{
    private const float wind_threshold = 0.17f;
    private const float spawn_chance = 35f;
    private const int loop_chance = 10;

    private const int offscreen_margin = 100;

    public const int WIND_COUNT = 65;
    public static readonly ParticleHandler<WindParticle> Winds = new(WIND_COUNT);

    [OnLoad]
    private static void Load()
    {
        On_Main.DoUpdate += DoUpdate_Wind;

        IL_Main.UpdateAudio += UpdateAudio_MenuWind;
    }

    private static void DoUpdate_Wind(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        orig(self, ref gameTime);

        if (Main.dedServ ||
            Main.gamePaused && !Main.gameMenu ||
            HighFPSSupportCompat.IsPartialTick ||
            !SkyConfig.Instance.UseWindParticles ||
            SkyConfig.Instance.WindOpacity <= 0)
            return;

        Winds.Update();

        if (MathF.Abs(Main.WindForVisuals) < wind_threshold)
            return;

        SpawnWind();
    }

    private static void SpawnWind()
    {
        float spawnChance = spawn_chance / MathF.Abs(Main.WindForVisuals);

        if (!Main.rand.NextBool((int)spawnChance))
            return;

        Vector2 screensize = Utilities.ScreenSize;

        Rectangle spawn = new((int)(Main.screenPosition.X - screensize.X * Main.WindForVisuals * 0.5f), (int)Main.screenPosition.Y,
            (int)screensize.X, (int)screensize.Y);

        spawn.Inflate(offscreen_margin, offscreen_margin);

        Vector2 position = Main.rand.NextVector2FromRectangle(spawn);

        if (!Main.gameMenu && (position.Y > Main.worldSurface * 16f || Collision.SolidCollision(position, 1, 1)))
            return;

        Winds.Spawn(new(position, Main.WindForVisuals, Main.rand.NextBool(loop_chance)));
    }

    #region Ambience

    private static void UpdateAudio_MenuWind(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            i => i.MatchLdsfld<Main>(nameof(Main.gameMenu)),
            i => i.MatchBrfalse(out _),
            i => i.MatchLdcR4(0)
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchStelemR4(),
            i => i.MatchBr(out _),
            i => i.MatchLdsfld<Main>(nameof(Main.gameMenu))
        );

        c.EmitPop();
        c.EmitDelegate(UsingModdedMenu);

        static bool UsingModdedMenu()
        {
            ModMenu menu = MenuLoader.currentMenu;

            return
                menu is not MenutML &&
                menu is not MenuJourneysEnd &&
                menu is not MenuOldVanilla;
        }
    }

    #endregion
}

public record struct WindParticle : IParticle
{
    private const int max_old_positions = 43;

    private const float width = 3.5f;

    private const float lifetime_increment = 0.004f;

    private const float lifetime_multiplier = 7f;

    private const float wave_frequency = 0.6f;
    private const float wave_amplitude = 0.1f;

    private const float loop_range = 0.06f;

    private const float loop_max_offset = 0.3f;

    private const float velocity_magnitude = 13f;

    public Vector2 Position { get; set; }

    public Vector2[] OldPositions { get; init; }

    public Vector2 Velocity { get; set; }

    public float Wind { get; init; }

    public float LoopOffset { get; init; }

    public bool ShouldLoop { get; init; }

    public float Lifetime { get; set; }

    public bool IsActive { get; set; }

    public WindParticle(Vector2 position, float wind, bool shouldLoop)
    {
        Position = position;
        OldPositions = new Vector2[max_old_positions];
        Velocity = Vector2.Zero;
        Wind = wind;
        LoopOffset = Main.rand.NextFloat(-loop_max_offset, loop_max_offset);
        ShouldLoop = shouldLoop;
        Lifetime = 0f;
        IsActive = true;
    }

    void IParticle.Update()
    {
        float increment = lifetime_increment * MathF.Abs(Wind);

        Lifetime += increment;

        if (Lifetime > 1f)
        {
            IsActive = false;
        }

        float wave = MathF.Sin((Lifetime * lifetime_multiplier + Main.GlobalTimeWrappedHourly) * wave_frequency) * wave_amplitude;

        Vector2 newVelocity = new(Wind, wave);

        // Loop behavior, similar to vanilla paper airplanes
        if (ShouldLoop)
        {
            float range = loop_range / MathHelper.Clamp(MathF.Abs(Wind), 0.01f, 1);
            range *= 0.5f;

            float offset = 0.5f + LoopOffset;

            float interpolator = Utils.Remap(Lifetime, offset - range, offset + range, 0f, 1f);

            newVelocity = newVelocity.RotatedBy(MathHelper.TwoPi * interpolator * -MathF.Sign(Wind));
        }

        Velocity = newVelocity.SafeNormalize(Vector2.UnitY) * velocity_magnitude * MathF.Abs(Wind);

        Position += Velocity;

        for (int i = OldPositions.Length - 2; i >= 0; i--)
        {
            OldPositions[i + 1] = OldPositions[i];
        }

        OldPositions[0] = Position;
    }

    readonly void IParticle.Draw(SpriteBatch spriteBatch, GraphicsDevice device)
    {
        Vector3[] positions =
            OldPositions.Where(pos => pos != default)
            .Select(p => new Vector3(Vector2.Transform(p, spriteBatch.transformMatrix), 0))
            .ToArray();

        if (positions.Length <= 2)
            return;

        float brightness = MathF.Sin(Lifetime * MathHelper.Pi) * Main.atmo * MathF.Abs(Wind);

        float alpha = SkyConfig.Instance.WindOpacity;

        // Color based on the tile at the center of the trail
        Vector3 center = positions[positions.Length / 2];

        Point tilePosition = (new Vector2(center.X, center.Y) - Main.screenPosition).ToTileCoordinates();

        Color color = Lighting.GetColor(tilePosition).MultiplyRGB(Main.ColorOfTheSkies) * brightness * alpha;
        color.A = 0;

        // TODO: Use upcoming DAYBREAK rendering
        VertexPositionColorTexture[] vertices =
            TriangleStripBuilder.BuildPath(positions,
            t => MathF.Sin(t * MathHelper.Pi) * brightness * width,
            t => color,
            smoothingSubdivisions: 1);

        if (vertices.Length > 3)
        {
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
        }
    }
}
