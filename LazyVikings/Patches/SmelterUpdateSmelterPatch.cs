using System;
using HarmonyLib;
using LazyVikings.Functions;
using LazyVikings.Utils;

namespace LazyVikings.Patches;

[HarmonyPatch(typeof(Smelter), nameof(Smelter.UpdateSmelter))]
public static class SmelterUpdateSmelterPatch
{
    private static void Prefix(Smelter __instance)
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
            case "$piece_charcoalkiln" when Plugin._enableKiln.Value == Toggle.Off || Plugin._kilnAutomation.Value == Automation.Deposit:
                return;
            case "$piece_charcoalkiln":
                value = Plugin._kilnRadius.Value;
                flag = Plugin._kilnIgnorePrivateAreaCheck.Value == Toggle.On;
                flag2 = true;
                break;
            case "$piece_smelter" when Plugin._enableSmelter.Value == Toggle.Off || Plugin._smelterAutomation.Value == Automation.Deposit:
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
            case "$piece_windmill" when Plugin._enableWindmill.Value == Toggle.Off || Plugin._windmillAutomation.Value == Automation.Deposit:
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
        }

        value = Math.Min(50f, Math.Max(1f, value));
        var num = __instance.m_maxOre - __instance.GetQueueSize();
        var num2 = __instance.m_maxFuel - (int)Math.Ceiling(__instance.GetFuel());
        if ((bool)__instance.m_fuelItem && num2 > 0)
        {
            var itemData = __instance.m_fuelItem.m_itemData;
            var num3 = Helper.DeductItemFromAllNearbyContainers(__instance.gameObject, value, itemData, num2,
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
                var num5 = Helper.DeductItemFromContainer(item, itemData2, num);
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
}