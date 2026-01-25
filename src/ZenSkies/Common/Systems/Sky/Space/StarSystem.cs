using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using ZenSkies.Core;
using ZenSkies.Core.ModCall;
using ZenSkies.Core.Net;
using ZenSkies.Core.Utils;
using static ZenSkies.Common.Systems.Sky.Space.StarHooks;
using static ZenSkies.Core.Net.NetMessageHooks;

namespace ZenSkies.Common.Systems.Sky.Space;

public sealed class StarSystem : ModSystem, IPacketHandler
{
    #region Private Fields

    private const float CircularRadius = 1200f;

    private const float DawnTime = 6700f;
    private const float DuskStartTime = 48000f;
    private const float DayLength = 54000f;
    private const float MainMenuDayRateDivisor = 10000f;
    private const float GameDayRateDivisor = 70000f;
    private const float GraveyardAlphaMultiplier = 1.4f;
    private const float AtmosphereMultiplier = 0.43f;

    private const int DefaultStarGenerationSeed = 100;

    private const string InactiveCountKey = "InactiveCount";

    #endregion

    #region Public Fields

    public const int StarCount = 1200;
    public static readonly Star[] Stars = new Star[StarCount];

    public static readonly Dictionary<int, IStarModifier> StarModifiers =[];

    #endregion

    #region Public Properties

    public static float StarRotation
    {
        [ModCall(nameof(StarRotation), $"Get{nameof(StarRotation)}")]
        get; 
        private set; 
    }

    public static float StarAlpha
    {
        [ModCall(nameof(StarAlpha), $"Get{nameof(StarAlpha)}")]
        get
        {
            float alpha;

            if (Main.dayTime)
            {
                if (Main.time < DawnTime)
                    alpha = (float)(1f - Main.time / DawnTime);
                else if (Main.time > DuskStartTime)
                    alpha = (float)((Main.time - DuskStartTime) / (DayLength - DuskStartTime));
                else
                    alpha = 0f;
            }
            else
                alpha = 1f;

            if (Main.gameMenu)
                Main.shimmerAlpha = 0f;

            alpha += Main.shimmerAlpha;

            if (Main.GraveyardVisualIntensity > 0f)
                alpha *= 1f - Main.GraveyardVisualIntensity * GraveyardAlphaMultiplier;

            float atmosphericBoost = Easings.InCubic(1f - Main.atmo) * AtmosphereMultiplier;

            alpha = Utilities.Saturate(Easings.InCubic(alpha + atmosphericBoost));

            InvokeModifyStarAlpha(ref alpha);

            return alpha;
        }
    }

    #endregion

    #region Loading

    public override void Load()
    {
        GenerateStars();

        On_Main.DoUpdate += UpdateStars;
        On_Star.UpdateStars += DisableVanillaStars;

        OnSyncWorldData += WorldDataStars;
    }

    public override void Unload()
    {
        On_Main.DoUpdate -= UpdateStars;
        On_Star.UpdateStars -= DisableVanillaStars;

        StarHooks.Clear();
    }

    #endregion

    #region Updating

    private void UpdateStars(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        orig(self, ref gameTime);

        float dayRateDivisor = Main.gameMenu ? MainMenuDayRateDivisor : GameDayRateDivisor;

        StarRotation += (float)(Main.dayRate / dayRateDivisor);

        StarRotation %= MathHelper.TwoPi;

        foreach (KeyValuePair<int, IStarModifier> pair in StarModifiers)
        {
            if (Stars[pair.Key].IsActive &&
                pair.Value.IsActive)
                pair.Value.Update(ref Stars[pair.Key]);
            else
                StarModifiers.Remove(pair.Key);
        }

        InvokeUpdateStars();
    }

    private void DisableVanillaStars(On_Star.orig_UpdateStars orig)
    {
        if (ModImpl.CanDrawSky)
            return;

        orig();
    }

    #endregion

    #region Saving and Syncing

    public override void OnWorldLoad() =>
        GenerateStars(Main.worldID);

    public override void OnWorldUnload() =>
        GenerateStars();

    public override void ClearWorld() =>
        GenerateStars();

    #region World Data

