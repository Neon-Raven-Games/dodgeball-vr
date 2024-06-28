using System.Collections.Generic;
using UnityEngine;

public class MouthData : ScriptableObject
{
    public Expressions expression;
    public MouthSprites spriteIndex;
    public List<Sprite> mouthSprites;
}