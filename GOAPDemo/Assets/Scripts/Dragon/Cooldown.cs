using UnityEngine;

/// <summary>
/// 시간 기반 쿨타임. Trigger() 후 duration초가 지나면 다시 IsReady가 true
/// </summary>
[System.Serializable]
public class Cooldown
{
    [SerializeField] private float duration;
    private float readyTime;

    public Cooldown(float duration)
    {
        this.duration = duration;
    }

    public bool IsReady => Time.time >= readyTime;
    public float Remaining => Mathf.Max(0f, readyTime - Time.time);

    public void Trigger()
    {
        readyTime = Time.time + duration;
    }
}
