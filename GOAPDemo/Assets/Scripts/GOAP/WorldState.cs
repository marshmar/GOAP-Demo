using System.Collections.Generic;

public class WorldState
{
    private readonly Dictionary<string, bool> values = new Dictionary<string, bool>();

    public void Set(string key, bool value)
    {
        values[key] = value;
    }

    public bool Get(string key)
    {
        return values.TryGetValue(key, out bool value) && value;
    }

    /// <summary>
    /// 현재 상태가 조건 목록에 적힌 항목을 전부 맞추고 있는지 확인
    /// </summary>
    public bool Satisfies(WorldState conditions)
    {
        foreach (var kvp in conditions.values)
        {
            if (Get(kvp.Key) != kvp.Value)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 조건 중 아직 맞추지 못한 항목 수. 플래너가 "목표까지 얼마나 남았나"를 추정할 때 사용
    /// </summary>
    public int CountUnsatisfied(WorldState conditions)
    {
        int count = 0;
        foreach (var kvp in conditions.values)
        {
            if (Get(kvp.Key) != kvp.Value)
            {
                count++;
            }
        }
        return count;
    }

    public WorldState Apply(WorldState effects)
    {
        WorldState newState = new WorldState();
        foreach (var kvp in values)
        {
            newState.Set(kvp.Key, kvp.Value);
        }
        foreach (var kvp in effects.values)
        {
            newState.Set(kvp.Key, kvp.Value);
        }
        return newState;
    }

    /// <summary>
    /// 두 상태가 같은지 비교하기 위한 문자열. "키 없음 = false"이므로 true인 키만 정렬해서 이어 붙임
    /// </summary>
    public string GetSignature()
    {
        var trueKeys = new List<string>();
        foreach (var kvp in values)
        {
            if (kvp.Value)
            {
                trueKeys.Add(kvp.Key);
            }
        }
        trueKeys.Sort(System.StringComparer.Ordinal);
        return string.Join("|", trueKeys);
    }

    public override string ToString()
    {
        var stateStrings = new List<string>();
        foreach (var kvp in values)
        {
            stateStrings.Add($"{kvp.Key}: {kvp.Value}");
        }
        return string.Join(", ", stateStrings);
    }
}
