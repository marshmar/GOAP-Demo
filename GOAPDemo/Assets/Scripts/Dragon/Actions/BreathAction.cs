public class BreathAction : BreathAttackAction
{
    public BreathAction() : base("Breath", 2, "Breath", damagePerSecond: 20f, limitRange: true)
    {
        AddPrecondition(DragonKeys.InBreathRange, true);
        AddPrecondition(DragonKeys.IsFlying, false);
        AddPrecondition(DragonKeys.BreathReady, true);

        AddEffect(DragonKeys.TargetDamaged, true);
        AddEffect(DragonKeys.BreathReady, false);
    }
}
