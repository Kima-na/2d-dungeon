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
        EquipmentItem item = CreateRandomLoot();
        return Add(item) ? item : null;
    }

    public EquipmentItem RollRandomLoot(EquipmentRarity rarity)
    {
        EquipmentItem item = CreateRandomLoot(rarity);
        return Add(item) ? item : null;
    }

    public EquipmentItem RollShopLoot(EquipmentRarity rarity, PlayerStats.PlayerClass playerClass)
    {
        EquipmentData[] catalog = Resources.LoadAll<EquipmentData>("Equipment");
        var candidates = new List<EquipmentData>();
        foreach (EquipmentData data in catalog)
            if (data != null && data.IsUsableBy(playerClass)) candidates.Add(data);

        // Keep locally assigned templates as a safe fallback for test scenes,
        // while preserving the same strict class restriction.
        if (candidates.Count == 0)
        {
            List<EquipmentData> local = lootTemplates.Count > 0 ? lootTemplates : startingEquipment;
            foreach (EquipmentData data in local)
                if (data != null && data.IsUsableBy(playerClass) && !candidates.Contains(data)) candidates.Add(data);
        }
        if (candidates.Count == 0) return null;
        EquipmentItem item = candidates[UnityEngine.Random.Range(0, candidates.Count)].Roll(rarity);
        return Add(item) ? item : null;
    }

    public EquipmentItem CreateRandomLoot(EquipmentRarity? forcedRarity = null)
    {
        List<EquipmentData> source = GetLootCandidates();
        if (source.Count == 0) return null;
        float roll = UnityEngine.Random.value;
        EquipmentRarity rarity = forcedRarity ?? (roll < 0.03f ? EquipmentRarity.Legendary :
            roll < 0.12f ? EquipmentRarity.Epic : roll < 0.32f ? EquipmentRarity.Rare :
            roll < 0.62f ? EquipmentRarity.Uncommon : EquipmentRarity.Common);
        return source[UnityEngine.Random.Range(0, source.Count)].Roll(rarity);
    }

    public bool Add(EquipmentItem item)
    {
        if (item?.data == null) return false;
        ownedItems.Add(item); InventoryChanged?.Invoke(); return true;
    }

    private List<EquipmentData> GetLootCandidates()
    {
        List<EquipmentData> catalog = lootTemplates.Count > 0 ? lootTemplates : startingEquipment;
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) return new List<EquipmentData>(catalog);
        return catalog.FindAll(data => data != null && data.IsUsableBy(stats.CurrentClass));
    }

    public bool Add(EquipmentData data) => AddRandom(data, data != null ? data.Rarity : null) != null;

    public bool Equip(EquipmentItem item)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (item?.data == null || !ownedItems.Contains(item) ||
            (stats != null && !item.data.IsUsableBy(stats.CurrentClass))) return false;
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

    public EquipmentItem GetUsableEquippedWeapon(PlayerStats.PlayerClass playerClass) =>
        equippedItems.Find(item => item?.data != null && item.data.Slot == EquipmentSlot.Weapon &&
                                   item.data.IsUsableBy(playerClass));

    public float Sum(EquipmentStat stat)
    {
        float total = 0f;
        PlayerStats stats = GetComponent<PlayerStats>();
        foreach (EquipmentItem item in equippedItems)
            if (item?.data != null && (stats == null || item.data.IsUsableBy(stats.CurrentClass)))
                total += item.GetBonus(stat);
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
            PlayerStats stats = GetComponent<PlayerStats>();
            if (stats != null && data.IsUsableBy(stats.CurrentClass))
            {
                equippedItems.RemoveAll(value => value?.data != null && value.data.Slot == data.Slot);
                equippedItems.Add(item);
            }
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
