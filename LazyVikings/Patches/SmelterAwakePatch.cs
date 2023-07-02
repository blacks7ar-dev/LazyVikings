using System.Collections.Generic;
using HarmonyLib;
using LazyVikings.Utils;

namespace LazyVikings.Patches;

[HarmonyPatch(typeof(Smelter), nameof(Smelter.Awake))]
public static class SmelterAwakePatch
{
    private static void Prefix(ref Smelter __instance)
    {
        if (__instance.m_name == "$piece_blastfurnace" && Plugin._enableBlastFurnace.Value == Toggle.On)
        {
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
    }
}