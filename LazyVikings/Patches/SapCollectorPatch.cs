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

[HarmonyPatch]
public class SapCollectorPatch
{
    private static readonly MethodInfo _methodInfo =
        AccessTools.Method(typeof(SapCollectorPatch), nameof(DepositToContainers));

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.UpdateTick))]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        if (Plugin._enableSapCollector.Value == Toggle.Off) return instructions;
        var list = instructions.ToList();
        var num = list.Count - 2;
        list.Insert(++num, new CodeInstruction(OpCodes.Ldarga, 0));
        list.Insert(++num, new CodeInstruction(OpCodes.Call, _methodInfo));
        return list.AsEnumerable();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.RPC_Extract))]
    private static bool RpcExtract_Prefix(ref SapCollector __instance)
    {
        var sapCollector = __instance;
        if (Plugin._enableSapCollector.Value == Toggle.Off || !sapCollector.m_nview.IsOwner()) return true;
        if (sapCollector.GetLevel() <= 0) return true;
        var radius = Math.Min(50f, Math.Max(1f, Plugin._sapcollectorRadius.Value));
        var nearbyContainers = Helper.GetNearbyContainers(sapCollector.gameObject, radius);
        if (nearbyContainers.Count == 0) return true;
        while (sapCollector.GetLevel() > 0)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(__instance.m_spawnItem.gameObject.name);
            ZNetView.m_forceDisableInit = true;
            var gameObject = Object.Instantiate(prefab);
            ZNetView.m_forceDisableInit = false;
            var itemDrop = gameObject.GetComponent<ItemDrop>();
            var flag = SpawnInsideContainer(itemDrop, true);
            Object.Destroy(gameObject);
            if (!flag) return true;
        }

        if (sapCollector.GetLevel() == 0)
        {
            sapCollector.m_spawnEffect.Create(sapCollector.m_spawnPoint.position, Quaternion.identity);
        }

        return true;
        bool SpawnInsideContainer(ItemDrop item, bool mustHaveItem)
        {
            foreach (var container in from container in nearbyContainers
                     let inventory = container.GetInventory()
                     where (!mustHaveItem || inventory.HaveItem(item.m_itemData.m_shared.m_name)) &&
                           inventory.AddItem(item.m_itemData) select container)
            {
                sapCollector.m_nview.GetZDO().Set("level", sapCollector.GetLevel() - 1);
                container.Save();
                container.GetInventory().Changed();
                return true;
            }

            return mustHaveItem && SpawnInsideContainer(item, false);
        }
    }
    
    private static void DepositToContainers(ref SapCollector __instance)
    {
        var sapCollector = __instance;
        var radius = Math.Min(50f, Math.Max(1f, Plugin._sapcollectorRadius.Value));
        var nearbyContainers = Helper.GetNearbyContainers(sapCollector.gameObject, radius);
        if (sapCollector.GetLevel() != sapCollector.m_maxLevel) return;
        while (sapCollector.GetLevel() > 0)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(sapCollector.m_spawnItem.gameObject.name);
            ZNetView.m_forceDisableInit = true;
            var gameObject = Object.Instantiate(prefab);
            ZNetView.m_forceDisableInit = false;
            var itemDrop = gameObject.GetComponent<ItemDrop>();
            var flag = SpawnInsideContainers(itemDrop, true);
            Object.Destroy(gameObject);
            if (!flag) return;
        }

        if (sapCollector.GetLevel() == 0)
        {
            sapCollector.m_spawnEffect.Create(sapCollector.m_spawnPoint.position, Quaternion.identity);
        }

        return;

        bool SpawnInsideContainers(ItemDrop item, bool mustHaveItem)
        {
            foreach (var container in from container in nearbyContainers
                     let inventory = container.GetInventory()
                     where (!mustHaveItem || inventory.HaveItem(item.m_itemData.m_shared.m_name)) &&
                           inventory.AddItem(item.m_itemData) select container)
            {
                sapCollector.m_nview.GetZDO().Set("level", sapCollector.GetLevel() - 1);
                container.Save();
                container.GetInventory().Changed();
                return true;
            }

            return mustHaveItem && SpawnInsideContainers(item, false);
        }
    }
}