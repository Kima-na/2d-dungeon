using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EquipmentInventory : MonoBehaviour
{
    [SerializeField] private List<EquipmentData> startingEquipment = new();
    [SerializeField] private List<EquipmentData> lootTemplates = new();
    [SerializeField] private List<EquipmentItem> ownedItems = new();
    [SerializeField] private List<EquipmentItem> equippedItems = new();

    public IReadOnlyList<EquipmentItem> OwnedItems => ownedItems;
    public IReadOnlyList<EquipmentItem> EquippedItems => equippedItems;
    public event Action EquipmentChanged;
    public event Action InventoryChanged;

    private void Awake()
    {
        foreach (EquipmentData data in startingEquipment)
        {
            if (data == null) continue;
            EquipmentItem item = data.Roll(data.Rarity);
            ownedItems.Add(item);
            Equip(item);
        }
        if (GetComponent<EquipmentInventoryUI>() == null) gameObject.AddComponent<EquipmentInventoryUI>();
    }

    public EquipmentItem AddRandom(EquipmentData data, EquipmentRarity? rarity = null)
    {
        if (data == null) return null;
        EquipmentItem item = data.Roll(rarity);
        ownedItems.Add(item);
        InventoryChanged?.Invoke();
        return item;
    }

    public EquipmentItem RollRandomLoot()
    {
        List<EquipmentData> source = lootTemplates.Count > 0 ? lootTemplates : startingEquipment;
        if (source.Count == 0) return null;
        EquipmentData data = source[UnityEngine.Random.Range(0, source.Count)];
        float roll = UnityEngine.Random.value;
        EquipmentRarity rarity = roll < 0.03f ? EquipmentRarity.Legendary :
            roll < 0.12f ? EquipmentRarity.Epic : roll < 0.32f ? EquipmentRarity.Rare :
            roll < 0.62f ? EquipmentRarity.Uncommon : EquipmentRarity.Common;
        return AddRandom(data, rarity);
    }

    public EquipmentItem RollRandomLoot(EquipmentRarity rarity)
    {
        List<EquipmentData> source = lootTemplates.Count > 0 ? lootTemplates : startingEquipment;
        if (source.Count == 0) return null;
        return AddRandom(source[UnityEngine.Random.Range(0, source.Count)], rarity);
    }

    public bool Add(EquipmentData data) => AddRandom(data, data != null ? data.Rarity : null) != null;

    public bool Equip(EquipmentItem item)
    {
        if (item?.data == null || !ownedItems.Contains(item)) return false;
        Unequip(item.data.Slot, false);
        equippedItems.Add(item);
        EquipmentChanged?.Invoke();
        return true;
    }

    public bool Equip(EquipmentData data)
    {
        EquipmentItem item = ownedItems.Find(value => value.data == data);
        return Equip(item);
    }

    public bool Unequip(EquipmentSlot slot, bool notify = true)
    {
        int removed = equippedItems.RemoveAll(item => item?.data != null && item.data.Slot == slot);
        if (removed == 0) return false;
        if (notify) EquipmentChanged?.Invoke();
        return true;
    }

    public EquipmentItem GetEquipped(EquipmentSlot slot) =>
        equippedItems.Find(item => item?.data != null && item.data.Slot == slot);

    public float Sum(EquipmentStat stat)
    {
        float total = 0f;
        foreach (EquipmentItem item in equippedItems)
            if (item?.data != null) total += item.GetBonus(stat);
        return total;
    }

    public void ResetToStartingEquipment()
    {
        ownedItems.Clear();
        equippedItems.Clear();
        foreach (EquipmentData data in startingEquipment)
        {
            if (data == null) continue;
            EquipmentItem item = data.Roll(data.Rarity);
            ownedItems.Add(item);
            equippedItems.RemoveAll(value => value?.data != null && value.data.Slot == data.Slot);
            equippedItems.Add(item);
        }
        InventoryChanged?.Invoke();
        EquipmentChanged?.Invoke();
    }

    public void ApplySave(System.Collections.Generic.List<SavedEquipmentItem> savedItems)
    {
        ownedItems.Clear();
        equippedItems.Clear();
        EquipmentData[] catalog = Resources.LoadAll<EquipmentData>("Equipment");
        if (savedItems != null)
        {
            foreach (SavedEquipmentItem saved in savedItems)
            {
                EquipmentData data = System.Array.Find(catalog, value => value.ItemId == saved.itemId);
                if (data == null) continue;
                var item = new EquipmentItem
                {
                    data = data, rarity = (EquipmentRarity)saved.rarity,
                    instanceId = saved.instanceId, affixes = saved.affixes ?? new List<EquipmentAffix>()
                };
                ownedItems.Add(item);
                if (saved.equipped) equippedItems.Add(item);
            }
        }
        InventoryChanged?.Invoke();
        EquipmentChanged?.Invoke();
    }
}
