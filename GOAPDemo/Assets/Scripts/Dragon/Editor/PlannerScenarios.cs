using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static DragonKeys;

/// <summary>
/// 메뉴 GOAP > Run Planner Scenarios.
/// 여러 상황을 플래너에 넣어 보고 기대한 계획이 나오는지 콘솔에 출력한다 (플레이 모드 필요 없음)
/// </summary>
public static class PlannerScenarios
{
    [MenuItem("GOAP/Run Planner Scenarios")]
    private static void Run()
    {
        List<GoapAction> actions = DragonAgent.CreateDragonActions();
        var attack = new AttackGoal();
        var enrage = new EnrageGoal(() => 0.3f);
        var rest = new RestGoal();
        int failed = 0;

        // 1페이즈
        failed += Check("1페이즈 | 근접 | 할퀴기·브레스 준비", attack, "Claw", actions,
            HasTarget, InMeleeRange, InBreathRange, ClawReady, BreathReady);
        failed += Check("1페이즈 | 근접 | 할퀴기 쿨타임", attack, "Breath", actions,
            HasTarget, InMeleeRange, InBreathRange, BreathReady);
        failed += Check("1페이즈 | 근접 | 둘 다 쿨타임", attack, "Bite", actions,
            HasTarget, InMeleeRange, InBreathRange);
        failed += Check("1페이즈 | 브레스 사거리 | 둘 다 준비", attack, "Breath", actions,
            HasTarget, InBreathRange, ClawReady, BreathReady);
        failed += Check("1페이즈 | 사거리 밖 | 둘 다 준비", attack, "Chase > Claw", actions,
            HasTarget, ClawReady, BreathReady);
        failed += Check("1페이즈 | 사거리 밖 | 할퀴기 쿨타임", attack, "Chase > Breath", actions,
            HasTarget, BreathReady);

        // 2페이즈
        failed += Check("2페이즈 | 근접 | 둘 다 준비", attack, "Claw", actions,
            HasTarget, IsEnraged, InMeleeRange, InBreathRange, ClawReady, BreathReady);
        failed += CheckRandom("2페이즈 | 근접 | 할퀴기 쿨타임 (비용 2 동점 → 랜덤)", attack,
            new[] { "Breath", "TakeOff > FlyBreath" }, actions,
            HasTarget, IsEnraged, InMeleeRange, InBreathRange, BreathReady);
        failed += Check("2페이즈 | 근접 | 둘 다 쿨타임", attack, "Bite", actions,
            HasTarget, IsEnraged, InMeleeRange, InBreathRange);
        failed += Check("2페이즈 | 지상, 사거리 밖 | 둘 다 준비 (지상 브레스는 사거리 밖)", attack, "TakeOff > FlyBreath", actions,
            HasTarget, IsEnraged, ClawReady, BreathReady);
        failed += Check("2페이즈 | 지상, 사거리 밖 | 브레스 쿨타임", attack, "Chase > Claw", actions,
            HasTarget, IsEnraged, ClawReady);
        failed += Check("2페이즈 | 비행 중 | 브레스 준비", attack, "FlyBreath", actions,
            HasTarget, IsEnraged, IsFlying, BreathReady);
        failed += Check("2페이즈 | 비행 중 | 브레스 쿨타임", attack, "Land > Chase > Claw", actions,
            HasTarget, IsEnraged, IsFlying, ClawReady);
        failed += Check("2페이즈 | 비행 중, 근접 | 둘 다 쿨타임", attack, "Land > Bite", actions,
            HasTarget, IsEnraged, IsFlying, InMeleeRange, InBreathRange);

        // 광폭화, 휴식
        failed += Check("HP 50% 미만 | 지상", enrage, "Roar", actions,
            HasTarget, LowHealth);
        failed += Check("타겟 없음 | 지상", rest, "Rest", actions);
        failed += Check("타겟 없음 | 비행 중", rest, "Land > Rest", actions,
            IsEnraged, IsFlying);

        // 둥지 복귀 (같은 목표인데 페이즈에 따라 계획이 달라진다)
        var home = new ReturnHomeGoal();
        failed += Check("타겟 놓침 | 1페이즈 지상", home, "Watch > WalkHome", actions);
        failed += Check("타겟 놓침 | 2페이즈 지상", home, "Watch > TakeOff > FlyHome", actions,
            IsEnraged);
        failed += Check("타겟 놓침 | 2페이즈 비행 중", home, "Watch > FlyHome", actions,
            IsEnraged, IsFlying);
        failed += Check("노려보기 끝남 | 1페이즈 지상", home, "WalkHome", actions,
            GaveUpChase);

        Debug.Log(failed == 0 ? "[Planner] 모든 시나리오 통과" : $"[Planner] {failed}개 시나리오가 기대와 다름");
    }

    // trueKeys에 적은 키만 true, 나머지는 false인 상태에서 계획을 세워 기대값과 비교
    private static int Check(string label, GoapGoal goal, string expected, List<GoapAction> actions, params string[] trueKeys)
    {
        List<GoapAction> plan = GoapPlanner.Plan(MakeState(trueKeys), goal, actions);
        string actual = PlanToString(plan);
        string detail = plan == null ? actual : GoapPlanner.Describe(plan);
        return Report(actual == expected, $"{label}\n목표 {goal.Name} | 결과 {detail} | 기대 {expected}");
    }

    // 동점이라 랜덤으로 갈리는 상황: 여러 번 계획해서 기대한 결과들만 나오고, 각각 한 번 이상 나오는지 확인
    private static int CheckRandom(string label, GoapGoal goal, string[] expected, List<GoapAction> actions, params string[] trueKeys)
    {
        const int Runs = 40;
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < Runs; i++)
        {
            string actual = PlanToString(GoapPlanner.Plan(MakeState(trueKeys), goal, actions));
            counts[actual] = counts.TryGetValue(actual, out int n) ? n + 1 : 1;
        }

        bool passed = counts.Count == expected.Length;
        foreach (string option in expected)
        {
            passed &= counts.ContainsKey(option);
        }

        var summary = new List<string>();
        foreach (var pair in counts)
        {
            summary.Add($"{pair.Key} {pair.Value}회");
        }
        return Report(passed, $"{label}\n목표 {goal.Name} | {Runs}회 중 {string.Join(", ", summary)}");
    }

    private static WorldState MakeState(string[] trueKeys)
    {
        var state = new WorldState();
        foreach (string key in trueKeys)
        {
            state.Set(key, true);
        }
        return state;
    }

    private static string PlanToString(List<GoapAction> plan)
    {
        return plan == null ? "(계획 없음)" : string.Join(" > ", plan.ConvertAll(a => a.Name));
    }

    private static int Report(bool passed, string message)
    {
        if (passed)
        {
            Debug.Log($"[OK] {message}");
        }
        else
        {
            Debug.LogWarning($"[FAIL] {message}");
        }
        return passed ? 0 : 1;
    }
}
