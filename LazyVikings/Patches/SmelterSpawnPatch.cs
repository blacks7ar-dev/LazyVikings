using System;
using HarmonyLib;
using LazyVikings.Functions;
using LazyVikings.Utils;
using Object = UnityEngine.Object;

namespace LazyVikings.Patches;

[HarmonyPatch(typeof(Smelter), nameof(Smelter.Spawn))]
public static class SmelterSpawnPatch
{
    private static bool Prefix(string ore, int stack, ref Smelter __instance)
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
            var gameObject = Object.Instantiate(prefab);
            ZNetView.m_forceDisableInit = false;
            var itemDrop = gameObject.GetComponent<ItemDrop>();
            itemDrop.m_itemData.m_stack = stack;
            var result = SpawnInsideContainer(true);
            Object.Destroy(gameObject);
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
}