using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a single opponent encounter configuration.
/// </summary>
[CreateAssetMenu(fileName = "OpponentProfile_", menuName = "Croak and Roll/Opponent Profile", order = 20)]
public class OpponentProfileData : ScriptableObject
{
    [Header("Identity")]
    public string opponentName = "House";

    [Header("Lives")]
    [Min(1)] public int lives = 3;

    [Header("House AI")]
    [Min(1)] public int standValue = 17;
    [Range(0f, 1f)] public float cautiousness = 0.7f;
    [Min(1)] public int safeThreshold = 17;

    [Header("Dice Bag")]
    public List<DieData> startingDice = new List<DieData>();
}
