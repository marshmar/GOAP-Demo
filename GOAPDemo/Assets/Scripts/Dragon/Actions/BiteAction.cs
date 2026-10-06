public class BiteAction : MeleeAttackAction
{
    public BiteAction() : base("Bite", 4, "Bite", hitTime: 0.4f, damage: 8f)
    {
        AddPrecondition(DragonKeys.InMeleeRange, true);
        AddPrecondition(DragonKeys.IsFlying, false);

        AddEffect(DragonKeys.TargetDamaged, true);
    }
}
