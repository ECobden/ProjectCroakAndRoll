using UnityEngine;

/// <summary>
/// Grants lives to the die owner when a specific face value is rolled.
/// Example: triggerValue 6 and livesToAdd 1 gives +1 life whenever this die rolls a 6.
/// </summary>
[CreateAssetMenu(fileName = "GainLifeOnRollAbility_", menuName = "Croak and Roll/Abilities/Gain Life On Roll Ability", order = 8)]
public class GainLifeOnRollAbility : DiceAbility
{
    [SerializeField] private int triggerValue = 6;
    [SerializeField] private int livesToAdd = 1;

    public override void OnRoll(AbilityContext context)
    {
        if (context.instigator == null)
            return;

        if (livesToAdd == 0)
            return;

        if (context.diceValue != triggerValue)
        {
            Debug.Log($"{context.instigator.gameObject.name}'s {name} did not trigger. Rolled {context.diceValue}, needs {triggerValue}.");
            return;
        }

        context.instigator.AddLives(livesToAdd);
        context.gameManager?.UpdateLivesUI();

        int currentLives = context.instigator.GetCurrentLives();
        Debug.Log($"{context.instigator.gameObject.name}'s {name} triggered on {triggerValue}: {(livesToAdd > 0 ? "+" : string.Empty)}{livesToAdd} life. Current lives: {currentLives}");
    }
}
