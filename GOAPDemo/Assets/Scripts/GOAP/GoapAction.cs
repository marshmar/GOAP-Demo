

public abstract class GoapAction
{
    private const float PlaceholderDuration = 1f;

    public string Name { get; }
    public float Cost { get; protected set; }
    public WorldState Preconditions { get; } = new WorldState();
    public WorldState Effects { get; } = new WorldState();

    protected GoapAction(string name, float cost)
    {
        Name = name;
        Cost = cost;
    }

    protected void AddPrecondition(string key, bool value)
    {
        Preconditions.Set(key, value);
    }

    protected void AddEffect(string key, bool value)
    {
        Effects.Set(key, value);
    }

    /// <summary>
    /// 주어진 상태(실제 또는 플래너가 생성한 상태)가 이 액션을 할 수 있는지
    /// </summary>
    public bool CanRun(WorldState state)
    {
        return state.Satisfies(Preconditions);
    }

    /// <summary>
    /// 이 액션을 했다고 가정한 새 상태를 반환(원본은 그대로)
    /// </summary>
    public WorldState Simulate(WorldState state)
    {
        return state.Apply(Effects);
    }

    /// <summary>
    /// 실행 중에 더 중요한 목표가 생겼을 때 끊을 수 있는지. 공격 모션처럼 끝까지 보여줘야 하면 false로 재정의
    /// </summary>
    public virtual bool CanInterrupt => true;

    public virtual void OnEnter(GoapAgent agent)
    {
    }

    /// <summary>
    /// 실행 중 매 프레임 호출. 기본 구현은 실행 코드가 아직 없는 액션용 임시 처리(1초 뒤 성공)
    /// </summary>
    public virtual ActionStatus OnTick(GoapAgent agent)
    {
        return agent.CurrentActionTime >= PlaceholderDuration ? ActionStatus.Success : ActionStatus.Running;
    }

    /// <summary>
    /// 액션이 끝날 때 호출 (성공, 실패, 중단 모두)
    /// </summary>
    public virtual void OnExit(GoapAgent agent)
    {
    }

    public override string ToString()
    {
        return $"{Name} (Cost: {Cost})";
    }
}
