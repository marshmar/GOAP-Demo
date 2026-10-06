/// <summary>
/// 드래곤 액션의 공통 부모. 실행 메서드에서 GoapAgent를 DragonAgent로 바꿔서 넘겨준다
/// </summary>
public abstract class DragonAction : GoapAction
{
    protected DragonAction(string name, float cost) : base(name, cost)
    {
    }

    public override void OnEnter(GoapAgent agent)
    {
        Enter((DragonAgent)agent);
    }

    public override ActionStatus OnTick(GoapAgent agent)
    {
        return Tick((DragonAgent)agent);
    }

    public override void OnExit(GoapAgent agent)
    {
        Exit((DragonAgent)agent);
    }

    protected virtual void Enter(DragonAgent dragon)
    {
    }

    protected abstract ActionStatus Tick(DragonAgent dragon);

    protected virtual void Exit(DragonAgent dragon)
    {
    }
}
