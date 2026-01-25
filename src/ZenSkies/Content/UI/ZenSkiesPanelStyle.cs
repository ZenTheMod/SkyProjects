using Daybreak.Common.Features.ModPanel;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using ZenSkies.Common.Config;
using ZenSkies.Common.Systems.Sky.Space;
using ZenSkies.Common.Systems.Weather;
using ZenSkies.Core.Particles;
using ZenSkies.Core.Utils;
using Star = ZenSkies.Common.Systems.Sky.Space.Star;

namespace ZenSkies.Content.UI;

public sealed class ZenSkiesPanelStyle : ModPanelStyleExt
{
    private record struct SakuraLeafParticle : IParticle
    {
        private const int frametime = 8;
        private const int frames = 4;

        private const float lifetime_increment = 0.003f;

        private const float wind_speed = 6.5f;

        public Vector2 Position { get; set; }

        public float Rotation { get; set; }

        public Vector2 Velocity { get; set; }

        public int Frametimer { get; set; }

        public int Frame { get; set; }

        public float Lifetime { get; set; }

        public bool IsActive { get; set; }

        public SakuraLeafParticle(Vector2 position, Vector2? velocity = null)
        {
            Position = position;
            Velocity = velocity ?? Vector2.Zero;
            Frametimer = 0;
            Frame = 0;
            Lifetime = 0f;
            IsActive = true;
        }

        void IParticle.Update()
        {
            Lifetime += lifetime_increment;

            if (Lifetime >= 1)
            {
                IsActive = false;
            }

            if (++Frametimer >= frametime)
            {
                Frametimer = 0;

                if (++Frame >= frames)
                {
                    Frame = 0;
                }
            }

            // Modified vanilla tree leaf logic
            Vector2 newVelocity = Velocity;
            Vector2 newPosition = Position;

            Vector2 vector = Position + new Vector2(12f) / 2f - new Vector2(4f) / 2f;

            vector.Y -= 4f;

            Vector2 vector2 = Position - vector;

            if (newVelocity.Y < 0f)
            {
                Vector2 vector3 = new(newVelocity.X, -.2f);

                newVelocity.Y = 0.1f;

                vector3.X *= 0.94f;

                newVelocity.X = vector3.X;
                newPosition.X += newVelocity.X;
                return;
            }

            newVelocity.Y += MathF.PI / 180f;

            Vector2 vector4 = Vector2.UnitY.RotatedBy(newVelocity.Y);

            vector4.X += wind_speed;

            newPosition += vector2;

            newPosition += vector4;

            float newRotation = vector4.ToRotation() + MathHelper.PiOver2;

            Velocity = newVelocity;
            Position = newPosition;
            Rotation = newRotation;
        }

        readonly void IParticle.Draw(SpriteBatch spriteBatch, GraphicsDevice device)
        {
            Texture2D texture = PanelStyleTextures.Leaf;

            Rectangle frame = texture.Frame(1, frames, 0, Frame);

            Vector2 origin = frame.Size() * 0.5f;

            spriteBatch.Draw(PanelStyleTextures.Leaf, Position, frame, Color.White, Rotation, origin, 1f, SpriteEffects.None, 0f);
        }
    }

    private enum BirdState : byte
    {
        None,
        Idle,
        Flying
    }

    private static readonly Color panel_outline = new(76, 76, 76, 76);
    private static readonly Color panel_hover_outline = new(100, 80, 90, 0);

    private static readonly Color background = new(78, 62, 130);
    private static readonly Color background_gradient = new(64, 48, 22, 0);

    private static readonly Vector2 branch_position = new(-14, 10);
    private static readonly Vector2 branch_origin = new(-12, 47);

    private const float branch_rotation_frequency = 2.1f;
    private const float branch_rotation_amplitude = 0.06f;

    private static readonly Color foreground_gradient = new(117, 81, 47, 0);

    // Leaves
    private const int leaf_count = 55;
    private static readonly ParticleHandler<SakuraLeafParticle> leaves = new(leaf_count);

    private const int leaf_spawn_chance = 30;

    private const float leaf_spawn_offset_min = -100f;
    private const float leaf_spawn_offset_max = -13f;

    // Leaves on hover
    private const int leaf_hover_wait = 135;
    private static int leafHoverTimer;

    private const int leaf_hover_count_min = 4;
    private const int leaf_hover_count_max = 12;

    private static readonly Vector2 leaf_hover_velocity = new(28, 0);

    // Wind
    private const int wind_count = 45;
    private static readonly ParticleHandler<WindParticle> winds = new(wind_count);

