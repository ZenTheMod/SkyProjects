using Daybreak.Common.Features.Hooks;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace ZenSkies.Core;

public static class NetMessageHooks
{
    [AttributeUsage(AttributeTargets.Method)]
    [HookMetadata(TypeContainingEvent = typeof(OnSyncWorldData), EventName = "Event", DelegateName = "Definition")]
    public sealed class OnSyncWorldDataAttribute : SubscribesToAttribute;

    public sealed class OnSyncWorldData
    {
        public delegate void Definition(int toClient, int ignoreClient);

        public static void Invoke(int toClient, int ignoreClient)
        {
            Event?.Invoke(toClient, ignoreClient);
        }

        public static event Definition? Event;
    }

    [OnLoad]
    private static void Load()
    {
        On_NetMessage.SendData += InvokeMethods;
    }

    private static void InvokeMethods(
        On_NetMessage.orig_SendData orig,
        int msgType,
        int remoteClient,
        int ignoreClient,
        NetworkText text,
        int number,
        float number2,
        float number3,
        float number4,
        int number5,
        int number6,
        int number7
    )
    {
        orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);

        if (msgType == MessageID.WorldData)
        {
            OnSyncWorldData.Invoke(remoteClient, ignoreClient);
        }
    }
}
