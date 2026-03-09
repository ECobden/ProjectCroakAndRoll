using UnityEngine;

/// <summary>
/// Changes the die to display a specific face value.
/// This ability physically rotates the die and updates its value.
/// Useful for "lucky dice" that always roll a certain number, or cursed dice locked to a value.
/// </summary>
[CreateAssetMenu(fileName = "SetFaceValueAbility_", menuName = "Croak and Roll/Abilities/Set Face Value Ability", order = 8)]
public class SetFaceValueAbility : DiceAbility
{
    [SerializeField] [Range(1, 6)] private int targetFaceValue = 6;
    [SerializeField] private bool useAnimation = true;

    public override void OnRoll(AbilityContext context)
    {
        // Validate we have the necessary references
        if (context.gameManager == null)
        {
            Debug.LogWarning($"{name}: Cannot set face value - no game manager reference.");
            return;
        }

        // Use the active DiceManager owned by this GameManager.
        DB_DiceManager diceManager = context.gameManager.GetDiceManager();
        if (diceManager == null)
        {
            Debug.LogWarning($"{name}: Cannot find active DB_DiceManager on game manager.");
            return;
        }

        // Get the appropriate dice controller based on die index
        DB_DiceController diceController = null;
        if (context.dieIndex == 0)
        {
            diceController = diceManager.GetDiceControllerA();
            if (diceController == null)
                diceController = diceManager.GetLastRolledDiceControllerA();
        }
        else if (context.dieIndex == 1)
        {
            diceController = diceManager.GetDiceControllerB();
            if (diceController == null)
                diceController = diceManager.GetLastRolledDiceControllerB();
        }

        if (diceController == null)
        {
            Debug.LogWarning($"{name}: Cannot find dice controller for die index {context.dieIndex}.");
            return;
        }

        // Change the die to the target face value
        if (useAnimation)
        {
            // Use animated flip
            diceController.FlipToOppositeFace(targetFaceValue);
            Debug.Log($"{context.instigator.gameObject.name}'s {name} changed die {context.dieIndex} from {context.diceValue} to {targetFaceValue} (animated).");
        }
        else
        {
            // Instant rotation change
            diceController.SetRotationForFaceValue(targetFaceValue);
            Debug.Log($"{context.instigator.gameObject.name}'s {name} changed die {context.dieIndex} from {context.diceValue} to {targetFaceValue} (instant).");
        }

        // Face value is changed after the scoring row callback path, so refresh score UI explicitly.
        context.gameManager.RefreshScoreDisplays();
    }

    public override int OnScore(AbilityContext context)
    {
        // Keep scoring in sync with the forced face value.
        // Base score already includes context.diceValue, so return the delta.
        return targetFaceValue - context.diceValue;
    }
}
