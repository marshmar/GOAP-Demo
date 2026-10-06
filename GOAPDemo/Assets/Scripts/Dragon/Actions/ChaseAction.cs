/// <summary>
/// 추격: 타겟을 향해 달려가서 근접 사거리에 들어오면 성공
/// </summary>
public class ChaseAction : DragonAction
{
    public ChaseAction() : base("Chase", 2)
    {
        AddPrecondition(DragonKeys.HasTarget, true);
        AddPrecondition(DragonKeys.IsFlying, false);

        AddEffect(DragonKeys.InMeleeRange, true);
        AddEffect(DragonKeys.InBreathRange, true);
    }

    protected override void Enter(DragonAgent dragon)
    {
        dragon.PlayAnimation("Locomotion");
    }

    protected override ActionStatus Tick(DragonAgent dragon)
    {
        if (!dragon.HasTarget)
        {
            return ActionStatus.Failure;
        }
        if (dragon.IsTargetInMeleeRange())
        {
            return ActionStatus.Success;
        }

        dragon.MoveTo(dragon.Target.position);
        return ActionStatus.Running;
    }

    protected override void Exit(DragonAgent dragon)
    {
        dragon.StopMoving();
    }
}
