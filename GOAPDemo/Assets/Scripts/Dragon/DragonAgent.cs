using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 드래곤 GOAP 에이전트.
/// 액션과 목표를 등록하고, 센서로 월드 상태를 측정하며, 액션들이 쓰는 몸 동작(이동, 애니메이션, 공격)을 제공한다
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class DragonAgent : GoapAgent
{
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int DieState = Animator.StringToHash("Die");
    private const float CrossFadeTime = 0.15f;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float detectRange = 30f;
    [SerializeField] private float breathRange = 15f;
    [SerializeField] private float meleeRange = 6f;
    [SerializeField] private float breathAngle = 35f;

    [Header("Movement (시작할 때 NavMeshAgent에 적용)")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float turnSpeed = 240f;

    [Header("Home (둥지 = 시작 위치)")]
    [Tooltip("둥지에서 이 거리 밖에 있는 타겟은 쫓지 않는다")]
    [SerializeField] private float territoryRadius = 40f;
    [Tooltip("타겟을 놓친 뒤 그 자리에서 노려보는 시간")]
    [SerializeField] private float giveUpTime = 3f;
    [SerializeField] private float walkHomeSpeed = 2.5f;
    [SerializeField] private float homeRadius = 2f;

    [Header("Flight")]
    [Tooltip("비행 중 모델을 추가로 띄울 높이. 비행 애니메이션에 이미 높이가 있으면 0")]
    [SerializeField] private float flyHeight = 0f;
    [SerializeField] private float flyHeightSpeed = 4f;

    [Header("Cooldown")]
    [SerializeField] private Cooldown clawCooldown = new Cooldown(4f);
    [SerializeField] private Cooldown breathCooldown = new Cooldown(8f);

    [Header("Breath VFX")]
    [Tooltip("불 파티클. Project 창의 프리팹을 그대로 넣으면 시작할 때 생성해서 입 위치를 따라가게 한다")]
    [SerializeField] private ParticleSystem breathVfx;
    [Tooltip("불이 나오는 뼈. 비워 두면 이름이 Head인 뼈를 찾는다")]
    [SerializeField] private Transform breathOrigin;
    [SerializeField] private float breathForwardOffset = 1.5f;

    private NavMeshAgent nav;
    private Animator animator;
    private Health health;
    private Health targetHealth;
    private Transform model;
    private Vector3 modelBasePosition;
    private bool wantsToFly;
    private float currentFlyHeight;
    private bool aimBreathVfx;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private float watchTime;
    private Vector3 lastSeenPosition;

    public Transform Target => target;
    public Vector3 LastSeenPosition => lastSeenPosition;
    public bool IsFlying => CurrentState.Get(DragonKeys.IsFlying);

    // 감지 거리 안에 있어도 둥지 영역 밖이면 쫓지 않는다
    public bool HasTarget => DistanceToTarget() <= detectRange && HorizontalDistance(target.position, homePosition) <= territoryRadius;
    // 타겟을 놓친 뒤 경계(노려보기)를 giveUpTime초 동안 했는지. 공격 모션 중에 놓쳐도 노려보는 시간은 온전히 보장된다
    public bool HasGivenUp => !HasTarget && watchTime >= giveUpTime;
    public bool IsNearHome => HorizontalDistance(transform.position, homePosition) <= homeRadius;
    // 둥지에 도착해서 처음 바라보던 방향까지 맞췄는지
    public bool IsAtHome => IsNearHome && Quaternion.Angle(transform.rotation, homeRotation) <= 10f;

    /// <summary>
    /// 드래곤이 쓰는 액션 목록. 플래너 테스트(PlannerScenarios)도 같은 목록을 쓰도록 static으로 분리
    /// </summary>
    public static List<GoapAction> CreateDragonActions()
    {
        return new List<GoapAction>
        {
            new ChaseAction(),
            new BiteAction(),
            new ClawAction(),
            new BreathAction(),
            // "경계 → 비행"과 "비행 → 경계"는 같은 상태, 같은 비용이라 먼저 찾은 순서만 남는다.
            // 경계를 비행보다 앞에 둬서 "노려본 뒤 날아간다"가 되도록 한다
            new WatchAction(),
            new TakeOffAction(),
            new LandAction(),
            new FlyBreathAction(),
            new RoarAction(),
            new RestAction(),
            new WalkHomeAction(),
            new FlyHomeAction(),
        };
    }

    protected override void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        model = animator.transform;
        modelBasePosition = model.localPosition;
        homePosition = transform.position;
        homeRotation = transform.rotation;

        health = GetComponent<Health>();
        if (health == null)
        {
            // 프리팹에 Health를 아직 붙이지 않았을 때를 대비 (기본 HP 100)
            health = gameObject.AddComponent<Health>();
        }
        health.Died += OnDied;

        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        if (target != null)
        {
            targetHealth = target.GetComponent<Health>();
        }

        nav.speed = moveSpeed;
        nav.angularSpeed = turnSpeed;
        nav.stoppingDistance = meleeRange * 0.8f;

        SetupBreathVfx();

        base.Awake();
    }

    protected override IEnumerable<GoapAction> CreateActions()
    {
        return CreateDragonActions();
    }

    protected override IEnumerable<GoapGoal> CreateGoals()
    {
        return new GoapGoal[]
        {
            new AttackGoal(),
            new EnrageGoal(() => health.Ratio),
            new ReturnHomeGoal(),
            new RestGoal(),
        };
    }

    protected override void Update()
    {
        base.Update();
        UpdateAnimatorSpeed();
        UpdateFlightHeight();
    }

    // 애니메이션이 뼈를 움직인 뒤에 위치를 맞춰야 하므로 LateUpdate
    private void LateUpdate()
    {
        if (aimBreathVfx)
        {
            UpdateBreathVfxPose();
        }
    }

    protected override void Sense(WorldState sensed)
    {
        bool hasTarget = HasTarget;
        if (hasTarget)
        {
            lastSeenPosition = target.position;
            watchTime = 0f;
        }

        sensed.Set(DragonKeys.HasTarget, hasTarget);
        sensed.Set(DragonKeys.GaveUpChase, HasGivenUp);
        sensed.Set(DragonKeys.AtHome, IsAtHome);
        sensed.Set(DragonKeys.InMeleeRange, hasTarget && IsTargetInMeleeRange());
        sensed.Set(DragonKeys.InBreathRange, hasTarget && DistanceToTarget() <= breathRange);
        sensed.Set(DragonKeys.ClawReady, clawCooldown.IsReady);
        sensed.Set(DragonKeys.BreathReady, breathCooldown.IsReady);
        sensed.Set(DragonKeys.LowHealth, health.Ratio <= EnrageGoal.EnrageHpRatio);

        // 목표 플래그는 "이번 계획에서 해낼 일"이라 항상 false에서 출발한다.
        // 리셋하지 않으면 한 번 공격한 뒤 공격 목표가 영원히 "달성됨"이 된다.
        sensed.Set(DragonKeys.TargetDamaged, false);
        sensed.Set(DragonKeys.IsResting, false);

        // IsFlying, IsEnraged는 측정하지 않는다 → 액션 결과로 기억된 값을 그대로 사용
    }

    protected override void OnActionSucceeded(GoapAction action)
    {
        if (action is ClawAction)
        {
            clawCooldown.Trigger();
        }
        // 지상 브레스와 공중 브레스는 쿨타임을 같이 쓴다
        if (action is BreathAction || action is FlyBreathAction)
        {
            breathCooldown.Trigger();
        }
    }

    // ───────────── 액션이 쓰는 몸 동작 ─────────────

    /// <summary>
    /// Animator 상태로 부드럽게 전환. 이미 재생 중이면 그대로 두거나(restart = false) 처음부터 다시 재생
    /// </summary>
    public void PlayAnimation(string state, bool restart = false)
    {
        bool alreadyPlaying = !animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).IsName(state);
        if (alreadyPlaying)
        {
            if (restart)
            {
                animator.Play(state, 0, 0f);
            }
            return;
        }
        animator.CrossFadeInFixedTime(state, CrossFadeTime);
    }

    /// <summary>
    /// 해당 상태의 재생 진행도(0~1). 그 상태가 재생 중도 아니고 전환 대상도 아니면 -1
    /// </summary>
    public float GetAnimationProgress(string state)
    {
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            if (next.IsName(state))
            {
                return next.normalizedTime;
            }
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        return current.IsName(state) ? current.normalizedTime : -1f;
    }

    // 추격용 이동: 달리기 속도로, 근접 사거리 조금 안쪽에서 멈춘다
    public void MoveTo(Vector3 position)
    {
        SetDestination(position, moveSpeed, meleeRange * 0.8f);
    }

    /// <summary>
    /// 둥지로 이동하고, 도착하면 처음 바라보던 방향으로 돈다. 방향까지 맞추면 true
    /// </summary>
    public bool MoveHome(bool walk)
    {
        if (!IsNearHome)
        {
            SetDestination(homePosition, walk ? walkHomeSpeed : moveSpeed, 0.2f);
            return false;
        }

        StopMoving();
        transform.rotation = Quaternion.RotateTowards(transform.rotation, homeRotation, turnSpeed * Time.deltaTime);
        return IsAtHome;
    }

    private void SetDestination(Vector3 position, float speed, float stoppingDistance)
    {
        if (!nav.isOnNavMesh)
        {
            return;
        }
        nav.speed = speed;
        nav.stoppingDistance = stoppingDistance;
        nav.isStopped = false;
        nav.SetDestination(position);
    }

    public void StopMoving()
    {
        if (!nav.isOnNavMesh)
        {
            return;
        }
        nav.isStopped = true;
        nav.ResetPath();
    }

    // 경계 액션이 매 프레임 호출해서 노려본 시간을 쌓는다
    public void KeepWatching()
    {
        watchTime += Time.deltaTime;
        FacePosition(lastSeenPosition, 0.5f);
    }

    public void FaceTarget(float speedScale = 1f)
    {
        if (target != null)
        {
            FacePosition(target.position, speedScale);
        }
    }

    public void FacePosition(Vector3 position, float speedScale = 1f)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion look = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * speedScale * Time.deltaTime);
    }

    public bool HasAnimation(string state)
    {
        return animator.HasState(0, Animator.StringToHash(state));
    }

    public float DistanceToTarget()
    {
        return target != null ? HorizontalDistance(target.position, transform.position) : float.MaxValue;
    }

    // 높이 차이는 무시하고 바닥 평면(XZ) 거리만 잰다. 비행 중에도 사거리 판정이 같도록
    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        Vector3 offset = a - b;
        offset.y = 0f;
        return offset.magnitude;
    }

    /// <summary>
    /// tolerance는 사거리 배율. 타격 순간에는 1보다 크게 줘서 살짝 벗어난 타겟도 맞게 한다
    /// </summary>
    public bool IsTargetInMeleeRange(float tolerance = 1f)
    {
        return DistanceToTarget() <= meleeRange * tolerance;
    }

    /// <summary>
    /// 타겟이 브레스 부채꼴 안에 있는지. limitRange가 false면 거리는 보지 않고 방향만 본다(공중 브레스)
    /// </summary>
    public bool IsTargetInBreathCone(bool limitRange)
    {
        if (target == null || (limitRange && DistanceToTarget() > breathRange))
        {
            return false;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        return Vector3.Angle(transform.forward, direction) <= breathAngle;
    }

    public void DealDamage(float amount)
    {
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(amount);
        }
    }

    public void SetBreathVfx(bool on)
    {
        if (breathVfx == null)
        {
            return;
        }

        // isPlaying은 멈춘 뒤에도 남은 불꽃이 사라질 때까지 true일 수 있어서 isEmitting으로 판단
        if (on && !breathVfx.isEmitting)
        {
            breathVfx.Play();
        }
        else if (!on && breathVfx.isEmitting)
        {
            breathVfx.Stop();
        }
    }

    public void SetFlying(bool flying)
    {
        wantsToFly = flying;
    }

    // ───────────── 내부 처리 ─────────────

    // Locomotion 블렌드 트리의 임계값(0 / 0.3 / 0.8)에 맞춰 0~1로 정규화한 속도를 넣는다.
    // 최고 속도(moveSpeed) 기준이라 귀환할 때처럼 천천히 움직이면 걷기 모션이 나온다
    private void UpdateAnimatorSpeed()
    {
        float normalized = nav.velocity.magnitude / Mathf.Max(moveSpeed, 0.01f);
        animator.SetFloat(SpeedParam, normalized, 0.1f, Time.deltaTime);
    }

    private void SetupBreathVfx()
    {
        if (breathVfx == null)
        {
            return;
        }

        if (breathOrigin == null)
        {
            breathOrigin = FindChild(model, "Head");
        }

        // 프리팹 에셋은 씬에 존재하지 않아서 재생해도 보이지 않는다 → 복제해서 드래곤 아래에 생성
        if (!breathVfx.gameObject.scene.IsValid())
        {
            breathVfx = Instantiate(breathVfx, transform);
            aimBreathVfx = true;
        }

        // Play On Awake가 켜진 파티클이어도 브레스 전까지는 꺼 둔다
        breathVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // 입 위치에서 타겟 쪽을 향하게 한다. 좌우는 브레스 각도 안으로 제한해서 등 뒤로 불이 나가지 않게
    private void UpdateBreathVfxPose()
    {
        Vector3 origin = breathOrigin != null ? breathOrigin.position : transform.position + Vector3.up * 3f;
        Vector3 direction = transform.forward + Vector3.down * 0.3f;

        if (target != null)
        {
            Vector3 toTarget = target.position + Vector3.up - origin;
            Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
            float flatDistance = flat.magnitude;
            if (flatDistance > 0.01f)
            {
                Vector3 flatDirection = Vector3.RotateTowards(transform.forward, flat / flatDistance, breathAngle * Mathf.Deg2Rad, 0f);
                direction = flatDirection * flatDistance + Vector3.up * toTarget.y;
            }
        }

        direction.Normalize();
        breathVfx.transform.SetPositionAndRotation(origin + direction * breathForwardOffset, Quaternion.LookRotation(direction));
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }
        return null;
    }

    // NavMeshAgent(루트)는 바닥에 둔 채 모델만 위아래로 옮긴다
    private void UpdateFlightHeight()
    {
        float goal = wantsToFly ? flyHeight : 0f;
        currentFlyHeight = Mathf.MoveTowards(currentFlyHeight, goal, flyHeightSpeed * Time.deltaTime);
        model.localPosition = modelBasePosition + Vector3.up * currentFlyHeight;
    }

    private void OnDied()
    {
        Debug.Log("[Dragon] 쓰러짐");
        enabled = false;
        StopMoving();
        SetBreathVfx(false);
        model.localPosition = modelBasePosition;

        // Animator에 Die 상태를 추가해 두면 쓰러지는 모션을 재생
        if (animator.HasState(0, DieState))
        {
            animator.CrossFadeInFixedTime(DieState, CrossFadeTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, breathRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // 둥지 영역 (플레이 전에는 현재 위치가 둥지)
        Vector3 home = Application.isPlaying ? homePosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(home, territoryRadius);
        Gizmos.DrawWireSphere(home, homeRadius);
    }
}
