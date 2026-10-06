public class RestGoal : GoapGoal
{
    public RestGoal() : base("Rest")
    {
        AddDesiredState(DragonKeys.IsResting, true);
    }

    public override float GetPriority(WorldState currentState)
    {
        return currentState.Get(DragonKeys.HasTarget) ? 0 : 1;
    }
}
