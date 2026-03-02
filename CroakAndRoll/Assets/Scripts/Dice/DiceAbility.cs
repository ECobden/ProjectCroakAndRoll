using UnityEngine;

/// <summary>
/// Base class for dice abilities.
/// Each die can have special abilities that trigger when rolled or matched.
/// Examples: SwapAbility, MoneyAbility, ExtraRollAbility, etc.
/// </summary>
public abstract class DiceAbility : ScriptableObject
{
    public string abilityName;
    public string description;

    /// <summary>
    /// Execute this ability when triggered.
    /// </summary>
    /// <param name="instigator">The participant who triggered the ability</param>
    /// <param name="opponent">The opposing participant</param>
    /// <param name="diceValue">The value of the die that triggered this ability</param>
    public abstract void Execute(Participant instigator, Participant opponent, int diceValue);

    /// <summary>
    /// Get a display name for this ability.
    /// </summary>
    public virtual string GetDisplayName()
    {
        return abilityName;
    }
}

/// <summary>
/// Ability that allows swapping a die with opponent's die.
/// </summary>
[CreateAssetMenu(fileName = "SwapAbility_", menuName = "Croak and Roll/Abilities/Swap Ability", order = 1)]
public class SwapAbility : DiceAbility
{
    public override void Execute(Participant instigator, Participant opponent, int diceValue)
    {
        Debug.Log($"{instigator.gameObject.name} can swap a die! (triggered by value {diceValue})");
        // TODO: Implement swap logic - show UI for selecting opponent die to swap with
    }
}

/// <summary>
/// Ability that grants money based on roll value.
/// </summary>
[CreateAssetMenu(fileName = "MoneyAbility_", menuName = "Croak and Roll/Abilities/Money Ability", order = 2)]
public class MoneyAbility : DiceAbility
{
    [SerializeField] private int moneyPerPoint = 1;

    public override void Execute(Participant instigator, Participant opponent, int diceValue)
    {
        int earnedMoney = diceValue * moneyPerPoint;
        instigator.AddMoney(earnedMoney);
        Debug.Log($"{instigator.gameObject.name} earned {earnedMoney} from MoneyAbility!");
    }
}

/// <summary>
/// Ability that grants an extra roll.
/// </summary>
[CreateAssetMenu(fileName = "ExtraRollAbility_", menuName = "Croak and Roll/Abilities/Extra Roll Ability", order = 3)]
public class ExtraRollAbility : DiceAbility
{
    public override void Execute(Participant instigator, Participant opponent, int diceValue)
    {
        Debug.Log($"{instigator.gameObject.name} gets an extra roll! (triggered by value {diceValue})");
        // TODO: Implement extra roll logic - notify GameManager to allow another roll this turn
    }
}

/// <summary>
/// Ability that steals points from opponent.
/// </summary>
[CreateAssetMenu(fileName = "StealPointsAbility_", menuName = "Croak and Roll/Abilities/Steal Points Ability", order = 4)]
public class StealPointsAbility : DiceAbility
{
    [SerializeField] private int pointsToSteal = 5;

    public override void Execute(Participant instigator, Participant opponent, int diceValue)
    {
        Debug.Log($"{instigator.gameObject.name} steals {pointsToSteal} points from {opponent.gameObject.name}!");
        // TODO: Implement point stealing logic
    }
}

/// <summary>
/// Ability that doubles the roll value.
/// </summary>
[CreateAssetMenu(fileName = "DoubleValueAbility_", menuName = "Croak and Roll/Abilities/Double Value Ability", order = 5)]
public class DoubleValueAbility : DiceAbility
{
    public override void Execute(Participant instigator, Participant opponent, int diceValue)
    {
        int doubledValue = diceValue * 2;
        Debug.Log($"{instigator.gameObject.name} doubled their roll! {diceValue} -> {doubledValue}");
        // TODO: Implement doubling logic - modify the score calculation
    }
}
