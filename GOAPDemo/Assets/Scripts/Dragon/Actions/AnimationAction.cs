using UnityEngine;

/// <summary>
/// Animator 상태 하나를 재생하고, 그 상태가 끝나면 성공하는 액션의 공통 부모 (공격, 포효, 이착륙)
/// </summary>
public abstract class AnimationAction : DragonAction
{
    // 상태 이름이 틀렸을 때 무한히 기다리지 않도록, 이 시간 안에 재생이 시작되지 않으면 실패
    private const float StartTimeout = 1f;
    // Exit Time 전이가 없어 상태를 못 벗어나는 경우를 대비한 안전장치
    private const float MaxProgress = 1.5f;

    private readonly string stateName;
    private bool started;

    protected AnimationAction(string name, float cost, string stateName) : base(name, cost)
    {
        this.stateName = stateName;
    }

    // 모션 중간에 끊기면 어색하므로 끝까지 재생한다
    public override bool CanInterrupt => false;

    protected override void Enter(DragonAgent dragon)
    {
        started = false;
        dragon.StopMoving();
        dragon.PlayAnimation(stateName, restart: true);
    }

    protected override ActionStatus Tick(DragonAgent dragon)
    {
        // Animator는 이번 프레임의 Update가 끝난 뒤에 전환을 반영하므로 첫 프레임은 건너뛴다
        if (dragon.CurrentActionTime <= 0f)
        {
            return ActionStatus.Running;
        }

        float progress = dragon.GetAnimationProgress(stateName);

        // 재생이 시작된 뒤 상태를 벗어났으면(Exit Time 전이 완료) 끝난 것
        if (progress < 0f)
        {
            if (started)
            {
                return ActionStatus.Success;
            }
            return dragon.CurrentActionTime > StartTimeout ? ActionStatus.Failure : ActionStatus.Running;
        }

        started = true;
        OnProgress(dragon, Mathf.Min(progress, 1f));
        return progress >= MaxProgress ? ActionStatus.Success : ActionStatus.Running;
    }

    /// <summary>
    /// 재생 중 매 프레임 호출. progress는 0(시작) ~ 1(끝). 타격 타이밍 등을 여기서 처리한다
    /// </summary>
    protected virtual void OnProgress(DragonAgent dragon, float progress)
    {
    }
}