    private const int wind_spawn_chance = 55;

    private const float wind_spawn_offset_min = -1000f;
    private const float wind_spawn_offset_max = -400f;

    private const int star_count = 240;
    private static readonly Star[] stars = new Star[star_count];

    private static bool hasGeneratedStars = false;

    private const float star_rotation_speed = 0.00045f;
    private static float starRotation;

    // Bird
    private static BirdState birdState;

    private static readonly Vector2 bird_branch_offset = new(35, 44);
    private static readonly Vector2 bird_origin = new(15, 22);

    private static Vector2 birdPosition;

    private const float bird_velocity_multiplier = 1.07f;
    private const float bird_max_velocity_sqr = 12f * 12f;
    private static readonly Vector2 bird_fly_direction = new(.9f, -1.5f);
    private static Vector2 birdVelocity;

    private const int bird_frames = 5;
    private const int bird_flying_frames = 4;
    private const int bird_frametime = 6;
    private static int birdFrame;
    private static int birdFrameTimer;

    public override void Load()
    {
        MonoModHooks.Add(
            typeof(UIMods).GetMethod(nameof(UIMods.OnInitialize), BindingFlags.Instance | BindingFlags.Public),
            OnInitialize_Reorder
        );
    }

    private static void OnInitialize_Reorder(Action<UIMods> orig, UIMods self)
    {
        orig(self);

        // Move the mod list to the front to have it drawn after filtering buttons.
        self.uIPanel.RemoveChild(self.modList);
        self.uIPanel.Append(self.modList);
    }

    public override void PostInitialize(UIModItem element)
    {
        element.OnUpdate += Update;
        element.OnMouseOver += OnHover;

        ResetBird();

        hasGeneratedStars = false;
    }

    private static void ResetBird()
    {
        birdState = Main.rand.NextBool() ? BirdState.None : BirdState.Idle;
        birdVelocity = Vector2.Zero;
        birdFrame = bird_frames - 1;
        birdFrameTimer = 0;
    }

    public override bool PreSetHoverColors(UIModItem element, bool hovered)
    {
        // Use the default blue because it looks nicer.
        element.BackgroundColor = hovered ? UICommon.DefaultUIBlueMouseOver : UICommon.DefaultUIBlue;

        return false;
    }

    // Remove the panel behind the enable toggle.
    public override Dictionary<TextureKind, Asset<Texture2D>> TextureOverrides { get; } = new()
         {
             {TextureKind.InnerPanel, MiscTextures.Invis},
             {TextureKind.Deps, PanelStyleTextures.DepsButton}
         };

    public override UIImage? ModifyModIcon(UIModItem element, UIImage modIcon, ref int modIconAdjust) => null;

    private static void Update(UIElement element)
    {
        Vector2 size = element.Dimensions.Size();

        UpdateLeafs(size);
        UpdateWinds(size);

        UpdateStars(size);

        UpdateBird(element);
    }

    private static void UpdateLeafs(Vector2 size)
    {
        leaves.Update();

        if (leafHoverTimer > 0)
        {
            leafHoverTimer--;
        }

        if (!Main.rand.NextBool(leaf_spawn_chance))
        {
            return;
        }

        SpawnLeaf(size);
    }

    private static void SpawnLeaf(Vector2 size, Vector2? velocity = null)
    {
        Vector2 position =
            new(Main.rand.NextFloat(leaf_spawn_offset_min, leaf_spawn_offset_max),
            Main.rand.NextFloat(-size.Y * 0.3f, size.Y));

        leaves.Spawn(new(position, velocity));
    }

    private static void UpdateWinds(Vector2 size)
    {
        winds.Update();

        if (!Main.rand.NextBool(wind_spawn_chance))
        {
            return;
        }

        Vector2 position =
            new(Main.rand.NextFloat(wind_spawn_offset_min, wind_spawn_offset_max),
            Main.rand.NextFloat(-size.Y * 0.1f, size.Y * 1.1f));

        winds.Spawn(new(position, 0.6f, false));
    }

    private static void UpdateStars(Vector2 size)
    {
        starRotation += star_rotation_speed;
        starRotation %= MathHelper.TwoPi;

        if (hasGeneratedStars)
        {
            return;
        }

        hasGeneratedStars = true;

        Vector2 center = new(size.X * 0.5f, size.Y);

        float radius = center.Length() * Main.UIScale;

        for (int i = 0; i < star_count; i++)
        {
            stars[i] = new(Main.rand, radius);
        }
    }

