﻿using Bannerlord.BUTR.Shared.Extensions;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.UIExtenderEx.Utils;

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;

namespace Bannerlord.UIExtenderEx.Patches
{
    /// <summary>
    /// Skips type duplicates
    /// </summary>
    public static class WidgetFactoryPatch
    {
        private static bool _transpilerSuccessful;

        public static void Patch(Harmony harmony)
        {
            var e180 = ApplicationVersionHelper.TryParse("e1.8.0", out var e180Var) ? e180Var : ApplicationVersion.Empty;
            if (ApplicationVersionHelper.GameVersion() is { } gameVersion && gameVersion < e180)
            {
                harmony.Patch(
                    AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.WidgetFactory:Initialize"),
                    transpiler: new HarmonyMethod(typeof(WidgetFactoryPatch), nameof(InitializeTranspiler)));

                // Transpilers are very sensitive to code changes.
                // We can fall back to the old implementation of Initialize() as a last effort.
                if (!_transpilerSuccessful)
                {
                    harmony.Patch(
                        AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.WidgetFactory:Initialize"),
                        prefix: new HarmonyMethod(typeof(WidgetFactoryPatch), nameof(InitializePrefix)));
                }
            }
        }

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "For ReSharper")]
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> InitializeTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var instructionsList = instructions.ToList();

            IEnumerable<CodeInstruction> ReturnDefault(string place)
            {
                MessageUtils.DisplayUserWarning("Failed to patch WidgetPrefab.LoadFrom! {0}", place);
                return instructionsList.AsEnumerable();
            }

            if (AccessTools2.DeclaredField("TaleWorlds.GauntletUI.PrefabSystem.WidgetFactory:_builtinTypes") is not { } _builtinTypes)
                return ReturnDefault("WidgetFactory:_builtinTypes not found");

            var locals = method.GetMethodBody()?.LocalVariables;
            var typeLocal = locals?.FirstOrDefault(x => x.LocalType == typeof(Type));

            if (typeLocal is null)
                return ReturnDefault("Local not found");

            var startIndex = -1;
            var endIndex = -1;
            for (var i = 0; i < instructionsList.Count - 5; i++)
            {
                if (!instructionsList[i + 0].IsLdarg(0))
                    continue;

                if (!instructionsList[i + 1].LoadsField(_builtinTypes))
                    continue;

                if (!instructionsList[i + 2].IsLdloc())
                    continue;

                if (!instructionsList[i + 3].Calls(AccessTools2.DeclaredPropertyGetter("System.Reflection.MemberInfo:Name")))
                    continue;

                if (!instructionsList[i + 4].IsLdloc())
                    continue;

                if (!instructionsList[i + 5].Calls(AccessTools2.DeclaredMethod("System.Collections.Generic.Dictionary`2:Add")))
                    continue;

                startIndex = i;
                endIndex = i + 5;
                break;
            }

            if (startIndex == -1)
                return ReturnDefault("Pattern not found");

            if (instructionsList[endIndex + 1].labels.Count == 0)
                return ReturnDefault("Jmp was not found");

            var jmpEnumerator = instructionsList[endIndex + 1].labels.FirstOrDefault();

            // if (!this._builtinTypes.ContainsKey(type.Name))
            instructionsList.InsertRange(startIndex, new List<CodeInstruction>
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, _builtinTypes),
                new(OpCodes.Ldloc, typeLocal.LocalIndex),
                new(OpCodes.Callvirt, AccessTools2.DeclaredPropertyGetter("System.Reflection.MemberInfo:Name")),
                new(OpCodes.Callvirt, AccessTools2.DeclaredMethod("System.Collections.Generic.Dictionary`2:ContainsKey")),
                new(OpCodes.Brtrue_S, jmpEnumerator)
            });
            _transpilerSuccessful = true;
            return instructionsList.AsEnumerable();
        }

        private static readonly AccessTools.FieldRef<object, Dictionary<string, Type>>? BuiltinTypesField =
            AccessTools2.FieldRefAccess<Dictionary<string, Type>>("TaleWorlds.GauntletUI.PrefabSystem.WidgetFactory:_builtinTypes");
        private static readonly MethodInfo? GetPrefabNamesAndPathsFromCurrentPathMethod =
            AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.WidgetFactory:GetPrefabNamesAndPathsFromCurrentPath");

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "For ReSharper")]
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool InitializePrefix(WidgetFactory __instance)
        {
            var builtinTypes = BuiltinTypesField is not null ? BuiltinTypesField(__instance) : null;
            if (builtinTypes is not null && GetPrefabNamesAndPathsFromCurrentPathMethod?.Invoke(__instance, Array.Empty<object>()) is Dictionary<string, string> prefabsData)
            {
                foreach (var prefabExtension in __instance.PrefabExtensionContext.PrefabExtensions)
                {
                    var method = AccessTools2.DeclaredMethod(prefabExtension.GetType(), "RegisterAttributeTypes");
                    method?.Invoke(prefabExtension, new object[] { __instance.WidgetAttributeContext });
                }

                foreach (var type in WidgetInfo.CollectWidgetTypes())
                {
                    // PATCH
                    if (!builtinTypes.ContainsKey(type.Name))
                    {
                        // PATCH
                        builtinTypes.Add(type.Name, type);
                    }
                }

                foreach (var (key, value) in prefabsData)
                {
                    __instance.AddCustomType(key, value);
                }

                return false;
            }
            else
            {
                return true; // fallback
            }
        }
    }
}