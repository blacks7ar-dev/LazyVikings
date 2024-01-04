using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LazyVikings.Functions;
using LazyVikings.Utils;

namespace LazyVikings.Patches;

[HarmonyPatch]
public class SmelterPatch
{
    private static readonly MethodInfo _methodInfo =
        AccessTools.Method(typeof(SmelterPatch), nameof(DontProcessAllWoods));

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.FindCookableItem))]
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
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.Awake))]
    private static void Awake_Prefix(ref Smelter __instance)
    {
        if (__instance.m_name != "$piece_blastfurnace" || Plugin._enableBlastFurnace.Value != Toggle.On) return;
        if (Plugin._blastfurnaceAllowAllOres.Value == Toggle.On)
        {
            __instance.m_conversion.AddRange(new List<Smelter.ItemConversion>
            {
                new()
                {
                    m_from = ObjectDB.instance.GetItemPrefab("CopperOre").GetComponent<ItemDrop>(),
                    m_to = ObjectDB.instance.GetItemPrefab("Copper").GetComponent<ItemDrop>()
                },
                new()
                {
                    m_from = ObjectDB.instance.GetItemPrefab("IronScrap").GetComponent<ItemDrop>(),
                    m_to = ObjectDB.instance.GetItemPrefab("Iron").GetComponent<ItemDrop>()
                },
                new()
                {
                    m_from = ObjectDB.instance.GetItemPrefab("SilverOre").GetComponent<ItemDrop>(),
                    m_to = ObjectDB.instance.GetItemPrefab("Silver").GetComponent<ItemDrop>()
                },
                new()
                {
                    m_from = ObjectDB.instance.GetItemPrefab("TinOre").GetComponent<ItemDrop>(),
                    m_to = ObjectDB.instance.GetItemPrefab("Tin").GetComponent<ItemDrop>()
                }
            });    
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.Spawn))]
    private static bool Spawn_Prefix(string ore, int stack, ref Smelter __instance)
    {
        if (__instance == null) return false;
        var smelter = __instance;
        if (!smelter.m_nview.IsOwner()) return true;
        return __instance.m_name switch
        {
            "$piece_charcoalkiln" when Plugin._enableKiln.Value == Toggle.On &&
                                       (Plugin._kilnAutomation.Value == Automation.Deposit ||
                                        Plugin._kilnAutomation.Value == Automation.Both) => Spawn(
                Math.Min(50f, Math.Max(1f, Plugin._kilnRadius.Value)),
                Plugin._kilnIgnorePrivateAreaCheck.Value == Toggle.On),
            "$piece_smelter" when Plugin._enableSmelter.Value == Toggle.On &&
                                  (Plugin._smelterAutomation.Value == Automation.Deposit ||
                                   Plugin._smelterAutomation.Value == Automation.Both) => Spawn(
                Math.Min(50f, Math.Max(1f, Plugin._smelterRadius.Value)),
                Plugin._smelterIgnorePrivateAreaCheck.Value == Toggle.On),
            "$piece_blastfurnace" when Plugin._enableBlastFurnace.Value == Toggle.On &&
                                       (Plugin._blastfurnaceAutomation.Value == Automation.Deposit ||
                                        Plugin._blastfurnaceAutomation.Value == Automation.Both) => Spawn(
                Math.Min(50f, Math.Max(1f, Plugin._blastfurnaceRadius.Value)),
                Plugin._blastfurnaceIgnorePrivateAreaCheck.Value == Toggle.On),
            "$piece_spinningwheel" when Plugin._enableSpinningWheel.Value == Toggle.On &&
                                        (Plugin._spinningwheelAutomation.Value == Automation.Deposit ||
                                         Plugin._spinningwheelAutomation.Value == Automation.Both) => Spawn(
                Math.Min(50f, Math.Max(1f, Plugin._spinningwheelRadius.Value)),
                Plugin._spinningwheelIgnorePrivateAreaCheck.Value == Toggle.On),
            "$piece_windmill" when Plugin._enableWindmill.Value == Toggle.On &&
                                   (Plugin._windmillAutomation.Value == Automation.Deposit ||
                                    Plugin._windmillAutomation.Value == Automation.Both) => Spawn(
                Math.Min(50f, Math.Max(1f, Plugin._windmillRadius.Value)),
                Plugin._windmillIgnorePrivateAreaCheck.Value == Toggle.On),
            "$piece_eitrrefinery" when Plugin._enableEitrRefinery.Value == Toggle.On &&
                                       (Plugin._eitrrefineryAutomation.Value == Automation.Deposit ||
                                        Plugin._eitrrefineryAutomation.Value == Automation.Both) => Spawn(
                Math.Min(50f, Math.Max(1f, Plugin._eitrrefineryRadius.Value)),
                Plugin._eitrrefineryIgnorePrivateAreaCheck.Value == Toggle.On),
            "$cws_stone_kiln" when Plugin._enableSteelKiln.Value == Toggle.On &&
                                   (Plugin._steelKilnAutomation.Value == Automation.Deposit ||
                                    Plugin._steelKilnAutomation.Value == Automation.Both) => Spawn(
                Math.Min(50f, Math.Max(1f, Plugin._steelKilnRadius.Value)),
                Plugin._steelKilnIgnorePrivateAreaCheck.Value == Toggle.On),
            "$cws_slack_tub" when Plugin._enableSteelSlackTub.Value == Toggle.On &&
                                  (Plugin._steelSlackTubAutomation.Value == Automation.Deposit ||
                                   Plugin._steelSlackTubAutomation.Value == Automation.Both) => Spawn(
                Math.Min(50f, Math.Max(1f, Plugin._steelSlackTubRadius.Value)),
                Plugin._steelSlackTubIgnorePrivateAreaCheck.Value == Toggle.On),
            _ => true
        };

        bool Spawn(float depositRadius, bool ignorePrivateAreaCheck)
        {
            var nearbyContainers = Helper.GetNearbyContainers(smelter.gameObject, depositRadius, !ignorePrivateAreaCheck);
            if (nearbyContainers.Count == 0) return true;
            depositRadius = depositRadius switch
            {
                > 50f => 50f,
                < 1f => 1f,
                _ => depositRadius
            };

            var prefab = ObjectDB.instance.GetItemPrefab(smelter.GetItemConversion(ore).m_to.gameObject.name);
            ZNetView.m_forceDisableInit = true;
            var gameObject = UnityEngine.Object.Instantiate(prefab);
            ZNetView.m_forceDisableInit = false;
            var itemDrop = gameObject.GetComponent<ItemDrop>();
            itemDrop.m_itemData.m_stack = stack;
            var result = SpawnInsideContainer(true);
            UnityEngine.Object.Destroy(gameObject);
            return result;
            bool SpawnInsideContainer(bool mustHaveItem)
            {
                foreach (var item in nearbyContainers)
                {
                    var inventory = item.GetInventory();
                    if ((mustHaveItem && !inventory.HaveItem(itemDrop.m_itemData.m_shared.m_name)) ||
                        !inventory.AddItem(itemDrop.m_itemData)) continue;
                    var transform = smelter.transform;
                    smelter.m_produceEffects.Create(transform.position, transform.rotation);
                    item.Save();
                    item.GetInventory().Changed();
                    return false;
                }

                return !mustHaveItem || SpawnInsideContainer(false);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.UpdateSmelter))]
    private static void UpdateSmelter_Prefix(Smelter __instance)
    {
        if (__instance == null || !Player.m_localPlayer || __instance.m_nview == null ||
            !__instance.m_nview.IsOwner()) return;
        var stopwatch = Helper.GetGameObjectStopwatch(__instance.gameObject);
        if (stopwatch.IsRunning && stopwatch.ElapsedMilliseconds < 1000) return;
        stopwatch.Restart();
        var value = 0f;
        var flag = false;
        var flag2 = false;
        switch (__instance.m_name)
        {
            case "$piece_charcoalkiln" when Plugin._enableKiln.Value == Toggle.Off ||
                                            Plugin._kilnAutomation.Value == Automation.Deposit:
                return;
            case "$piece_charcoalkiln":
                value = Plugin._kilnRadius.Value;
                flag = Plugin._kilnIgnorePrivateAreaCheck.Value == Toggle.On;
                flag2 = true;
                break;
            case "$piece_smelter" when Plugin._enableSmelter.Value == Toggle.Off ||
                                       Plugin._smelterAutomation.Value == Automation.Deposit:
                return;
            case "$piece_smelter":
                value = Plugin._smelterRadius.Value;
                flag = Plugin._smelterIgnorePrivateAreaCheck.Value == Toggle.On;
                break;
            case "$piece_blastfurnace" when Plugin._enableBlastFurnace.Value == Toggle.Off ||
                                            Plugin._blastfurnaceAutomation.Value == Automation.Deposit:
                return;
            case "$piece_blastfurnace":
                value = Plugin._blastfurnaceRadius.Value;
                flag = Plugin._blastfurnaceIgnorePrivateAreaCheck.Value == Toggle.On;
                break;
            case "$piece_spinningwheel" when Plugin._enableSpinningWheel.Value == Toggle.Off ||
                                             Plugin._spinningwheelAutomation.Value == Automation.Deposit:
                return;
            case "$piece_spinningwheel":
                value = Plugin._spinningwheelRadius.Value;
                flag = Plugin._spinningwheelIgnorePrivateAreaCheck.Value == Toggle.On;
                break;
            case "$piece_windmill" when Plugin._enableWindmill.Value == Toggle.Off ||
                                        Plugin._windmillAutomation.Value == Automation.Deposit:
                return;
            case "$piece_windmill":
                value = Plugin._windmillRadius.Value;
                flag = Plugin._windmillIgnorePrivateAreaCheck.Value == Toggle.On;
                break;
            case "$piece_eitrrefinery" when Plugin._enableEitrRefinery.Value == Toggle.Off ||
                                            Plugin._eitrrefineryAutomation.Value == Automation.Deposit:
                return;
            case "$piece_eitrrefinery":
                value = Plugin._eitrrefineryRadius.Value;
                flag = Plugin._eitrrefineryIgnorePrivateAreaCheck.Value == Toggle.On;
                break;
            case "$cws_stone_kiln" when Plugin._enableSteelKiln.Value == Toggle.Off ||
                                        Plugin._steelKilnAutomation.Value == Automation.Deposit:
                return;
            case "$cws_stone_kiln":
                if (!Plugin._hasOdinSteelWorks) return;
                value = Plugin._steelKilnRadius.Value;
                flag = Plugin._steelKilnIgnorePrivateAreaCheck.Value == Toggle.On;
                break;
            case "$cws_slack_tub" when Plugin._enableSteelSlackTub.Value == Toggle.Off ||
                                       Plugin._steelSlackTubAutomation.Value == Automation.Deposit:
                return;
            case "$cws_slack_tub":
                if (!Plugin._hasOdinSteelWorks) return;
                value = Plugin._steelSlackTubRadius.Value;
                flag = Plugin._steelSlackTubIgnorePrivateAreaCheck.Value == Toggle.On;
                break;
        }

        value = Math.Min(50f, Math.Max(1f, value));
        var num = __instance.m_maxOre - __instance.GetQueueSize();
        var num2 = __instance.m_maxFuel - (int)Math.Ceiling(__instance.GetFuel());
        if ((bool)__instance.m_fuelItem && num2 > 0)
        {
            var itemData = __instance.m_fuelItem.m_itemData;
            var num3 = Helper.DeductItemFromAllNearbyContainers(__instance.gameObject, value, itemData, 1,
                !flag);
            for (var i = 0; i < num3; i++)
            {
                __instance.m_nview.InvokeRPC("AddFuel");
            }
        }

        if (num <= 0) return;
        var nearbyContainers = Helper.GetNearbyContainers(__instance.gameObject, value);
        foreach (var item in nearbyContainers)
        {
            foreach (var item2 in __instance.m_conversion)
            {
                if (flag2)
                {
                    if (Plugin._kilnProcessAllWoods.Value == Toggle.Off &&
                        item2.m_from.m_itemData.m_shared.m_name is "$item_finewood" or "$item_roundlog") continue;
                    var num4 = Plugin._kilnProductThreshold.Value >= 0 ? Plugin._kilnProductThreshold.Value : 0;
                    if (num4 > 0 &&
                        Helper.GetItemAmounts(Helper.GetNearbyItemsFromContainers(nearbyContainers),
                            item2.m_to.m_itemData) >= num4) return;
                }

                var itemData2 = item2.m_from.m_itemData;
                var num5 = Helper.DeductItemFromContainer(item, itemData2, 1);
                if (num5 <= 0) continue;
                var prefab = ObjectDB.instance.GetItemPrefab(item2.m_from.gameObject.name);
                for (var j = 0; j < num5; j++)
                {
                    __instance.m_nview.InvokeRPC("AddOre", prefab.name);
                }

                num -= num5;
                if (num == 0) return;
            }
        }
    }

    private static bool DontProcessAllWoods(Smelter smelter, Smelter.ItemConversion itemConversion)
    {
        return smelter.m_name == "$piece_charcoalkiln" && Plugin._kilnProcessAllWoods.Value == Toggle.Off &&
               itemConversion.m_from.m_itemData.m_shared.m_name is "$item_finewood" or "$item_roundlog";
    }
}