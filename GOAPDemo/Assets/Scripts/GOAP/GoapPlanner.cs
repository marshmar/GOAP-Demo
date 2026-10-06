using System.Collections.Generic;

/// <summary>
/// A* 탐색으로 현재 상태에서 목표 상태까지 가는 가장 싼 액션 순서를 찾는다
/// </summary>
public static class GoapPlanner
{
    private const float CostEpsilon = 0.001f;
    private static readonly System.Random random = new System.Random();

    // 탐색 트리의 한 칸: "여기까지 이 액션들을 했다고 치면 상태는 이렇고 비용은 이만큼"
    private class Node
    {
        public WorldState State;
        public Node Parent;
        public GoapAction Action;   // 이 칸에 오려고 실행한 액션 (시작 칸은 null)
        public float Cost;          // g: 시작부터 여기까지 실제 비용 합
        public int Remaining;       // h: 목표 조건 중 아직 못 맞춘 개수
        public float Total => Cost + Remaining;
    }

    /// <summary>
    /// 계획을 찾으면 실행 순서대로 담긴 리스트를, 못 찾으면 null을 반환.
    /// 가장 싼 계획이 여러 개(동점)면 그중 하나를 랜덤으로 고른다
    /// </summary>
    public static List<GoapAction> Plan(WorldState start, GoapGoal goal, IReadOnlyList<GoapAction> actions)
    {
        var open = new List<Node>();
        // 상태별로 지금까지 찾은 가장 싼 비용. 같은 상태에 더 비싸게 도착하는 경로는 버린다
        var bestCost = new Dictionary<string, float>();
        // 목표에 도착한 칸들 중 비용이 가장 싼 것들 (동점 후보)
        var goalNodes = new List<Node>();
        float goalCost = float.MaxValue;

        open.Add(new Node { State = start, Cost = 0, Remaining = start.CountUnsatisfied(goal.DesiredState) });
        bestCost[start.GetSignature()] = 0;

        while (open.Count > 0)
        {
            Node current = PopCheapest(open);

            // f가 싼 순서로 꺼내므로, 찾은 최저 비용보다 비싼 칸이 나오면 더 볼 필요가 없다
            if (current.Total > goalCost + CostEpsilon)
            {
                break;
            }

            if (current.Remaining == 0)
            {
                if (current.Cost < goalCost - CostEpsilon)
                {
                    goalCost = current.Cost;
                    goalNodes.Clear();
                }
                goalNodes.Add(current);
                continue;
            }

            // 꺼내기 전에 더 싼 경로가 같은 상태를 갱신했다면 이 칸은 낡은 것
            if (current.Cost > bestCost[current.State.GetSignature()])
            {
                continue;
            }

            foreach (GoapAction action in actions)
            {
                if (!action.CanRun(current.State))
                {
                    continue;
                }

                WorldState next = action.Simulate(current.State);
                float cost = current.Cost + action.Cost;
                string signature = next.GetSignature();

                if (bestCost.TryGetValue(signature, out float known) && known <= cost)
                {
                    continue;
                }

                bestCost[signature] = cost;
                open.Add(new Node
                {
                    State = next,
                    Parent = current,
                    Action = action,
                    Cost = cost,
                    Remaining = next.CountUnsatisfied(goal.DesiredState)
                });
            }
        }

        if (goalNodes.Count == 0)
        {
            return null;
        }
        return BuildPath(goalNodes[random.Next(goalNodes.Count)]);
    }

    /// <summary>
    /// 디버그 출력용. 예) "Chase → Claw (총 비용 3)"
    /// </summary>
    public static string Describe(IEnumerable<GoapAction> plan)
    {
        var names = new List<string>();
        float total = 0;
        foreach (GoapAction action in plan)
        {
            names.Add(action.Name);
            total += action.Cost;
        }
        return $"{string.Join(" → ", names)} (총 비용 {total})";
    }

    // f(= g + h)가 가장 작은 칸을 꺼낸다. 같으면 목표에 더 가까운(h가 작은) 칸이 먼저
    private static Node PopCheapest(List<Node> open)
    {
        int best = 0;
        for (int i = 1; i < open.Count; i++)
        {
            Node candidate = open[i];
            Node current = open[best];
            if (candidate.Total < current.Total ||
                (candidate.Total == current.Total && candidate.Remaining < current.Remaining))
            {
                best = i;
            }
        }

        Node node = open[best];
        open.RemoveAt(best);
        return node;
    }

    // 목표에 도착한 칸에서 부모를 따라 거꾸로 올라가며 액션을 모은 뒤 뒤집는다
    private static List<GoapAction> BuildPath(Node node)
    {
        var path = new List<GoapAction>();
        while (node.Action != null)
        {
            path.Add(node.Action);
            node = node.Parent;
        }
        path.Reverse();
        return path;
    }
}
