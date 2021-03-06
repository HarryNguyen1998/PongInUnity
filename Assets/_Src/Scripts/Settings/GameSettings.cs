using System;
using UnityEngine;

[CreateAssetMenu]
public class GameSettings : ScriptableObject
{
    [Header("Animation settings")]
    public Material LeftPadMat;
    public Material RightPadMat;
    public float AnimationCycle;
    public GameplaySettingsPOD DefaultSettings = new GameplaySettingsPOD(true, true, 3, 16, 18);

    public float WaitTimeBetweenRound
    {
        get
        {
            return AnimationCycle * 4;
        }
    }

}
