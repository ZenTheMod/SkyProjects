using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Linq;
using Terraria.ModLoader;
using ZenSkies.Core;

namespace ZenSkies.Common.Systems.Sky;

[Autoload(Side = ModSide.Client)]
public static partial class SunAndMoon
{
    #region Sun

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(PreDrawSun), EventName = "Event", DelegateName = "Definition")]
    public sealed class PreDrawSunAttribute : SubscribesToAttribute;

    public sealed class PreDrawSun
    {
        public delegate bool Definition(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            ref Vector2 position,
            ref Color color,
            ref float rotation,
            ref float scale
        );

        [ModCall(nameof(PreDrawSun))]
        public static bool Invoke(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            ref Vector2 position,
            ref Color color,
            ref float rotation,
            ref float scale
        )
        {
            bool ret = true;

            if (Event is null)
            {
                return ret;
            }

            foreach (Definition handler in Event.GetInvocationList().Select(h => (Definition)h))
            {
                ret &= handler(
                    spriteBatch,
                    device,
                    ref position,
                    ref color,
                    ref rotation,
                    ref scale
                );
            }

            return ret;
        }

        [ModCall($"add_{nameof(PreDrawSun)}", $"Add{nameof(PreDrawSun)}")]
        private static void Add(Definition action) => Event += action;

        public static event Definition? Event;
    }

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(PostDrawSun), EventName = "Event", DelegateName = "Definition")]
    public sealed class PostDrawSunAttribute : SubscribesToAttribute;

    public sealed class PostDrawSun
    {
        public delegate void Definition(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            Vector2 position,
            Color color,
            float rotation,
            float scale
        );

        [ModCall(nameof(PostDrawSun))]
        public static void Invoke(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            Vector2 position,
            Color color,
            float rotation,
            float scale
        )
        {
            Event?.Invoke(
                spriteBatch,
                device,
                position,
                color,
                rotation,
                scale
            );
        }

        [ModCall($"add_{nameof(PostDrawSun)}", $"Add{nameof(PostDrawSun)}")]
        private static void Add(Definition action) => Event += action;

        public static event Definition? Event;
    }

    #endregion

    #region Moon

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(EventMoonActive), EventName = "Event", DelegateName = "Definition")]
    public sealed class EventMoonActiveAttribute : SubscribesToAttribute;

    public sealed class EventMoonActive
    {
        public delegate bool Definition();

        [ModCall(nameof(EventMoonActive))]
        public static bool Invoke()
        {
            bool ret = true;

            if (Event is null)
            {
                return ret;
            }

            foreach (Definition handler in Event.GetInvocationList().Select(h => (Definition)h))
            {
                ret |= handler();
            }

            return ret;
        }

        [ModCall($"add_{nameof(EventMoonActive)}", $"Add{nameof(EventMoonActive)}")]
        private static void Add(Definition action) => Event += action;

        public static event Definition? Event;
    }

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(ModifyMoonTexture), EventName = "Event", DelegateName = "Definition")]
    public sealed class ModifyMoonTextureAttribute : SubscribesToAttribute;

    public sealed class ModifyMoonTexture
    {
        public delegate void Definition(
            ref Asset<Texture2D> moon,
            bool eventMoon
        );

        [ModCall(nameof(ModifyMoonTexture))]
        public static void Invoke(
            ref Asset<Texture2D> moon,
            bool eventMoon
        )
        {
            Event?.Invoke(
                ref moon,
                eventMoon
            );
        }

        [ModCall($"add_{nameof(ModifyMoonTexture)}", $"Add{nameof(ModifyMoonTexture)}")]
        private static void Add(Definition action) => Event += action;

        public static event Definition? Event;
    }

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(PreDrawMoon), EventName = "Event", DelegateName = "Definition")]
    public sealed class PreDrawMoonAttribute : SubscribesToAttribute;

    public sealed class PreDrawMoon
    {
        public delegate bool Definition(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            ref Asset<Texture2D> moon,
            ref Vector2 position,
            ref Color color,
            ref float rotation,
            ref float scale,
            ref Color moonColor,
            ref Color shadowColor,
            ref bool drawExtras,
            bool eventMoon
        );

        [ModCall(nameof(PreDrawMoon))]
        public static bool Invoke(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            ref Asset<Texture2D> moon,
            ref Vector2 position,
            ref Color color,
            ref float rotation,
            ref float scale,
            ref Color moonColor,
            ref Color shadowColor,
            ref bool drawExtras,
            bool eventMoon
        )
        {
            bool ret = true;

            if (Event is null)
            {
                return ret;
            }

            foreach (Definition handler in Event.GetInvocationList().Select(h => (Definition)h))
            {
                ret &= handler(
                    spriteBatch,
                    device,
                    ref moon,
                    ref position,
                    ref color,
                    ref rotation,
                    ref scale,
                    ref moonColor,
                    ref shadowColor,
                    ref drawExtras,
                    eventMoon
                );
            }

            return ret;
        }

        [ModCall($"add_{nameof(PreDrawMoon)}", $"Add{nameof(PreDrawMoon)}")]
        private static void Add(Definition action) => Event += action;

        public static event Definition? Event;
    }

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(PostDrawMoon), EventName = "Event", DelegateName = "Definition")]
    public sealed class PostDrawMoonAttribute : SubscribesToAttribute;

    public sealed class PostDrawMoon
    {
        public delegate void Definition(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            Asset<Texture2D> moon,
            Vector2 position,
            Color color,
            float rotation,
            float scale,
            Color moonColor,
            Color shadowColor,
            bool drawExtras,
            bool eventMoon
        );

        [ModCall(nameof(PostDrawMoon))]
        public static void Invoke(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            Asset<Texture2D> moon,
            Vector2 position,
            Color color,
            float rotation,
            float scale,
            Color moonColor,
            Color shadowColor,
            bool drawExtras,
            bool eventMoon
        )
        {
            Event?.Invoke(
                spriteBatch,
                device,
                moon,
                position,
                color,
                rotation,
                scale,
                moonColor,
                shadowColor,
                drawExtras,
                eventMoon
            );
        }

        [ModCall($"add_{nameof(PostDrawMoon)}", $"Add{nameof(PostDrawMoon)}")]
        private static void Add(Definition action) => Event += action;

        public static event Definition? Event;
    }

    // 'Extras' should be used for details in the moon style (e.g. Moon2's rings), which will run under pre/post draw
    #region Extras

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(PreDrawMoonExtras), EventName = "Event", DelegateName = "Definition")]
    public sealed class PreDrawMoonExtrasAttribute : SubscribesToAttribute;

    public sealed class PreDrawMoonExtras
    {
        public delegate bool Definition(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            ref Asset<Texture2D> moon,
            ref Vector2 position,
            ref Color color,
            ref float rotation,
            ref float scale,
            ref Color moonColor,
            ref Color shadowColor,
            bool eventMoon
        );

        [ModCall(nameof(PreDrawMoonExtras))]
        public static bool Invoke(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            ref Asset<Texture2D> moon,
            ref Vector2 position,
            ref Color color,
            ref float rotation,
            ref float scale,
            ref Color moonColor,
            ref Color shadowColor,
            bool eventMoon
        )
        {
            bool ret = true;

            if (Event is null)
            {
                return ret;
            }

            foreach (Definition handler in Event.GetInvocationList().Select(h => (Definition)h))
            {
                ret &= handler(
                    spriteBatch,
                    device,
                    ref moon,
                    ref position,
                    ref color,
                    ref rotation,
                    ref scale,
                    ref moonColor,
                    ref shadowColor,
                    eventMoon
                );
            }

            return ret;
        }

        [ModCall($"add_{nameof(PreDrawMoonExtras)}", $"Add{nameof(PreDrawMoonExtras)}")]
        private static void Add(Definition action) => Event += action;

        public static event Definition? Event;
    }

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(PostDrawMoonExtras), EventName = "Event", DelegateName = "Definition")]
    public sealed class PostDrawMoonExtrasAttribute : SubscribesToAttribute;

    public sealed class PostDrawMoonExtras
    {
        public delegate void Definition(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            Asset<Texture2D> moon,
            Vector2 position,
            Color color,
            float rotation,
            float scale,
            Color moonColor,
            Color shadowColor,
            bool eventMoon
        );

        [ModCall(nameof(PostDrawMoonExtras))]
        public static void Invoke(
            SpriteBatch spriteBatch,
            GraphicsDevice device,
            Asset<Texture2D> moon,
            Vector2 position,
            Color color,
            float rotation,
            float scale,
            Color moonColor,
            Color shadowColor,
            bool eventMoon
        )
        {
            Event?.Invoke(
                spriteBatch,
                device,
                moon,
                position,
                color,
                rotation,
                scale,
                moonColor,
                shadowColor,
                eventMoon
            );
        }

        [ModCall($"add_{nameof(PostDrawMoonExtras)}", $"Add{nameof(PostDrawMoonExtras)}")]
        private static void Add(Definition action) => Event += action;

        public static event Definition? Event;
    }

    #endregion

    #endregion

    #region Sun and moon

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(PreDrawSunAndMoon), EventName = "Event", DelegateName = "Definition")]
    public sealed class PreDrawSunAndMoonAttribute : SubscribesToAttribute;

    public sealed class PreDrawSunAndMoon
    {
        public delegate bool Definition(
            SpriteBatch spriteBatch,
            in SpriteBatchSnapshot snapshot
        );

        public static bool Invoke(
            SpriteBatch spriteBatch,
            in SpriteBatchSnapshot snapshot
        )
        {
            bool ret = true;

            if (Event is null)
            {
                return ret;
            }

            foreach (Definition handler in Event.GetInvocationList().Select(h => (Definition)h))
            {
                ret &= handler(
                    spriteBatch,
                    in snapshot
                );
            }

            return ret;
        }

        public static event Definition? Event;
    }

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(PostDrawSunAndMoon), EventName = "Event", DelegateName = "Definition")]
    public sealed class PostDrawSunAndMoonAttribute : SubscribesToAttribute;

    public sealed class PostDrawSunAndMoon
    {
        public delegate void Definition(
            SpriteBatch spriteBatch,
            in SpriteBatchSnapshot snapshot
        );

        public static void Invoke(
            SpriteBatch spriteBatch,
            in SpriteBatchSnapshot snapshot
        )
        {
            Event?.Invoke(
                spriteBatch,
                in snapshot
            );
        }

        public static event Definition? Event;
    }

    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(UpdateSunAndMoonInfo), EventName = "Event", DelegateName = "Definition")]
    public sealed class UpdateSunAndMoonInfoAttribute : SubscribesToAttribute;

    public sealed class UpdateSunAndMoonInfo
    {
        public delegate void Definition(SunAndMoonInfo info);

        public static void Invoke(SunAndMoonInfo info)
        {
            Event?.Invoke(info);
        }

        public static event Definition? Event;
    }

    #endregion
}
