using System;
using UnityEngine;

public class CharaData : MonoBehaviour
{
    public AnimData[] standingRandomAnims;
    public AnimData[] standingOneShotAnims;
    public AnimData[] sittingRandomAnims;
    public AnimData[] sittingOneShotAnims;
    public AnimData[] draggedAnims;
    public AnimData strokedStandingAnim;
    public AnimData strokedSittingAnim;
    public AnimData pickedStandingAnim;
    public AnimData pickedSittingAnim;
    public AnimData jumpOutAnim;
    public AnimData jumpInAnim;
    public AnimData[] hideLeftAnims;
    public AnimData[] hideRightAnims;
    public AnimData alarmAnim;
}

[Serializable]
public class AnimData
{
    public string animName;
    public float time;
    public bool isArmIK;
}
