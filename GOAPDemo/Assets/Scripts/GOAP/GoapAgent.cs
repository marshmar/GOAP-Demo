using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GOAP 실행기. 매 프레임 "감지 → 목표 선택 → 계획 → 액션 실행"을 반복한다.
/// 어떤 액션, 목표, 센서를 쓸지는 자식 클래스(DragonAgent)가 정한다.
/// </summary>
public abstract class GoapAgent : MonoBehaviour
{
    [SerializeField] private bool logPlans = true;

    private readonly List<GoapAction> actions = new List<GoapAction>();
    private readonly List<GoapGoal> goals = new List<GoapGoal>();
    private readonly Queue<GoapAction> plan = new Queue<GoapAction>();

    // 액션 결과로 생긴 사실(IsFlying, IsEnraged 등)을 기억. 센서 값은 매 프레임 이 위에 덮어쓴다
    private WorldState memory = new WorldState();
    private float actionStartTime;
    // 지금 계획을 세웠을 때(또는 마지막으로 다시 검토했을 때)의 상태. 바뀌면 더 싼 계획이 있는지 확인한다
    private string plannedSignature;

    public WorldState CurrentState { get; private set; } = new WorldState();
    public GoapGoal CurrentGoal { get; private set; }
    public GoapAction CurrentAction { get; private set; }
    public IEnumerable<GoapAction> RemainingPlan => plan;
    public IReadOnlyList<GoapGoal> Goals => goals;
    public float CurrentActionTime => Time.time - actionStartTime;

    protected abstract IEnumerable<GoapAction> CreateActions();
    protected abstract IEnumerable<GoapGoal> CreateGoals();

    /// <summary>
    /// 센서. 실제 게임 상황을 측정해서 sensed에 적는다 (매 프레임 호출)
    /// </summary>
    protected abstract void Sense(WorldState sensed);

    /// <summary>
    /// 액션이 성공했을 때 추가로 할 일 (예: 쿨타임 시작)
    /// </summary>
    protected virtual void OnActionSucceeded(GoapAction action)
    {
    }

    protected virtual void Awake()
    {
        actions.AddRange(CreateActions());
        goals.AddRange(CreateGoals());
    }

    protected virtual void Update()
    {
        UpdateState();
        UpdateGoal();
        RunAction();
    }

    protected virtual void OnDisable()
    {
        ClearPlan();
    }

    private void UpdateState()
    {
        var sensed = new WorldState();
        Sense(sensed);
        CurrentState = memory.Apply(sensed);
    }

    /// <summary>
    /// 지금 가장 원하는 목표가 진행 중인 목표와 다르면 다시 계획한다.
    /// 목표가 같아도 상황이 바뀌었으면 더 싼 계획이 생겼는지 확인한다
    /// </summary>
    private void UpdateGoal()
    {
        List<GoapGoal> candidates = GetCandidateGoals();
        GoapGoal best = candidates.Count > 0 ? candidates[0] : null;
        if (best == CurrentGoal)
        {
            if (CurrentGoal != null)
            {
                TryImprovePlan();
            }
            return;
        }

        // 끊을 수 없는 액션(공격 모션 등)이 진행 중이면 끝날 때까지 기다린다
        if (CurrentAction != null && !CurrentAction.CanInterrupt)
        {
            return;
        }

        Replan(candidates);
    }

    /// <summary>
    /// 우선순위가 높은 목표부터 계획을 시도해서 처음 성공한 계획으로 교체한다
    /// </summary>
    private void Replan(List<GoapGoal> candidates)
    {
        foreach (GoapGoal goal in candidates)
        {
            // 더 높은 목표가 전부 계획 불가능하면 진행 중인 계획을 그대로 유지
            if (goal == CurrentGoal)
            {
                return;
            }

            List<GoapAction> newPlan = GoapPlanner.Plan(CurrentState, goal, actions);
            if (newPlan == null)
            {
                continue;
            }

            StartPlan(goal, newPlan);
            return;
        }
        ClearPlan();
    }

    /// <summary>
    /// 우선순위가 0보다 크고 아직 달성되지 않은 목표를 우선순위 높은 순으로 반환
    /// </summary>
    private List<GoapGoal> GetCandidateGoals()
    {
        var candidates = new List<GoapGoal>();
        foreach (GoapGoal goal in goals)
        {
            if (goal.GetPriority(CurrentState) > 0 && !goal.IsSatisfied(CurrentState))
            {
                candidates.Add(goal);
            }
        }
        candidates.Sort((a, b) => b.GetPriority(CurrentState).CompareTo(a.GetPriority(CurrentState)));
        return candidates;
    }

