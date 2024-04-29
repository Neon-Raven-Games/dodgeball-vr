using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerName", menuName = "AI Character", order = 1)]
public class CharacterData : ScriptableObject
{
    public CharacterData()
    {

    }

    public string characterName = "";
    public Texture2D playerPortrait;

    [Space(10)]
    [Header("CUSTOMIZER DATA")]
    //public CustomizerItem body;
   
    [Space(10)]
    [Header("STATS")]
    public float runSpeed = 5f;
    public float throwSpeed = 30;
    public float punchPower = 12;
    [Range(0, 1)]
    public float fumbleChance = 0;

    public const float chestHeight = 1.5f;
    public const float armLength = 1;
}