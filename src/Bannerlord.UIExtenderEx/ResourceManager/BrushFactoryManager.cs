﻿using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;

using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;

namespace Bannerlord.UIExtenderEx.ResourceManager
{
    public static class BrushFactoryManager
    {
        private static readonly Dictionary<string, Brush> CustomBrushes = new();

        private delegate Brush LoadBrushFromDelegate(object instance, XmlNode brushNode);

        private static readonly LoadBrushFromDelegate? LoadBrushFrom =
            AccessTools2.GetDeclaredDelegate<LoadBrushFromDelegate>("TaleWorlds.GauntletUI.BrushFactory:LoadBrushFrom");

        public static IEnumerable<Brush> Create(XmlDocument xmlDocument)
        {
            foreach (XmlNode brushNode in xmlDocument.SelectSingleNode("Brushes")!.ChildNodes)
            {
                var brush = LoadBrushFrom?.Invoke(UIResourceManager.BrushFactory, brushNode);
                if (brush is not null)
                {
                    yield return brush;
                }
            }
        }

        public static void Register(IEnumerable<Brush> brushes)
        {
            foreach (var brush in brushes)
            {
                CustomBrushes[brush.Name] = brush;
            }
        }

        public static void CreateAndRegister(XmlDocument xmlDocument) => Register(Create(xmlDocument));

        internal static void Patch(Harmony harmony)
        {
            harmony.Patch(
                AccessTools2.DeclaredPropertyGetter("TaleWorlds.GauntletUI.BrushFactory:Brushes"),
                postfix: new HarmonyMethod(typeof(BrushFactoryManager), nameof(GetBrushesPostfix)));

            harmony.Patch(
                AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.BrushFactory:GetBrush"),
                prefix: new HarmonyMethod(typeof(BrushFactoryManager), nameof(GetBrushPrefix)));

#pragma warning disable BHA0001
            // Preventing inlining GetBrush
            harmony.TryPatch(
                AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.ConstantDefinition:GetValue"),
                transpiler: AccessTools2.DeclaredMethod("Bannerlord.UIExtenderEx.ResourceManager.BrushFactoryManager:BlankTranspiler"));
            harmony.TryPatch(
                AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.WidgetExtensions:SetWidgetAttributeFromString"),
                transpiler: AccessTools2.DeclaredMethod("Bannerlord.UIExtenderEx.ResourceManager.BrushFactoryManager:BlankTranspiler"));
            harmony.TryPatch(
                AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.UIContext:GetBrush"),
                transpiler: AccessTools2.DeclaredMethod("Bannerlord.UIExtenderEx.ResourceManager.BrushFactoryManager:BlankTranspiler"));
            harmony.TryPatch(
                AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.PrefabSystem.WidgetExtensions:ConvertObject"),
                transpiler: AccessTools2.DeclaredMethod("Bannerlord.UIExtenderEx.ResourceManager.BrushFactoryManager:BlankTranspiler"));
            harmony.TryPatch(
                AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.GauntletUI.Widgets.BoolBrushChanger:OnBooleanUpdated"),
                transpiler: AccessTools2.DeclaredMethod("Bannerlord.UIExtenderEx.ResourceManager.BrushFactoryManager:BlankTranspiler"));
            // Preventing inlining GetBrush
#pragma warning restore BHA0001
        }

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "For ReSharper")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void GetBrushesPostfix(ref IEnumerable<Brush> __result)
        {
            __result = __result.Concat(CustomBrushes.Values);
        }

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "For ReSharper")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool GetBrushPrefix(string name, IReadOnlyDictionary<string, Brush> ____brushes, ref Brush __result)
        {
            if (____brushes.ContainsKey(name) || !CustomBrushes.ContainsKey(name))
            {
                return true;
            }

            if (CustomBrushes[name] is { } brush)
            {
                __result = brush;
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> BlankTranspiler(IEnumerable<CodeInstruction> instructions) => instructions;
    }
}