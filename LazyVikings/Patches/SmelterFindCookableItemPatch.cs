using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LazyVikings.Utils;

namespace LazyVikings.Patches;

[HarmonyPatch(typeof(Smelter), nameof(Smelter.FindCookableItem))]
public static class SmelterFindCookableItemPatch
{
    private static MethodInfo _methodInfo =
        AccessTools.Method(typeof(SmelterFindCookableItemPatch), nameof(DontProcessAllWoods));

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        if (Plugin._enableKiln.Value == Toggle.Off) return instructions;
        var num = -1;
        var list = instructions.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].opcode == OpCodes.Stloc_1)
            {
                list.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                list.Insert(++i, new CodeInstruction(OpCodes.Ldloc_1));
                list.Insert(++i, new CodeInstruction(OpCodes.Call, _methodInfo));
                num = i;
            }
            else if (num != -1 && list[i].opcode == OpCodes.Brfalse)
            {
                list.Insert(++num, new CodeInstruction(OpCodes.Brtrue, list[i].operand));
                return list.AsEnumerable();
            }
        }

        return instructions;
    }

    private static bool DontProcessAllWoods(Smelter smelter, Smelter.ItemConversion itemConversion)
    {
        return smelter.m_name == "$piece_charcoalkiln" && Plugin._kilnProcessAllWoods.Value == Toggle.Off &&
               itemConversion.m_from.m_itemData.m_shared.m_name is "$item_finewood" or "$item_roundlog";
    }
}