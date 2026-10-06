public class ClawAction : MeleeAttackAction
{
    public ClawAction() : base("Claw", 1, "Claw", hitTime: 0.4f, damage: 12f)
    {
        AddPrecondition(DragonKeys.InMeleeRange, true);
        AddPrecondition(DragonKeys.IsFlying, false);
        AddPrecondition(DragonKeys.ClawReady, true);

        AddEffect(DragonKeys.TargetDamaged, true);
        AddEffect(DragonKeys.ClawReady, false);
    }
}