    private static void UpdateBird(UIElement element)
    {
        birdState = birdState switch
        {
            BirdState.Idle => UpdateIdle(element),
            BirdState.Flying => UpdatingFlying(),
            _ => BirdState.None
        };

        static BirdState UpdateIdle(UIElement element)
        {
            Vector2 position = element.Dimensions.Position() * Main.UIScale;
            Vector2 size = element.Dimensions.Size() * Main.UIScale;

            Vector2 branchPosition =
                position +
                branch_position +
                (Vector2.UnitY * size.Y * 0.5f);

            float branchRotation = MathF.Sin(Main.GlobalTimeWrappedHourly * branch_rotation_frequency) * branch_rotation_amplitude;

            birdPosition = bird_branch_offset - branch_origin;
            birdPosition = birdPosition.RotatedBy(branchRotation);

            birdPosition += branchPosition;

            // Only allow transitioning to flying if the bird is not cutoff.
            if (!BirdOnScreen(element) ||
                !element.IsMouseHovering)
            {
                return BirdState.Idle;
            }

            birdFrame = 0;

            birdVelocity = bird_fly_direction;

            return BirdState.Flying;
        }

        static BirdState UpdatingFlying()
        {
            if (++birdFrameTimer >= bird_frametime)
            {
                birdFrameTimer = 0;

                if (++birdFrame >= bird_flying_frames)
                {
                    birdFrame = 0;
                }
            }

            if (birdVelocity.LengthSquared() <= bird_max_velocity_sqr)
            {
                birdVelocity *= bird_velocity_multiplier;
            }

            birdPosition += birdVelocity;

            if (birdPosition.Y <= 0)
            {
                birdState = BirdState.None;
            }

            return BirdState.Flying;
        }

        static bool BirdOnScreen(UIElement element)
        {
            Texture2D texture = PanelStyleTextures.Bird;

            Rectangle rectangle = texture.Frame(1, bird_frames, 0, 0);

            Vector2 position = birdPosition - bird_origin;
            rectangle.X += (int)position.X;
            rectangle.Y += (int)position.Y;

            UIElement? innerList = element.Parent?.Parent;

            if (innerList is null)
            {
                return false;
            }

            Rectangle parentRectangle = innerList.ParentRelativeDimensions.Multiply(Main.UIScale);

            return parentRectangle.Contains(rectangle);
        }
    }

    private static void OnHover(UIMouseEvent evt, UIElement element)
    {
        Vector2 size = element.Dimensions.Size();

        SpawnLeavesHover(size);
    }

    private static void SpawnLeavesHover(Vector2 size)
    {
        if (leafHoverTimer > 0)
        {
            return;
        }

        leafHoverTimer = leaf_hover_wait;

        int count = Main.rand.Next(leaf_hover_count_min, leaf_hover_count_max);

        for (int i = 0; i < count; i++)
        {
            SpawnLeaf(size, leaf_hover_velocity);
        }
    }

