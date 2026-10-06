using UnityEngine;

/// <summary>
/// 브레스(지상, 공중) 공통. 불을 뿜는 구간 동안 VFX를 켜고, 정면 부채꼴 안의 타겟에게 초당 피해를 준다
/// </summary>
public abstract class BreathAttackAction : AnimationAction
{
    private readonly float damagePerSecond;
    private readonly bool limitRange;
    private readonly float flameStart;
    private readonly float flameEnd;

    /// <param name="limitRange">false면 거리 제한 없이 방향만 본다 (공중 브레스)</param>
    /// <param name="flameStart">애니메이션 진행도 기준으로 불이 나오기 시작하는 지점</param>
    /// <param name="flameEnd">불이 멈추는 지점</param>
    protected BreathAttackAction(string name, float cost, string stateName, float damagePerSecond, bool limitRange,
        float flameStart = 0.2f, float flameEnd = 0.65f)
        : base(name, cost, stateName)
    {
        this.damagePerSecond = damagePerSecond;
        this.limitRange = limitRange;
        this.flameStart = flameStart;
        this.flameEnd = flameEnd;
    }

    protected override void OnProgress(DragonAgent dragon, float progress)
    {
        bool flaming = progress >= flameStart && progress <= flameEnd;
        dragon.SetBreathVfx(flaming);

        if (!flaming)
        {
            if (progress < flameStart)
            {
                dragon.FaceTarget();
            }
            return;
        }

        // 불을 뿜는 동안에는 천천히 따라가서, 옆으로 피할 여지를 준다
        dragon.FaceTarget(0.3f);
        if (dragon.IsTargetInBreathCone(limitRange))
        {
            dragon.DealDamage(damagePerSecond * Time.deltaTime);
        }
    }

    protected override void Exit(DragonAgent dragon)
    {
        dragon.SetBreathVfx(false);
    }
}
