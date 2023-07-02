using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LazyVikings.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LazyVikings.Patches;

[HarmonyPatch(typeof(Beehive), nameof(Beehive.UpdateBees))]
public static class BeehiveUpdateBeesPatch
{
    private static readonly MethodInfo _methodInfo =
        AccessTools.Method(typeof(BeehiveUpdateBeesPatch), nameof(DepositToContainers));

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        if (Plugin._enableBeehive.Value == Toggle.Off) return instructions;
        var list = instructions.ToList();
        var num = list.Count - 2;
        list.Insert(++num, new CodeInstruction(OpCodes.Ldarga, 0));
        list.Insert(++num, new CodeInstruction(OpCodes.Call, _methodInfo));
        return list.AsEnumerable();
    }

    private static void DepositToContainers(ref Beehive __instance)
    {
        var beehive = __instance;
        var radius = Math.Min(50f, Math.Max(1f, Plugin._beehiveRadius.Value));
        var nearbyContainers = Helper.GetNearbyContainers(beehive.gameObject, radius);
        if (beehive.GetHoneyLevel() != beehive.m_maxHoney) return;
        while (beehive.GetHoneyLevel() > 0)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(beehive.m_honeyItem.gameObject.name);
            ZNetView.m_forceDisableInit = true;
            var gameObject = Object.Instantiate(prefab);
            ZNetView.m_forceDisableInit = false;
            var itemDrop = gameObject.GetComponent<ItemDrop>();
            var flag = SpawnInsideContainers(itemDrop, true);
            Object.Destroy(gameObject);
            if (!flag) return;
        }

        if (beehive.GetHoneyLevel() == 0)
        {
            beehive.m_spawnEffect.Create(beehive.m_spawnPoint.position, Quaternion.identity);
        }

        bool SpawnInsideContainers(ItemDrop item, bool mustHaveItem)
        {
            foreach (var item2 in from item2 in nearbyContainers
                     let inventory = item2.GetInventory()
                     where (!mustHaveItem || inventory.HaveItem(item.m_itemData.m_shared.m_name)) &&
                           inventory.AddItem(item.m_itemData) select item2)
            {
                beehive.m_nview.GetZDO().Set("level", beehive.GetHoneyLevel() - 1);
                item2.Save();
                item2.GetInventory().Changed();
                return true;
            }

            return mustHaveItem && SpawnInsideContainers(item, false);
        }
    }
}