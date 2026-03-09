using UnityEngine;

/// <summary>
/// Base class for dice abilities.
/// Each die can have special abilities that trigger at different game lifecycle moments.
/// Abilities override specific lifecycle hooks (OnRoll, OnScore, OnTurnStart, etc.) to implement their effects.
/// Examples: SwapAbility, MoneyAbility, ExtraRollAbility, etc.
/// </summary>
public abstract class DiceAbility : ScriptableObject
{
    public string abilityName;
    public string description;

    #region Lifecycle Hooks
    /// <summary>
    /// Called when the die is rolled and lands on a value.
    /// Use this for immediate effects like granting money, triggering extra rolls, etc.
    /// </summary>
    public virtual void OnRoll(AbilityContext context) { }

    /// <summary>
    /// Called during score calculation. Can modify or return a score adjustment.
    /// Use this for abilities that affect scoring like doubling values or stealing points.
    /// </summary>
    /// <returns>Score modifier to add (positive or negative). Return 0 for no change.</returns>
    public virtual int OnScore(AbilityContext context) { return 0; }

    /// <summary>
    /// Called at the start of the instigator's turn (before any rolls).
    /// Use this for turn-start effects like buffs, preparations, or UI triggers.
    /// </summary>
    public virtual void OnParticipantTurnStart(AbilityContext context) { }

    /// <summary>
    /// Called at the end of the instigator's turn (after all rolls and actions).
    /// Use this for cleanup, end-of-turn effects, or cooldown resets.
    /// </summary>
    public virtual void OnParticipantTurnEnd(AbilityContext context) { }

    /// <summary>
    /// Called at the start of a new round (before any participant takes their turn).
    /// Use this for round-based cooldowns or recurring effects.
    /// </summary>
    public virtual void OnRoundStart(AbilityContext context) { }

    /// <summary>
    /// Called at the end of a round (after all participants have finished their turns).
    /// Use this for round cleanup or end-of-round scoring effects.
    /// </summary>
    public virtual void OnRoundEnd(AbilityContext context) { }

    /// <summary>
    /// Called when the instigator busts (goes over 21).
    /// Use this for bust-related penalties or reactions.
    /// </summary>
    public virtual void OnBust(AbilityContext context) { }

    /// <summary>
    /// Called when the opponent busts (goes over 21).
    /// Use this for opponent-bust bonuses or reactions.
    /// </summary>
    public virtual void OnOpponentBust(AbilityContext context) { }
    #endregion

}


