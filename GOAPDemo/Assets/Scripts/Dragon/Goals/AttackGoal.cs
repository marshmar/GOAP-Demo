public class AttackGoal : GoapGoal
{
    public AttackGoal() : base("Attack")
    {
        AddDesiredState(DragonKeys.TargetDamaged, true);
    }

    public override float GetPriority(WorldState currentState)
    {
        return currentState.Get(DragonKeys.HasTarget) ? 2 : 0;
    }
}
