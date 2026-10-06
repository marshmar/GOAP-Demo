/// <summary>
/// 경계: 타겟을 놓친 자리에서 마지막으로 본 쪽을 노려보며 잠시 기다린다 (다크소울식 어그로 해제)
/// </summary>
public class WatchAction : DragonAction
{
    public WatchAction() : base("Watch", 1)
    {
        AddPrecondition(DragonKeys.HasTarget, false);
        AddPrecondition(DragonKeys.GaveUpChase, false);

        AddEffect(DragonKeys.GaveUpChase, true);
    }

    protected override void Enter(DragonAgent dragon)
    {
        dragon.StopMoving();
        dragon.PlayAnimation(dragon.IsFlying ? "FlyIdle" : "Locomotion");
    }

    // 일정 시간 노려보면(센서 GaveUpChase) 포기하고 성공
    protected override ActionStatus Tick(DragonAgent dragon)
    {
        dragon.KeepWatching();
        return dragon.HasGivenUp ? ActionStatus.Success : ActionStatus.Running;
    }
}