    public override bool PreDrawPanel(UIModItem element, SpriteBatch spriteBatch, ref bool drawDivider)
    {
        if (element._needsTextureLoading)
        {
            element._needsTextureLoading = false;
            element.LoadTextures();
        }

        GraphicsDevice device = Main.instance.GraphicsDevice;

        Rectangle dims = element.Dimensions;

        Vector2 size = Vector2.Transform(dims.Size(), Main.UIScaleMatrix);
        Vector2 position = Vector2.Transform(dims.Position(), Main.UIScaleMatrix);

        Rectangle source = new((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);

        var snapshot = new SpriteBatchSnapshot(spriteBatch);
        using (spriteBatch.Scope())
        {
            RenderTargetLease nextLease = RenderTargetPool.Shared.Rent(device, (int)size.X, (int)size.Y, RenderTargetDescriptor.Default);

            // Background
            {
                using (nextLease.Scope(clearColor: Color.Transparent))
                {
                    DrawPanelBackground(spriteBatch, size);
                }
                DrawAsPanel(nextLease.Target);
            }

            // that fucking bird that i hate
            DrawBird(spriteBatch, snapshot, device, element);

            // Foreground
            {
                using (new RenderTargetScope(nextLease.Target, true, Color.Transparent))
                {
                    DrawPanelForeground(spriteBatch, device, size);
                }

                DrawAsPanel(nextLease.Target, Color.Transparent);
            }

            nextLease.Dispose();
        }

        // Border
        {
            Color borderColor =
                element.IsMouseHovering ?
                panel_hover_outline :
                panel_outline;

            element.DrawPanel(spriteBatch, element._borderTexture.Value, borderColor);
        }

        // Divider
        {
            drawDivider = false;

            Rectangle innerDimensions = element.InnerDimensions;

            Rectangle dividerSize = new(
                innerDimensions.X + 5 + element._modIconAdjust, innerDimensions.Y + 30,
                innerDimensions.Width - 10 - element._modIconAdjust, 4);

            spriteBatch.Draw(PanelStyleTextures.Divider, dividerSize, Color.White);
        }

        return false;

        void DrawAsPanel(Texture2D texture, Color? color = null)
        {
            spriteBatch.Begin(snapshot with { SortMode = SpriteSortMode.Immediate });

            UIEffects.Panel.Source = new(source.Width, source.Height, source.X, source.Y);

            UIEffects.Panel.Apply();

            device.Textures[1] = texture;
            device.SamplerStates[1] = SamplerState.PointClamp;

            element.DrawPanel(spriteBatch, element._backgroundTexture.Value, color ?? element.BackgroundColor);
            element.DrawPanel(spriteBatch, element._borderTexture.Value, color ?? element.BorderColor);

            spriteBatch.End();
        }
    }

    private static void DrawPanelBackground(SpriteBatch spriteBatch, Vector2 size)
    {
        // Background gradient
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        {
            Rectangle background = new(0, 0, (int)size.X, (int)size.Y);

            spriteBatch.Draw(MiscTextures.Pixel, background, ZenSkiesPanelStyle.background);
            spriteBatch.Draw(SkyTextures.SkyGradient, background, background_gradient);
        }
        spriteBatch.End(out var snapshot);

        // Stars
        spriteBatch.Begin(snapshot with { TransformMatrix = StarMatrix(size) });
        {
            StarRendering.DrawStars(spriteBatch, 0.2f, -starRotation, stars, SkyConfig.Instance.StarStyle);
        }
        spriteBatch.End();

        // Branch
        spriteBatch.Begin(snapshot with { SamplerState = SamplerState.PointClamp });
        {
            Vector2 branchPosition = branch_position + (Vector2.UnitY * size.Y * 0.5f);

            float branchRotation = MathF.Sin(Main.GlobalTimeWrappedHourly * branch_rotation_frequency) * branch_rotation_amplitude;

            spriteBatch.Draw(PanelStyleTextures.Branch, branchPosition, null, Color.White, branchRotation, branch_origin, 1f, SpriteEffects.None, 0f);
        }
        spriteBatch.End();

        static Matrix StarMatrix(Vector2 size)
        {
            Matrix rotation = Matrix.CreateRotationZ(starRotation);
            Matrix offset = Matrix.CreateTranslation(new(size.X * 0.5f, size.Y, 0f));

            return Matrix.Identity * rotation * offset;
        }
    }

    private static void DrawPanelForeground(SpriteBatch spriteBatch, GraphicsDevice device, Vector2 size)
    {
        // Leaves
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        {
            leaves.Draw(spriteBatch, device);
        }
        spriteBatch.End(out var snapshot);

        // Wind
        Color oldSkyColor = Main.ColorOfTheSkies;
        Main.ColorOfTheSkies = Color.White;
        {
            device.Textures[0] = SkyTextures.SunBloom;
            device.SamplerStates[0] = SamplerState.LinearClamp;

            winds.Draw(spriteBatch, device);
        }
        Main.ColorOfTheSkies = oldSkyColor;

        // Foreground light
        spriteBatch.Begin(snapshot with { SamplerState = SamplerState.LinearClamp });
        {
            Rectangle background = new(0, 0, (int)size.X, (int)size.Y);

            spriteBatch.Draw(SkyTextures.SkyGradient, background, foreground_gradient);
        }
        spriteBatch.End();
    }

    private static void DrawBird(SpriteBatch spriteBatch, SpriteBatchSnapshot snapshot, GraphicsDevice device, UIPanel panel)
    {
        if (birdState == BirdState.None)
        {
            return;
        }

        Rectangle scissor = device.ScissorRectangle;

        if (birdState == BirdState.Flying)
        {
            device.ScissorRectangle = device.Viewport.Bounds;
        }

        spriteBatch.Begin(snapshot with
        {
            BlendState = BlendState.AlphaBlend,
            SamplerState = SamplerState.PointClamp,
            TransformMatrix = Matrix.Identity
        });
        {
            Texture2D texture = PanelStyleTextures.Bird;

            Rectangle frame = texture.Frame(1, bird_frames, 0, birdFrame);

            spriteBatch.Draw(texture, birdPosition, frame, Color.White, 0f, bird_origin, 1f, SpriteEffects.None, 0f);
        }
        spriteBatch.End();

        device.ScissorRectangle = scissor;
    }
}
