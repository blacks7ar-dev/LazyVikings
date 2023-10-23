using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LazyVikings.Functions;
using LazyVikings.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LazyVikings.Patches;

[HarmonyPatch]
public class FermenterPatch
{
    private static readonly MethodInfo _methodDepositToContainers =
        AccessTools.Method(typeof(FermenterPatch), nameof(DepositToContainers));
    private static readonly MethodInfo _methodSetActive = AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive));
    private static readonly MethodInfo _methodTap = AccessTools.Method(typeof(FermenterPatch), nameof(Tap));
    private static readonly MethodInfo _methodAddItemFromNearbyContainers =
        AccessTools.Method(typeof(FermenterPatch), nameof(AddItemFromNearbyContainers));

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.DelayedTap))]
    private static IEnumerable<CodeInstruction> DelayedTapTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        if (Plugin._enableFermenter.Value == Toggle.Off || Plugin._fermenterAutomation.Value == Automation.Fuel)
            return instructions;
        var list = instructions.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].opcode == OpCodes.Brfalse)
            {
                var num = i;
                list.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                list.Insert(++i, new CodeInstruction(OpCodes.Ldloca, 0));
                list.Insert(++i, new CodeInstruction(OpCodes.Call, _methodDepositToContainers));
                list.Insert(++i, new CodeInstruction(OpCodes.Brtrue, list[num].operand));
                return list.AsEnumerable();
            }
        }

        return instructions;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.SlowUpdate))]
    private static IEnumerable<CodeInstruction> SlowUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        if (Plugin._enableFermenter.Value == Toggle.Off) return instructions;
        var list = instructions.ToList();
        var num = 0;
        var flag = false;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Calls(_methodSetActive))
            {
                num++;
            }

            if (num == 3)
            {
                list.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                list.Insert(++i, new CodeInstruction(OpCodes.Call, _methodAddItemFromNearbyContainers));
                flag = true;
                break;
            }
        }

        if (!flag) return instructions;
        flag = false;
        for (var j = list.Count - 1; j >= 0; j--)
        {
            if (list[j].Calls(_methodSetActive))
            {
                list.Insert(++j, new CodeInstruction(OpCodes.Ldarg_0));
                list.Insert(++j, new CodeInstruction(OpCodes.Call, _methodTap));
                return list.AsEnumerable();
            }
        }

        return instructions;
    }

    private static bool DepositToContainers(Fermenter __instance, ref Fermenter.ItemConversion itemConversion)
    {
        var radius = Math.Min(50f, Math.Max(1f, Plugin._fermenterRadius.Value));
        var flag = Plugin._fermenterIgnorePrivateAreaCheck.Value == Toggle.On;
        var nearbyContainers = Helper.GetNearbyContainers(__instance.gameObject, radius, !flag);
        var num = 0;
        for (var i = 0; i < itemConversion.m_producedItems; i++)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(itemConversion.m_to.gameObject.name);
            ZNetView.m_forceDisableInit = true;
            var gameObject = Object.Instantiate(prefab);
            ZNetView.m_forceDisableInit = false;
            var itemDrop = gameObject.GetComponent<ItemDrop>();
            var flag2 = SpawnInsideContainers(itemDrop, true);
            Object.Destroy(gameObject);
            if (!flag2)
            {
                itemConversion.m_producedItems -= num;
                return false;
            }

            num++;
        }

        return true;

        bool SpawnInsideContainers(ItemDrop item, bool mustHaveItem)
        {
            foreach (var item2 in from item2 in nearbyContainers
                     let inventory = item2.GetInventory()
                     where (!mustHaveItem || inventory.HaveItem(item.m_itemData.m_shared.m_name)) &&
                           inventory.AddItem(item.m_itemData) select item2)
            {
                item2.Save();
                item2.GetInventory().Changed();
                return true;
            }

            return mustHaveItem && SpawnInsideContainers(item, false);
        }
    }

    private static void Tap(Fermenter __instance)
    {
        if (Plugin._fermenterAutomation.Value == Automation.Deposit ||
            Plugin._fermenterAutomation.Value == Automation.Both)
        {
            __instance.m_nview.InvokeRPC("Tap");
        }
    }

    private static void AddItemFromNearbyContainers(Fermenter __instance)
    {
        if (Plugin._fermenterAutomation.Value == Automation.Deposit || __instance.GetStatus() != 0 ||
            !__instance.m_nview.IsOwner()) return;
        var stopWatch = Helper.GetGameObjectStopwatch(__instance.gameObject);
        if (stopWatch.IsRunning && stopWatch.ElapsedMilliseconds <= 1000) return;
        var radius = Math.Min(50f, Math.Max(1f, Plugin._fermenterRadius.Value));
        var flag = Plugin._fermenterIgnorePrivateAreaCheck.Value == Toggle.On;
        var nearbyContainers = Helper.GetNearbyContainers(__instance.gameObject, radius, !flag);
        foreach (var item in from container in nearbyContainers
                 let item = __instance.FindCookableItem(container.GetInventory())
                 where item != null && Helper.DeductItemFromContainer(container, item) != 0
                 select item)
        {
            __instance.m_nview.InvokeRPC("AddItem", item.m_dropPrefab.name);
            break;
        }
        stopWatch.Restart();
    }
}