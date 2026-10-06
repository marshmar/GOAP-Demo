public class FlyBreathAction : BreathAttackAction
{
    public FlyBreathAction() : base("FlyBreath", 1, "FlyBreath", damagePerSecond: 15f, limitRange: false)
    {
        AddPrecondition(DragonKeys.IsFlying, true);
        AddPrecondition(DragonKeys.BreathReady, true);
        AddPrecondition(DragonKeys.HasTarget, true);
        AddPrecondition(DragonKeys.IsEnraged, true);

        AddEffect(DragonKeys.TargetDamaged, true);
        AddEffect(DragonKeys.BreathReady, false);
    }
}