    public override void SaveWorldData(TagCompound tag)
    {
        tag[nameof(StarRotation)] = StarRotation;

        int count = Stars.Count(s => !s.IsActive);
        tag[InactiveCountKey] = count;

        int index = 0;

        ReadOnlySpan<Star> starSpan = Stars;

        for (int i = 0; i < starSpan.Length; i++)
        {
            Star star = starSpan[i];

            if (star.IsActive)
                continue;

            tag[nameof(Stars) + index] = i;

            index++;
        }
    }

    public override void LoadWorldData(TagCompound tag)
    {
        try
        {
            StarRotation = tag.Get<float>(nameof(StarRotation));

            int count = tag.Get<int>(InactiveCountKey);

            for (int i = 0; i < count; i++)
            {
                int index = tag.Get<int>(nameof(Stars) + i);

                Stars[index].IsActive = false;
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Error($"Failed to load stars: {ex.Message}");
        }
    }

    #endregion

    #region Net Syncing

    private void WorldDataStars(int toClient, int ignoreClient) =>
        PacketSystem.Send<StarSystem>(toClient, ignoreClient);

    void IPacketHandler.Write(BinaryWriter writer)
    {
        writer.Write(StarRotation);

        int count = Stars.Count(s => !s.IsActive);
        writer.Write7BitEncodedInt(count);

        ReadOnlySpan<Star> starSpan = Stars;

        for (int i = 0; i < starSpan.Length; i++)
        {
            Star star = starSpan[i];

            if (star.IsActive)
                continue;

            writer.Write7BitEncodedInt(i);
        }
    }

    void IPacketHandler.Receive(BinaryReader reader)
    {
        try
        {
            StarRotation = reader.ReadSingle();

            int count = reader.Read7BitEncodedInt();

            for (int i = 0; i < count; i++)
            {
                int index = reader.Read7BitEncodedInt();

                Stars[index].IsActive = false;
            }
        }
        catch (Exception ex)
        {
            Main.NewText($"Failed to sync stars: {ex.Message}", Color.Red);
            Mod.Logger.Error($"Failed to sync stars: {ex.Message}");
        }
    }

    #endregion

    #endregion

    #region Public Methods

    [ModCall("RegenStars", "RegenerateStars")]
    public static void GenerateStars(int seed = DefaultStarGenerationSeed)
    {
        StarModifiers.Clear();

        UnifiedRandom rand = new(seed);

        StarRotation = 0f;

        for (int i = 0; i < StarCount; i++)
            Stars[i] = new(rand, CircularRadius);

        InvokeGenerateStars(rand, seed);
    }

    #region Modifiers

    public static void AddStarModifier<T>(Func<Star, T> modifier, int index = -1) where T : class, IStarModifier
    {
        if (index == -1)
            index = Main.rand.Next(StarCount);

        if (!Stars[index].IsActive ||
            StarModifiers.ContainsKey(index))
            return;

        StarModifiers.Add(index, modifier(Stars[index]));
    }

    public static int StarModifiersCount<T>() where T : class, IStarModifier =>
        StarModifiers.Count(m => m.Value.IsActive && m.Value is T);

    public static void ForStarModifiers<T>(Action<int, T> action) where T : class, IStarModifier
    {
        foreach (KeyValuePair<int, IStarModifier> pair in StarModifiers)
        {
            if (pair.Value is T modifier)
                action(pair.Key, modifier);
        }
    }

    public static void DrawStarModifiers<T>(SpriteBatch spriteBatch, GraphicsDevice device, float alpha, float rotation) where T : class, IStarModifier
    {
        foreach (KeyValuePair<int, IStarModifier> pair in StarModifiers)
        {
            if (Stars[pair.Key].IsActive &&
                pair.Value.IsActive)
                pair.Value.Draw(spriteBatch, device, ref Stars[pair.Key], alpha, rotation);
            else
                StarModifiers.Remove(pair.Key);
        }
    }

    public static void RemoveStarModifiers<T>() where T : class, IStarModifier
    {
        foreach (KeyValuePair<int, IStarModifier> pair in StarModifiers)
        {
            if (pair.Value is T)
                StarModifiers.Remove(pair.Key);
        }
    }

    #endregion

    #endregion
}
