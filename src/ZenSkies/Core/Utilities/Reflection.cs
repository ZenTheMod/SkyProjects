using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using static System.Reflection.BindingFlags;

namespace ZenSkies.Core;

public static partial class Utilities
{
    /// <summary>
    /// Checks if <paramref name="methodInfo"/>'s arguments matches the types of <paramref name="arguments"/>.
    /// </summary>
    public static bool MatchesParameters(this MethodInfo methodInfo, object?[]? arguments)
    {
        ParameterInfo[] parameters = methodInfo.GetParameters();

        if (parameters.Length != (arguments?.Length ?? 0))
        {
            return false;
        }

        if (parameters.Length <= 0)
        {
            return true;
        }

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType != arguments?[i]?.GetType())
            {
                return false;
            }
        }

        return true;
    }

    /// <returns>All types in <paramref name="assembly"/> with the attribute <typeparamref name="T"/>.</returns>
    public static IEnumerable<Type> GetAllDecoratedTypes<T>(
        this Assembly assembly,
        bool inherit = true
    ) where T : Attribute
    {
        return
            assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<T>(inherit) is not null);
    }

    /// <returns>All methods in <paramref name="assembly"/> with the attribute <typeparamref name="T"/>.</returns>
    public static IEnumerable<MethodInfo> GetAllDecoratedMethods<T>(
        this Assembly assembly,
        BindingFlags flags = Public | NonPublic | Static,
        bool inherit = true
    ) where T : Attribute
    {
        return
            assembly.GetTypes()
            .SelectMany(t => t.GetMethods(flags))
            .Where(m => m.GetCustomAttribute<T>(inherit) is not null &&
                !m.IsGenericMethod);
    }

    /// <returns>All events in <paramref name="assembly"/> with the attribute <typeparamref name="T"/>.</returns>
    public static IEnumerable<EventInfo> GetAllDecoratedEvents<T>(
        this Assembly assembly,
        BindingFlags flags = Public | NonPublic | Static,
        bool inherit = true
    ) where T : Attribute
    {
        return
            assembly.GetTypes()
            .SelectMany(t => t.GetEvents(flags))
            .Where(e => e.GetCustomAttribute<T>(inherit) is not null);
    }

    /// <inheritdoc cref="ModContent.GetInstance"/>
    public static object GetInstance(Type type) => ContentInstance.contentByType[type].instance;

    /// <inheritdoc cref="ModContent.GetInstance"/>
    public static bool TryGetInstance(
        Type type,
        [NotNullWhen(true)] out object? obj
    )
    {
        obj = null;

        if (ContentInstance.contentByType.TryGetValue(type, out ContentInstance.ContentEntry? entry) &&
            entry is not null)
        {
            obj = entry.instance;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets, or creates the singleton instance of all classes that inherit from <typeparamref name="T"/>.
    /// </summary>
    public static IEnumerable<T> GetAllInstancesOf<T>(Assembly assembly) where T : class
    {
        return 
            assembly.GetTypes()
            .Where(
                p => p.IsAssignableTo(typeof(T)) &&
                p.IsClass &&
                !p.IsAbstract &&
                p != typeof(T)
            ).Select(
                t =>
                {
                    if (TryGetInstance(t, out object? instance))
                    {
                        return (T)instance;
                    }
                    else
                    {
                        return (T)Activator.CreateInstance(t)!;
                    }
                }
            );
    }

    extension(PropertyFieldWrapper member)
    {
        public bool IsStatic =>
            member.fieldInfo?.IsStatic ??
            member.propertyInfo.GetAccessors(true)[0].IsStatic;
    }
}
