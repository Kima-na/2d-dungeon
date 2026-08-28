using System;
using UnityEngine;

[Serializable]
public sealed class PlayerDesign
{
    public string displayName;
    public Sprite[] run = new Sprite[6];
    public Sprite[] stand = new Sprite[4];
    public Sprite[] dead = new Sprite[2];
    public Sprite Preview => stand != null && stand.Length > 0 ? stand[0] : null;
}

[CreateAssetMenu(menuName = "2D Dungeon/Player Visual Database", fileName = "PlayerVisualDatabase")]
public sealed class PlayerVisualDatabase : ScriptableObject
{
    public PlayerDesign[] designs = new PlayerDesign[4];
}
