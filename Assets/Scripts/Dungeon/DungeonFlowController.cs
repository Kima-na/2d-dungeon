using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class DungeonProgress
{
    const string Key = "DungeonDifficultyCleared.";
    public static bool EasyCleared => IsCleared(DungeonDifficulty.Easy);
    public static bool NormalCleared => IsCleared(DungeonDifficulty.Normal);
    public static bool HardCleared => IsCleared(DungeonDifficulty.Hard);
    public static bool NightmareCleared => IsCleared(DungeonDifficulty.Nightmare);
    public static bool IsCleared(DungeonDifficulty d) => PlayerPrefs.GetInt(Key + d, 0) == 1;
    public static bool IsUnlocked(DungeonDifficulty d) => d == DungeonDifficulty.Easy ||
        d == DungeonDifficulty.Normal && EasyCleared || d == DungeonDifficulty.Hard && NormalCleared ||
        d == DungeonDifficulty.Nightmare && HardCleared;
    public static void MarkCleared(DungeonDifficulty d) { PlayerPrefs.SetInt(Key + d, 1); PlayerPrefs.Save(); }
    public static void Reset()
    {
        foreach (DungeonDifficulty d in System.Enum.GetValues(typeof(DungeonDifficulty))) PlayerPrefs.DeleteKey(Key + d);
        PlayerPrefs.Save();
    }
    public static void Apply(GameSaveData d)
    {
        Reset(); if (d == null) return;
        if (d.easyCleared) PlayerPrefs.SetInt(Key + DungeonDifficulty.Easy, 1);
        if (d.normalCleared) PlayerPrefs.SetInt(Key + DungeonDifficulty.Normal, 1);
        if (d.hardCleared) PlayerPrefs.SetInt(Key + DungeonDifficulty.Hard, 1);
        if (d.nightmareCleared) PlayerPrefs.SetInt(Key + DungeonDifficulty.Nightmare, 1);
        PlayerPrefs.Save();
    }
}

public sealed class DungeonFlowController : MonoBehaviour
{
    DungeonGenerator generator;
    Canvas canvas;
    PlayerController player;
    PlayerStats stats;
    EquipmentInventory inventory;
    GameObject main, confirm, jobs, designs, difficulty, settings, defeated, message, exitDungeon;
    Text bgmLabel, sfxLabel, messageLabel;
    bool bgm, sfx, resultShown;
    PlayerStats.PlayerClass pendingClass;
    GameObject[] Panels => new[] { main, confirm, jobs, designs, difficulty, settings, defeated, message };
    public bool IsDifficultySelectionVisible => difficulty != null && difficulty.activeSelf;
    public bool IsBossDefeatedVisible => defeated != null && defeated.activeSelf;

    public static DungeonFlowController Create(DungeonGenerator generator)
    {
        var old = FindAnyObjectByType<DungeonFlowController>();
        if (old != null) { old.generator = generator; return old; }
        var root = new GameObject("Dungeon Flow Controller", typeof(DungeonFlowController));
        var flow = root.GetComponent<DungeonFlowController>(); flow.generator = generator; flow.Initialize(); return flow;
    }

    void Initialize()
    {
        player = FindAnyObjectByType<PlayerController>();
        stats = player != null ? player.GetComponent<PlayerStats>() : null;
        inventory = player != null ? player.GetComponent<EquipmentInventory>() : null;
        bgm = PlayerPrefs.GetInt("Settings.BGM", 1) == 1; sfx = PlayerPrefs.GetInt("Settings.SFX", 1) == 1;
        EnsureEventSystem(); BuildCanvas(); ApplyAudio(); Show(main); Lock(true);
    }
    void OnEnable() => BossHealth.AnyBossDefeated += BossDefeated;
    void OnDisable() => BossHealth.AnyBossDefeated -= BossDefeated;
    void Start()
    {
        if (stats != null) stats.LeveledUp += OnLevelUp;
        if (inventory != null) { inventory.InventoryChanged += SaveEvent; inventory.EquipmentChanged += SaveEvent; }
    }
    void OnDestroy()
    {
        if (stats != null) stats.LeveledUp -= OnLevelUp;
        if (inventory != null) { inventory.InventoryChanged -= SaveEvent; inventory.EquipmentChanged -= SaveEvent; }
    }
    void OnLevelUp(int _) => Save();
    void SaveEvent() => Save();
    void OnApplicationQuit() => Save();

