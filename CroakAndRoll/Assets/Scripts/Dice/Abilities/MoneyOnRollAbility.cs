using UnityEngine;

/// <summary>
/// Grants money to the ability owner based on the rolled die value.
/// </summary>
[CreateAssetMenu(fileName = "MoneyOnRollAbility_", menuName = "Croak and Roll/Abilities/Money On Roll Ability", order = 2)]
public class MoneyOnRollAbility : DiceAbility
{
    [SerializeField] private int moneyPerPoint = 1;

    public override void OnRoll(AbilityContext context)
    {
        if (context.instigator == null)
            return;

        int rolledValue = Mathf.Max(0, context.diceValue);
        int earnedMoney = rolledValue * moneyPerPoint;

        if (earnedMoney <= 0)
            return;

        context.instigator.AddMoney(earnedMoney);
        Debug.Log($"{context.instigator.gameObject.name} earned {earnedMoney} money from {name} (roll: {rolledValue}).");
    }
}
