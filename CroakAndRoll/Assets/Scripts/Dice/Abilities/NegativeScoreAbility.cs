using UnityEngine;

/// <summary>
/// Makes the die's rolled value contribute as a negative score instead of positive.
/// For example, rolling a 5 will subtract 5 points instead of adding 5.
/// </summary>
[CreateAssetMenu(fileName = "NegativeScoreAbility_", menuName = "Croak and Roll/Abilities/Negative Score Ability", order = 6)]
public class NegativeScoreAbility : DiceAbility
{
    public override int OnScore(AbilityContext context)
    {
        // The dice value is already added to the score by default
        // To make it negative, we need to subtract twice the value
        // Example: rolled 5 -> default +5, we return -10 -> final result: 5 + (-10) = -5
        int modifier = -2 * context.diceValue;
        
        Debug.Log($"{context.instigator.gameObject.name}'s {name} turns {context.diceValue} into negative ({modifier} modifier).");
        
        return modifier;
    }
}
