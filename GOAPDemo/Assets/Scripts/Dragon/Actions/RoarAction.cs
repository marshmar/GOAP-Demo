public class RoarAction : AnimationAction
{
    public RoarAction() : base("Roar", 1, "Roar")
    {
        AddPrecondition(DragonKeys.IsFlying, false);
        AddPrecondition(DragonKeys.IsEnraged, false);
        AddPrecondition(DragonKeys.LowHealth, true);

        AddEffect(DragonKeys.IsEnraged, true);
    }

    protected override void OnProgress(DragonAgent dragon, float progress)
    {
        dragon.FaceTarget();
    }
}
