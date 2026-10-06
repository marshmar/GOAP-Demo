/// <summary>
/// 둥지 복귀: 타겟을 놓치면 둥지(시작 위치)로 돌아간다
/// </summary>
public class ReturnHomeGoal : GoapGoal
{
    public ReturnHomeGoal() : base("ReturnHome")
    {
        AddDesiredState(DragonKeys.AtHome, true);
    }

    // 휴식(1)보다 높고 공격(2)보다 낮다. 타겟이 없을 때만 의미가 있다
    public override float GetPriority(WorldState currentState)
    {
        return currentState.Get(DragonKeys.HasTarget) ? 0 : 1.5f;
    }
}
