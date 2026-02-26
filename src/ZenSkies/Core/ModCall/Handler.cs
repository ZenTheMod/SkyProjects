using Daybreak.Common.Features.Hooks;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;
using ZenSkies.Core.DataStructures;

namespace ZenSkies.Core;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class ModCallAttribute : Attribute
{
    public bool UsesDefaultName = true;

    public string[] NameAliases = [];

    public ModCallAttribute()
    { }

    public ModCallAttribute(params string[] nameAliases) : this(false, nameAliases)
    { }

    public ModCallAttribute(bool includeDefaultName, params string[] nameAliases)
    {
        UsesDefaultName = includeDefaultName;
        NameAliases = nameAliases;
    }
}

internal static class ModCallLoader
{
    private sealed class ModCallHandlers : AliasedList<string, MethodInfo>
    {
        public object? Invoke(string name, object?[]? args)
        {
            int matching = this[name].FindIndex(m => m.MatchesParameters(args));

            if (matching != -1)
            {
                return this[name][matching]?.Invoke(null, args);
            }

            throw new ArgumentException($"No suitable method matching {args} was found!");
        }
    }

    private static readonly ModCallHandlers handlers = [];

    [OnLoad]
    private static void Load()
    {
        Assembly assembly = ModContent.GetInstance<ModImpl>().Code;

        var methods = assembly.GetAllDecoratedMethods<ModCallAttribute>();

        foreach (MethodInfo method in methods)
        {
            ModCallAttribute? attribute = method.GetCustomAttribute<ModCallAttribute>();

            if (attribute is null)
            {
                continue;
            }

            string[] names;

            if (attribute.NameAliases.Length <= 0)
            {
                names = [method.Name];
            }
            else if (attribute.UsesDefaultName)
            {
                names = [method.Name, .. attribute.NameAliases];
            }
            else
            {
                names = attribute.NameAliases;
            }

            handlers.Add(names.ToHashSet(), method);
        }
    }

    public static object? HandleCall(string name, object?[]? arguments)
    {
        try
        {
            return handlers.Invoke(name, arguments);
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentException($"ModCall \"{name}\" does not match any method alias!");
        }
    }
}