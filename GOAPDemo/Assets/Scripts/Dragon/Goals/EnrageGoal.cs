using System;

public class EnrageGoal : GoapGoal
{
    public const float EnrageHpRatio = 0.5f;

    // 현재 HP 비율(0~1)을 물어보는 함수. 목표가 HP 컴포넌트를 직접 알 필요가 없도록 함
    private readonly Func<float> getHpRatio;

    public EnrageGoal(Func<float> getHpRatio) : base("Enrage")
    {
        this.getHpRatio = getHpRatio;
        AddDesiredState(DragonKeys.IsEnraged, true);
    }

    public override float GetPriority(WorldState currentState)
    {
        return getHpRatio() <= EnrageHpRatio ? 3 : 0;
    }
}
