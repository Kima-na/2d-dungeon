#if UNITY_EDITOR
using UnityEditor;

public static class GoblinWarriorSpriteSetup
{
    [MenuItem("Tools/2D Dungeon/Setup Goblin Warrior Sprite")]
    public static void Setup() => GoblinFamilySpriteSetup.Setup();
}
#endif
