﻿using Bannerlord.UIExtenderEx.Utils;

using HarmonyLib.BUTR.Extensions;

namespace Bannerlord.UIExtenderEx.Extensions
{
    internal static class ReflectionHelpers
    {
        /// <summary>
        /// Can be null
        /// </summary>
        public static T? PrivateValue<T>(this object? o, string fieldName)
        {
            if (o is null) return default!;
            var field = AccessTools2.DeclaredField(o.GetType(), fieldName);
            MessageUtils.Assert(field is not null, $"private value getter on {o}.{fieldName}");
            return field?.GetValue(o) is T obj ? obj : default;
        }

        public static void PrivateValueSet<T>(this object? o, string fieldName, T? value)
        {
            if (o is null) return;
            var field = AccessTools2.DeclaredField(o.GetType(), fieldName);
            MessageUtils.Assert(field is not null, $"private value setter on {o}.{fieldName}");
            field?.SetValue(o, value);
        }
    }
}