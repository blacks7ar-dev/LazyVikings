using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace LazyVikings.Utils;

public static class Helper
{
    private static readonly ConcurrentDictionary<float, Stopwatch> _stopwatches = new();
    public static List<Container> GetNearbyContainers(GameObject gameObject, float radius, bool checkWard = true)
    {
        var layerNames = new[] { "piece", "item", "vehicle" };
        var source = Physics.OverlapSphere(gameObject.transform.position, radius, LayerMask.GetMask(layerNames));
        var orderedByEnumerable =
            source.OrderBy(x => Vector3.Distance(x.gameObject.transform.position, gameObject.transform.position));
        var list = new List<Container>();
        foreach (var item in orderedByEnumerable)
        {
            try
            {
                var componentInParent = item.GetComponentInParent<Container>();
                var flag = componentInParent.CheckAccess(Player.m_localPlayer.GetPlayerID());
                if (checkWard)
                {
                    flag = flag && PrivateArea.CheckAccess(item.gameObject.transform.position, 0f, false, true);
                }

                var componentInParent2 = componentInParent.GetComponentInParent<Piece>();
                var flag2 = componentInParent.GetComponentInParent<Vagon>() != null;
                var flag3 = componentInParent.GetComponentInParent<Ship>() != null;
                if (componentInParent2 != null && flag && componentInParent.GetInventory() != null && !flag2 &&
                    !flag3 && componentInParent2.IsPlacedByPlayer())
                {
                    list.Add(componentInParent);
                }
            }
            catch
            {
            }
        }

        return list;
    }

    private static float GetGameObjectPositionHash(GameObject gameObject)
    {
        var position = gameObject.transform.position;
        return 1000f * position.x + position.y + 0.001f * position.z;
    }

    public static Stopwatch GetGameObjectStopwatch(GameObject gameObject)
    {
        var gameObjectPosHash = GetGameObjectPositionHash(gameObject);
        if (_stopwatches.TryGetValue(gameObjectPosHash, out var value)) return value;
        value = new Stopwatch();
        _stopwatches.TryAdd(gameObjectPosHash, value);

        return value;
    }

    public static List<ItemDrop.ItemData> GetNearbyItemsFromContainers(IEnumerable<Container> nearbyContainers)
    {
        return nearbyContainers.SelectMany(nearbyContainer => nearbyContainer.GetInventory().GetAllItems()).ToList();
    }

    public static int GetItemAmounts(IEnumerable<ItemDrop.ItemData> itemList, ItemDrop.ItemData itemData)
    {
        return itemList.Where(item => item.m_shared.m_name == itemData.m_shared.m_name).Sum(item => item.m_stack);
    }

    private static bool ContainerContainsItem(Container container, ItemDrop.ItemData itemData)
    {
        var allItems = container.GetInventory().GetAllItems();
        return allItems.Any(item => item.m_shared.m_name == itemData.m_shared.m_name);
    }

    public static int DeductItemFromContainer(Container container, ItemDrop.ItemData itemData, int amount = 1)
    {
        if (!ContainerContainsItem(container, itemData)) return 0;
        var num = 0;
        var allItems = container.GetInventory().GetAllItems();
        foreach (var item in allItems)
        {
            if (item.m_shared.m_name != itemData.m_shared.m_name) continue;
            var num2 = Mathf.Min(item.m_stack, amount);
            item.m_stack -= num2;
            amount -= num2;
            num += num2;
            if (amount <= 0) break;
        }

        if (num == 0) return 0;
        allItems.RemoveAll(x => x.m_stack <= 0);
        container.m_inventory.m_inventory = allItems;
        container.Save();
        container.GetInventory().Changed();
        return num;
    }

    public static int DeductItemFromAllNearbyContainers(GameObject gameObject, float radius, ItemDrop.ItemData itemData,
        int amount, bool checkWard = true)
    {
        var nearbyContainers = GetNearbyContainers(gameObject, radius, checkWard);
        //var nearbyItemsFromContainer = GetNearbyItemsFromContainers(nearbyContainers);
        //var itemAmountInContainer = GetItemAmounts(nearbyItemsFromContainer, itemData);
        if (amount == 0) return 0;
        var num = 0;
        foreach (var num2 in from item in nearbyContainers
                 where num != amount
                 select DeductItemFromContainer(item, itemData, amount))
        {
            num += num2;
            amount -= num2;
        }

        return num;
    }
}