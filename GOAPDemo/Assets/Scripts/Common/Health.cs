using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 체력. 드래곤과 타겟이 같이 쓴다. 맞으면 잠깐 빨갛게 깜빡이고, 머리 위에 HP를 표시한다
/// </summary>
public class Health : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private const float FlashTime = 0.1f;

    [SerializeField] private float maxHp = 100f;
    [SerializeField] private string displayName = "HP";

    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private float flashEndTime;
    private bool flashing;
    private GUIStyle labelStyle;

    public float Current { get; private set; }
    public float Max => maxHp;
    public float Ratio => maxHp > 0f ? Current / maxHp : 0f;
    public bool IsDead => Current <= 0f;

    public event Action Died;

    private void Awake()
    {
        Current = maxHp;
        block = new MaterialPropertyBlock();

        // 깜빡임과 HP 표시 위치에는 몸체만 사용 (브레스 파티클 등은 제외)
        var bodyRenderers = new List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
            {
                bodyRenderers.Add(r);
            }
        }
        renderers = bodyRenderers.ToArray();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        Current = Mathf.Max(0f, Current - amount);
        flashEndTime = Time.time + FlashTime;
        SetFlash(true);

        if (IsDead)
        {
            Died?.Invoke();
        }
    }

    public void ResetHealth()
    {
        Current = maxHp;
    }

    private void Update()
    {
        if (flashing && Time.time >= flashEndTime)
        {
            SetFlash(false);
        }
    }

    private void SetFlash(bool on)
    {
        flashing = on;
        foreach (Renderer r in renderers)
        {
            if (on)
            {
                block.Clear();
                block.SetColor(BaseColorId, Color.red);
                block.SetColor(ColorId, Color.red);
                r.SetPropertyBlock(block);
            }
            else
            {
                r.SetPropertyBlock(null);
            }
        }
    }

    // 몸체 바운드의 꼭대기 위치에 HP 글자를 띄운다
    private void OnGUI()
    {
        Camera cam = Camera.main;
        if (cam == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 screen = cam.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
        if (screen.z < 0f)
        {
            return;
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
        }

        var rect = new Rect(screen.x - 70f, Screen.height - screen.y - 28f, 140f, 22f);
        GUI.Label(rect, $"{displayName}  {Current:0} / {maxHp:0}", labelStyle);
    }
}
