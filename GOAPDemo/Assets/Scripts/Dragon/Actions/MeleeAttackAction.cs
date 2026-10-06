/// <summary>
/// 근접 공격(할퀴기, 물기) 공통. 타격 순간까지 타겟을 바라보고, 타격 순간에 사거리 안이면 피해를 준다
/// </summary>
public abstract class MeleeAttackAction : AnimationAction
{
    // 타격 순간에는 사거리를 조금 넉넉하게 봐서, 살짝 물러난 타겟도 맞게 한다
    private const float HitRangeTolerance = 1.3f;

    private readonly float hitTime;
    private readonly float damage;
    private bool hitDone;

    protected MeleeAttackAction(string name, float cost, string stateName, float hitTime, float damage)
        : base(name, cost, stateName)
    {
        this.hitTime = hitTime;
        this.damage = damage;
    }

    protected override void Enter(DragonAgent dragon)
    {
        base.Enter(dragon);
        hitDone = false;
    }

    protected override void OnProgress(DragonAgent dragon, float progress)
    {
        if (progress < hitTime)
        {
            dragon.FaceTarget();
            return;
        }

        if (hitDone)
        {
            return;
        }

        hitDone = true;
        if (dragon.IsTargetInMeleeRange(HitRangeTolerance))
        {
            dragon.DealDamage(damage);
        }
    }
}
