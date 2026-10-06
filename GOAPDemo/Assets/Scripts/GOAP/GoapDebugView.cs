using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 게임 화면 왼쪽 위에 GOAP 에이전트의 목표 우선순위, 현재 액션, 남은 계획, 월드 상태를 표시한다
/// </summary>
public class GoapDebugView : MonoBehaviour
{
    private GoapAgent agent;
    private GUIStyle style;

    private void Awake()
    {
        agent = GetComponent<GoapAgent>();
    }

    private void OnGUI()
    {
        if (agent == null)
        {
            return;
        }

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, richText = true };
        }

        var text = new StringBuilder();

        text.AppendLine("<b>Goals</b>");
        foreach (GoapGoal goal in agent.Goals)
        {
            string mark = goal == agent.CurrentGoal ? "> " : "   ";
            text.AppendLine($"{mark}{goal.Name}  {goal.GetPriority(agent.CurrentState)}");
        }

        string action = agent.CurrentAction != null
            ? $"{agent.CurrentAction.Name} ({agent.CurrentActionTime:0.0}s)"
            : "-";
        text.AppendLine($"\n<b>Action</b>  {action}");

        var remaining = new List<string>();
        foreach (GoapAction next in agent.RemainingPlan)
        {
            remaining.Add(next.Name);
        }
        text.AppendLine($"<b>Next</b>  {(remaining.Count > 0 ? string.Join(" > ", remaining) : "-")}");

        text.AppendLine("\n<b>World State</b>");
        text.Append(agent.CurrentState.ToString().Replace(", ", "\n"));

        GUI.Label(new Rect(10, 10, 300, 420), text.ToString(), style);
    }
}
