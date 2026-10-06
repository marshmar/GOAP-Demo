public class LandAction : AnimationAction
{
    public LandAction() : base("Land", 2, "Land")
    {
        AddPrecondition(DragonKeys.IsFlying, true);
        AddPrecondition(DragonKeys.IsEnraged, true);

        AddEffect(DragonKeys.IsFlying, false);
    }

    protected override void Enter(DragonAgent dragon)
    {
        base.Enter(dragon);
        dragon.SetFlying(false);
    }
}