    void NewGame() { if (GameSaveSystem.HasSave) Show(confirm); else FreshGame(); }
    void FreshGame() { GameSaveSystem.DeleteSave(); DungeonProgress.Reset(); inventory?.ResetToStartingEquipment(); Show(jobs); }
    void ContinueGame()
    {
        GameSaveData data = GameSaveSystem.Load();
        if (data == null) { messageLabel.text = "저장된 게임이 없습니다."; Show(message); return; }
        DungeonProgress.Apply(data); inventory?.ApplySave(data.equipment); stats?.ApplySave(data);
        player?.GetComponent<PlayerVisualController>()?.SetDesign(data.characterDesign);
        ShowDifficulty();
    }
    void ChooseClass(PlayerStats.PlayerClass c) { pendingClass = c; Show(designs); }
    void ChooseDesign(int index)
    {
        stats?.ResetForNewGame(pendingClass);
        player?.GetComponent<PlayerVisualController>()?.SetDesign(index);
        Save(true); ShowDifficulty();
    }
    public void SelectDifficulty(DungeonDifficulty d)
    {
        if (!DungeonProgress.IsUnlocked(d) || generator == null) return;
        resultShown = false; Hide(); exitDungeon.SetActive(false); Lock(false); generator.BeginDungeon(d); Save();
    }
    public void ContinueExploring()
    {
        if (!resultShown) return; defeated.SetActive(false); exitDungeon.SetActive(true); Lock(false);
    }
    public void ReturnToSelection()
    {
        defeated.SetActive(false); exitDungeon.SetActive(false); generator?.ExitDungeon(); Save(); ShowDifficulty();
    }
    void BossDefeated(BossHealth boss)
    {
        if (resultShown || boss == null || generator == null || !boss.transform.IsChildOf(generator.transform)) return;
        resultShown = true; DungeonProgress.MarkCleared(generator.Difficulty); Save(); RebuildDifficulties();
        defeated.SetActive(true); exitDungeon.SetActive(false); Lock(true);
    }
    void ShowDifficulty() { resultShown = false; RebuildDifficulties(); Show(difficulty); Lock(true); }
    void Save(bool force = false)
    {
        if (!force && !GameSaveSystem.HasSave) return;
        GameSaveSystem.Save(stats, inventory, generator != null ? generator.Difficulty : DungeonDifficulty.Easy);
    }
    void ToggleBgm() { bgm = !bgm; SaveAudio(); }
    void ToggleSfx() { sfx = !sfx; SaveAudio(); }
    void SaveAudio()
    {
        PlayerPrefs.SetInt("Settings.BGM", bgm ? 1 : 0); PlayerPrefs.SetInt("Settings.SFX", sfx ? 1 : 0);
        PlayerPrefs.Save(); ApplyAudio();
    }
    void ApplyAudio()
    {
        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            bool music = source.loop || source.name.ToLowerInvariant().Contains("bgm");
            source.mute = music ? !bgm : !sfx;
        }
        if (bgmLabel != null) bgmLabel.text = bgm ? "ON" : "OFF";
        if (sfxLabel != null) sfxLabel.text = sfx ? "ON" : "OFF";
    }
    void Quit()
    {
        Save();
#if UNITY_EDITOR
        Debug.Log("게임 종료 요청 확인 (Build에서는 Application.Quit 실행)");
#else
        Application.Quit();
#endif
    }

    void BuildCanvas()
    {
        var go = new GameObject("Dungeon RPG Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false); canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        main = Panel("Main Menu", new Vector2(1920, 1080)); Text(main, "DUNGEON RPG", 0, 280, 58);
        Button(main, "새 게임", 120, NewGame); Button(main, "이어하기", 35, ContinueGame);
        Button(main, "설정", -50, () => Show(settings)); Button(main, "게임 종료", -135, Quit);
        confirm = Panel("New Game Confirmation", new Vector2(720, 380));
        Text(confirm, "기존 저장 데이터가 있습니다.\n새 게임을 시작하면 기존 진행 상황을 덮어씁니다.", 0, 70, 24);
        Button(confirm, "새 게임 시작", -70, FreshGame, -160); Button(confirm, "취소", -70, () => Show(main), 160);
        jobs = Panel("Job Selection", new Vector2(620, 610)); Text(jobs, "직업 선택", 0, 230, 40);
        Button(jobs, "WARRIOR", 105, () => ChooseClass(PlayerStats.PlayerClass.Warrior));
        Button(jobs, "ARCHER", 20, () => ChooseClass(PlayerStats.PlayerClass.Archer));
        Button(jobs, "MAGE", -65, () => ChooseClass(PlayerStats.PlayerClass.Mage)); Button(jobs, "뒤로", -175, () => Show(main));
        designs = Panel("Character Design Selection", new Vector2(820, 620));
        Text(designs, "CHARACTER DESIGN", 0, 245, 40);
        PlayerVisualDatabase visualDatabase = Resources.Load<PlayerVisualDatabase>("PlayerVisualDatabase");
        for (int i = 0; i < 4; i++)
        {
            int selected = i;
            float x = -270 + i * 180;
            Button choice = Button(designs, $"DESIGN {i + 1}", -75, () => ChooseDesign(selected), x);
            choice.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 190);
            Image preview = CreateImage(choice.gameObject, new Vector2(0, 35), new Vector2(96, 108));
            if (visualDatabase != null && i < visualDatabase.designs.Length)
                preview.sprite = visualDatabase.designs[i].Preview;
            choice.GetComponentInChildren<Text>().rectTransform.anchoredPosition = new Vector2(0, -65);
        }
        Button(designs, "뒤로", -235, () => Show(jobs));
        difficulty = Panel("Difficulty Selection", new Vector2(650, 760)); Text(difficulty, "DUNGEON DIFFICULTY", 0, 305, 36);
        RebuildDifficulties(); Button(difficulty, "뒤로", -300, () => Show(main));
        settings = Panel("Settings", new Vector2(620, 500)); Text(settings, "설정", 0, 180, 40);
        Text(settings, "BGM", -110, 70, 26); bgmLabel = Button(settings, "ON", 70, ToggleBgm, 120).GetComponentInChildren<Text>();
        Text(settings, "SFX", -110, -10, 26); sfxLabel = Button(settings, "ON", -10, ToggleSfx, 120).GetComponentInChildren<Text>();
        Button(settings, "뒤로", -140, () => Show(main));
        defeated = Panel("Boss Defeated", new Vector2(720, 360)); Text(defeated, "BOSS DEFEATED", 0, 105, 38);
        Text(defeated, "던전을 더 탐사할까요?", 0, 35, 24);
        Button(defeated, "더 탐사하기", -80, ContinueExploring, -165); Button(defeated, "복귀", -80, ReturnToSelection, 165);
        message = Panel("Message", new Vector2(560, 260)); messageLabel = Text(message, "", 0, 45, 25);
        Button(message, "확인", -70, () => Show(main));
        exitDungeon = Button(canvas.gameObject, "던전 탐사 종료", 475, ReturnToSelection).gameObject;
        Hide(); exitDungeon.SetActive(false);
    }
    void RebuildDifficulties()
    {
        if (difficulty == null) return; Transform old = difficulty.transform.Find("Rows"); if (old != null) Destroy(old.gameObject);
        var rows = new GameObject("Rows", typeof(RectTransform)); rows.transform.SetParent(difficulty.transform, false);
        DungeonDifficulty[] ds = { DungeonDifficulty.Easy, DungeonDifficulty.Normal, DungeonDifficulty.Hard, DungeonDifficulty.Nightmare };
        for (int i = 0; i < ds.Length; i++)
        {
            DungeonDifficulty d = ds[i]; bool unlocked = DungeonProgress.IsUnlocked(d);
            string label = d.ToString().ToUpper() + (!unlocked ? "  [LOCKED]" : DungeonProgress.IsCleared(d) ? "  [CLEAR]" : "");
            Button(rows, label, 145 - i * 88, () => SelectDifficulty(d), 0, unlocked);
        }
    }
    void Show(GameObject panel) { Hide(); panel.SetActive(true); }
    void Hide() { foreach (GameObject panel in Panels) if (panel != null) panel.SetActive(false); }
    void Lock(bool value) { if (player == null) player = FindAnyObjectByType<PlayerController>(); player?.SetMovementLocked(value); }
    GameObject Panel(string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(canvas.transform, false);
        var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(.018f, .026f, .043f, 1f); return go;
    }
    static Text Text(GameObject parent, string value, float x, float y, int size)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent.transform, false);
        var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(700, 120);
        var t = go.GetComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.text = value; t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; return t;
    }
    static Button Button(GameObject parent, string label, float y, UnityEngine.Events.UnityAction action, float x = 0, bool enabled = true)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent.transform, false);
        var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(360, 64);
        go.GetComponent<Image>().color = enabled ? new Color(.14f, .36f, .56f) : new Color(.13f, .14f, .17f);
        var b = go.GetComponent<Button>(); b.interactable = enabled; b.onClick.AddListener(action);
        var t = Text(go, label, 0, 0, 22); t.rectTransform.sizeDelta = r.sizeDelta; if (!enabled) t.color = Color.gray; return b;
    }
    static Image CreateImage(GameObject parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject("Preview", typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent.transform, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        Image image = go.GetComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false; return image;
    }
    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
