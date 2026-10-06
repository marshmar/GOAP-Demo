/// <summary>
/// 걸어서 귀환: 둥지까지 천천히 걸어가서 처음 바라보던 방향으로 돌아서면 성공
/// </summary>
public class WalkHomeAction : DragonAction
{
    public WalkHomeAction() : base("WalkHome", 4)
    {
        AddPrecondition(DragonKeys.IsFlying, false);
        AddPrecondition(DragonKeys.GaveUpChase, true);

        AddEffect(DragonKeys.AtHome, true);
    }

    protected override void Enter(DragonAgent dragon)
    {
        dragon.PlayAnimation("Locomotion");
    }

    protected override ActionStatus Tick(DragonAgent dragon)
    {
        return dragon.MoveHome(walk: true) ? ActionStatus.Success : ActionStatus.Running;
    }

    protected override void Exit(DragonAgent dragon)
    {
        dragon.StopMoving();
    }
}
