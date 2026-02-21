using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that defines the properties of a die type.
/// Contains face values, abilities, and other die-specific data.
/// Create instances of this in the Unity Editor to define different die types.
/// </summary>
[CreateAssetMenu(fileName = "DieData_", menuName = "Croak and Roll/Die Data", order = 1)]
public class DieData : ScriptableObject
{
    [Header("Die Identity")]
    [SerializeField] public string dieName = "Standard Die";
    [SerializeField] public string description = "A standard six-sided die";
    [SerializeField] public Sprite dieIcon;
    [SerializeField] public Color dieColor = Color.white;

    [Header("Face Values")]
    [SerializeField] public int[] faceValues = new int[6] { 1, 2, 3, 4, 5, 6 };

    [Header("Abilities")]
    [SerializeField] public List<DiceAbility> abilities = new List<DiceAbility>();

    [Header("Rarity & Progression")]
    [SerializeField] public int rarity = 1; // 1-5, affects cost in shop
    [SerializeField] public int cost = 100; // Cost to purchase in shop

    /// <summary>
    /// Get the value of a specific face.
    /// </summary>
    public int GetFaceValue(int faceIndex)
    {
        if (faceIndex >= 0 && faceIndex < faceValues.Length)
            return faceValues[faceIndex];
        return 1; // Default fallback
    }

    /// <summary>
    /// Get all face values for this die.
    /// </summary>
    public int[] GetAllFaceValues()
    {
        return (int[])faceValues.Clone();
    }

    /// <summary>
    /// Check if this die has any abilities.
    /// </summary>
    public bool HasAbilities()
    {
        return abilities != null && abilities.Count > 0;
    }

    /// <summary>
    /// Get all abilities for this die.
    /// </summary>
    public List<DiceAbility> GetAbilities()
    {
        return new List<DiceAbility>(abilities);
    }

    /// <summary>
    /// Check if die has a specific ability type.
    /// </summary>
    public bool HasAbilityOfType<T>() where T : DiceAbility
    {
        foreach (var ability in abilities)
        {
            if (ability is T)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Get the first ability of a specific type.
    /// </summary>
    public T GetAbilityOfType<T>() where T : DiceAbility
    {
        foreach (var ability in abilities)
        {
            if (ability is T typedAbility)
                return typedAbility;
        }
        return null;
    }

    /// <summary>
    /// Validate that face values are properly set.
    /// </summary>
    public bool IsValid()
    {
        return faceValues != null && faceValues.Length == 6 && dieName != "";
    }
}