    private void StartPlan(GoapGoal goal, List<GoapAction> newPlan)
    {
        ClearPlan();
        CurrentGoal = goal;
        plannedSignature = CurrentState.GetSignature();
        foreach (GoapAction action in newPlan)
        {
            plan.Enqueue(action);
        }
        Log($"목표 {goal.Name} → 계획 {GoapPlanner.Describe(newPlan)}");
    }

    /// <summary>
    /// 계획을 세운 뒤 상황이 바뀌었으면(쿨타임 종료, 사거리 진입 등) 같은 목표로 다시 계획해 보고,
    /// 남은 계획보다 확실히 싸면 교체한다. 예) Chase → Bite 도중 브레스가 준비되면 Breath로 전환
    /// </summary>
    private void TryImprovePlan()
    {
        string signature = CurrentState.GetSignature();
        if (signature == plannedSignature)
        {
            return;
        }

        // 끊을 수 없는 액션이 진행 중이면 끝난 뒤에 다시 확인한다 (signature를 갱신하지 않음)
        if (CurrentAction != null && !CurrentAction.CanInterrupt)
        {
            return;
        }
        plannedSignature = signature;

        List<GoapAction> newPlan = GoapPlanner.Plan(CurrentState, CurrentGoal, actions);
        if (newPlan == null || newPlan.Count == 0)
        {
            return;
        }

        // 비용이 같을 때 바꾸면 계획이 왔다 갔다 하므로, 확실히 쌀 때만 교체
        const float MinGain = 0.01f;
        if (PlanCost(newPlan) > RemainingCost() - MinGain)
        {
            return;
        }

        Log($"상황 변화 → 더 싼 계획 {GoapPlanner.Describe(newPlan)}");

        // 첫 액션이 지금 하던 액션과 같으면(예: 계속 Chase) 끊지 않고 이어서 진행
        int first = 0;
        if (CurrentAction != null && newPlan[0] == CurrentAction)
        {
            first = 1;
        }
        else if (CurrentAction != null)
        {
            EndCurrentAction();
        }

        plan.Clear();
        for (int i = first; i < newPlan.Count; i++)
        {
            plan.Enqueue(newPlan[i]);
        }
    }

    private float RemainingCost()
    {
        float cost = CurrentAction != null ? CurrentAction.Cost : 0f;
        foreach (GoapAction action in plan)
        {
            cost += action.Cost;
        }
        return cost;
    }

    private static float PlanCost(List<GoapAction> actionList)
    {
        float cost = 0f;
        foreach (GoapAction action in actionList)
        {
            cost += action.Cost;
        }
        return cost;
    }

    private void RunAction()
    {
        if (CurrentAction == null && !StartNextAction())
        {
            return;
        }

        ActionStatus status = CurrentAction.OnTick(this);
        if (status == ActionStatus.Running)
        {
            return;
        }

        GoapAction finished = CurrentAction;
        EndCurrentAction();

        if (status == ActionStatus.Success)
        {
            // 결과를 기억에 반영. IsFlying처럼 센서가 측정하지 않는 값은 이렇게 유지된다
            memory = memory.Apply(finished.Effects);
            OnActionSucceeded(finished);

            if (plan.Count == 0)
            {
                Log($"목표 {CurrentGoal.Name} 계획 완료");
                CurrentGoal = null;
            }
        }
        else
        {
            Log($"{finished.Name} 실패 → 다시 계획");
            ClearPlan();
        }
    }

    /// <summary>
    /// 계획에서 다음 액션을 꺼내 시작한다. 그사이 상황이 바뀌어 조건이 안 맞으면 계획을 버린다
    /// </summary>
    private bool StartNextAction()
    {
        if (plan.Count == 0)
        {
            return false;
        }

        GoapAction next = plan.Dequeue();
        if (!next.CanRun(CurrentState))
        {
            Log($"{next.Name}의 조건이 맞지 않음 → 다시 계획");
            ClearPlan();
            return false;
        }

        CurrentAction = next;
        actionStartTime = Time.time;
        CurrentAction.OnEnter(this);
        return true;
    }

    private void EndCurrentAction()
    {
        CurrentAction.OnExit(this);
        CurrentAction = null;
    }

    private void ClearPlan()
    {
        if (CurrentAction != null)
        {
            EndCurrentAction();
        }
        plan.Clear();
        CurrentGoal = null;
    }

    private void Log(string message)
    {
        if (logPlans)
        {
            Debug.Log($"[GOAP] {message}", this);
        }
    }
}
