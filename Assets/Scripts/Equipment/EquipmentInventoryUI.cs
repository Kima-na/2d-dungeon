using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class EquipmentInventoryUI : MonoBehaviour
{
    private EquipmentInventory inventory;
    private bool visible;
    private Vector2 scroll;
    private GUIStyle titleStyle;
    private GUIStyle sectionStyle;
    private GUIStyle itemStyle;
    private GUIStyle hintStyle;

    private void Awake() => inventory = GetComponent<EquipmentInventory>();

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            visible = !visible;
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.Label(new Rect(Screen.width - 210f, 12f, 200f, 26f), "[I] 배낭 열기", hintStyle);
        if (!visible || inventory == null) return;

        float width = Mathf.Min(880f, Screen.width - 32f);
        float height = Mathf.Min(620f, Screen.height - 32f);
        Rect window = new((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(window, GUIContent.none);
        GUILayout.BeginArea(new Rect(window.x + 20f, window.y + 16f, window.width - 40f, window.height - 32f));

        GUILayout.BeginHorizontal();
        GUILayout.Label("배낭", titleStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("랜덤 장비 획득", GUILayout.Width(150f), GUILayout.Height(32f))) inventory.RollRandomLoot();
        if (GUILayout.Button("닫기  [I]", GUILayout.Width(100f), GUILayout.Height(32f))) visible = false;
        GUILayout.EndHorizontal();
        GUILayout.Label("장비를 선택해 바로 장착하거나, 현재 장비를 해제할 수 있습니다.", hintStyle);
        GUILayout.Space(12f);

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(width * 0.38f), GUILayout.ExpandHeight(true));
        GUILayout.Label("현재 장착", sectionStyle);
        GUILayout.Space(4f);
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            EquipmentItem equipped = inventory.GetEquipped(slot);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(SlotName(slot), hintStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(equipped != null ? equipped.DisplayName : "비어 있음", itemStyle);
            GUI.enabled = equipped != null;
            if (GUILayout.Button("해제", GUILayout.Width(58f), GUILayout.Height(25f))) inventory.Unequip(slot);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();

        GUILayout.Space(12f);
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
        GUILayout.Label($"보유 장비  {inventory.OwnedItems.Count}개", sectionStyle);
        GUILayout.Space(4f);
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (EquipmentItem item in inventory.OwnedItems)
        {
            if (item?.data == null) continue;
            Color previous = GUI.contentColor;
            GUI.contentColor = EquipmentRarityUtility.Color(item.rarity);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{item.DisplayName}  [{SlotName(item.data.Slot)}]", itemStyle);
            bool equipped = inventory.GetEquipped(item.data.Slot) == item;
            GUI.enabled = !equipped;
            if (GUILayout.Button(equipped ? "장착 중" : "장착", GUILayout.Width(76f), GUILayout.Height(27f)))
                inventory.Equip(item);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUI.contentColor = previous;
            foreach (EquipmentAffix affix in item.affixes)
                GUILayout.Label("  • " + affix.Description, hintStyle);
            GUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        titleStyle ??= new GUIStyle(GUI.skin.label)
            { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        sectionStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        itemStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, wordWrap = true };
        hintStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
    }

    private static string SlotName(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.Weapon => "무기",
        EquipmentSlot.Helmet => "투구",
        EquipmentSlot.Armor => "갑옷",
        EquipmentSlot.Boots => "신발",
        _ => "장신구"
    };
}
