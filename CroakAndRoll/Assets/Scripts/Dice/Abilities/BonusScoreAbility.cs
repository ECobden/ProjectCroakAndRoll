using UnityEngine;

/// <summary>
/// Adds a fixed bonus amount on top of the die's rolled value during scoring.
/// For example, if bonus is +3 and you roll a 4, your total contribution is 7 (4 + 3).
/// </summary>
[CreateAssetMenu(fileName = "BonusScoreAbility_", menuName = "Croak and Roll/Abilities/Bonus Score Ability", order = 7)]
public class BonusScoreAbility : DiceAbility
{
    [SerializeField] private int bonusAmount = 2;

    public override int OnScore(AbilityContext context)
    {
        if (bonusAmount == 0)
            return 0;

        Debug.Log($"{context.instigator.gameObject.name}'s {name} adds +{bonusAmount} bonus (rolled {context.diceValue}).");
        
        return bonusAmount;
    }
}
