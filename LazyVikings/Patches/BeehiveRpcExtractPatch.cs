using System;
using System.Linq;
using HarmonyLib;
using LazyVikings.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LazyVikings.Patches;

[HarmonyPatch(typeof(Beehive), nameof(Beehive.RPC_Extract))]
public static class BeehiveRpcExtractPatch
{
    private static bool Prefix(ref Beehive __instance)
    {
        var beehive = __instance;
        if (Plugin._enableBeehive.Value == Toggle.Off || !beehive.m_nview.IsOwner()) return true;
        if (beehive.GetHoneyLevel() <= 0) return true;
        var radius = Math.Min(50f, Math.Max(1f, Plugin._beehiveRadius.Value));
        var nearbyContainers = Helper.GetNearbyContainers(beehive.gameObject, radius);
        if (nearbyContainers.Count == 0) return true;
        while (beehive.GetHoneyLevel() > 0)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(__instance.m_honeyItem.gameObject.name);
            ZNetView.m_forceDisableInit = true;
            var gameObject = Object.Instantiate(prefab);
            ZNetView.m_forceDisableInit = false;
            var itemDrop = gameObject.GetComponent<ItemDrop>();
            var flag = SpawnInsideContainers(itemDrop, true);
            Object.Destroy(gameObject);
            if (!flag) return true;
        }

        if (beehive.GetHoneyLevel() == 0)
        {
            beehive.m_spawnEffect.Create(beehive.m_spawnPoint.position, Quaternion.identity);
        }

        return true;
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