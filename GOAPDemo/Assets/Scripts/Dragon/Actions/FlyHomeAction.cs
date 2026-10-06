/// <summary>
/// 날아서 귀환: 공중에 뜬 채로 둥지까지 날아간다. 착지는 휴식 목표가 이어서 처리한다
/// </summary>
public class FlyHomeAction : DragonAction
{
    // Animator에 FlyForward 상태가 없으면 FlyIdle로 대신한다
    private const string FlyForwardState = "FlyForward";

    public FlyHomeAction() : base("FlyHome", 1)
    {
        AddPrecondition(DragonKeys.IsFlying, true);
        AddPrecondition(DragonKeys.GaveUpChase, true);

        AddEffect(DragonKeys.AtHome, true);
    }

    protected override void Enter(DragonAgent dragon)
    {
        dragon.PlayAnimation(dragon.HasAnimation(FlyForwardState) ? FlyForwardState : "FlyIdle");
    }

    protected override ActionStatus Tick(DragonAgent dragon)
    {
        // 둥지 위에 도착하면 제자리 비행으로 바꾸고 방향만 맞춘다
        if (dragon.IsNearHome)
        {
            dragon.PlayAnimation("FlyIdle");
        }
        return dragon.MoveHome(walk: false) ? ActionStatus.Success : ActionStatus.Running;
    }

    protected override void Exit(DragonAgent dragon)
    {
        dragon.StopMoving();
        dragon.PlayAnimation("FlyIdle");
    }
}
