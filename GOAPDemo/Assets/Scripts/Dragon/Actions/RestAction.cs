/// <summary>
/// 대기(휴식): 잠자는 모션을 계속 재생한다. 스스로 끝나지 않고, 타겟을 발견하는 등 더 중요한 목표가 생기면 중단된다
/// </summary>
public class RestAction : DragonAction
{
    public RestAction() : base("Rest", 1)
    {
        AddPrecondition(DragonKeys.IsFlying, false);

        AddEffect(DragonKeys.IsResting, true);
    }

    protected override void Enter(DragonAgent dragon)
    {
        dragon.StopMoving();
        dragon.PlayAnimation("Rest");
    }

    protected override ActionStatus Tick(DragonAgent dragon)
    {
        return ActionStatus.Running;
    }
}
