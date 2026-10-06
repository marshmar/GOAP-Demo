public abstract class GoapGoal
{
    public string Name { get; }
    public WorldState DesiredState { get; } = new WorldState();

    protected GoapGoal(string name)
    {
        Name = name;
    }

    protected void AddDesiredState(string key, bool value)
    {
        DesiredState.Set(key, value);
    }

    // 현재 상황에서 이 목표를 얼마나 원하는지. 0이면 비활성
    public abstract float GetPriority(WorldState currentState);

    // 이미 원하는 상태인지. 달성된 목표는 선택하지 않기 위해 사용
    public bool IsSatisfied(WorldState currentState)
    {
        return currentState.Satisfies(DesiredState);
    }

    public override string ToString()
    {
        return Name;
    }
}
