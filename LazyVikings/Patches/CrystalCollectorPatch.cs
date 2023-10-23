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
public class CrystalCollectorPatch
{
    private static readonly MethodInfo _methodInfo =
        AccessTools.Method(typeof(CrystalCollectorPatch), nameof(DepositToContainers));

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CrystalCollector.CrystalCollector), nameof(CrystalCollector.CrystalCollector.UpdateTicks))]
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
    [HarmonyPatch(typeof(CrystalCollector.CrystalCollector), nameof(CrystalCollector.CrystalCollector.RPC_Extract))]
    private static bool RpcExtract_Prefix(ref CrystalCollector.CrystalCollector __instance)
    {
        var crystalCollector = __instance;
        if (Plugin._enableSapCollector.Value == Toggle.Off || !crystalCollector.m_nview.IsOwner()) return true;
        if (crystalCollector.GetLevel() <= 0) return true;
        var radius = Math.Min(50f, Math.Max(1f, Plugin._sapcollectorRadius.Value));
        var nearbyContainers = Helper.GetNearbyContainers(crystalCollector.gameObject, radius);
        if (nearbyContainers.Count == 0) return true;
        while (crystalCollector.GetLevel() >= 0)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(crystalCollector.m_CrystalItem.gameObject.name);
            ZNetView.m_forceDisableInit = true;
            var gameObject = Object.Instantiate(prefab);
            ZNetView.m_forceDisableInit = false;
            var itemDrop = gameObject.GetComponent<ItemDrop>();
            var flag = SpawnInsideContainers(itemDrop, true);
            Object.Destroy(gameObject);
            if (!flag) return true;
        }
        
        if (crystalCollector.GetLevel() == 0)
        {
            crystalCollector.m_outputEffect.Create(crystalCollector.m_output.position, Quaternion.identity);
        }
        return true;

        bool SpawnInsideContainers(ItemDrop item, bool mustHaveItem)
        {
            foreach (var container in from container in nearbyContainers
                     let inventory = container.GetInventory()
                     where (!mustHaveItem || inventory.HaveItem(item.m_itemData.m_shared.m_name)) &&
                           inventory.AddItem(item.m_itemData)
                     select container)
            {
                crystalCollector.m_nview.GetZDO().Set("level", crystalCollector.GetLevel() - 1);
                container.Save();
                container.GetInventory().Changed();
                return true;
            }

            return mustHaveItem && SpawnInsideContainers(item, false);
        }
    }
    
    private static void DepositToContainers(ref CrystalCollector.CrystalCollector __instance)
    {
        var crystalCollector = __instance;
        var radius = Math.Min(50f, Math.Max(1f, Plugin._sapcollectorRadius.Value));
        var nearbyContainers = Helper.GetNearbyContainers(crystalCollector.gameObject, radius);
        if (crystalCollector.GetLevel() != crystalCollector.m_maxCrystal) return;
        while (crystalCollector.GetLevel() > 0)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(crystalCollector.m_CrystalItem.gameObject.name);
            ZNetView.m_forceDisableInit = true;
            var gameObject = Object.Instantiate(prefab);
            ZNetView.m_forceDisableInit = false;
            var itemDrop = gameObject.GetComponent<ItemDrop>();
            var flag = SpawnInsideContainers(itemDrop, true);
            Object.Destroy(gameObject);
            if (!flag) return;
        }

        if (crystalCollector.GetLevel() == 0)
        {
            crystalCollector.m_outputEffect.Create(crystalCollector.m_output.position, Quaternion.identity);
        }
        return;

        bool SpawnInsideContainers(ItemDrop item, bool mustHaveItem)
        {
            foreach (var container in from container in nearbyContainers
                     let inventory = container.GetInventory()
                     where (!mustHaveItem || inventory.HaveItem(item.m_itemData.m_shared.m_name)) &&
                           inventory.AddItem(item.m_itemData)
                     select container)
            {
                crystalCollector.m_nview.GetZDO().Set("level", crystalCollector.GetLevel() - 1);
                container.Save();
                container.GetInventory().Changed();
                return true;
            }

            return mustHaveItem && SpawnInsideContainers(item, false);
        }
    }
}