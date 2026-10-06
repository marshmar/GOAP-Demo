using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 테스트용 플레이어(Target 캡슐). WASD로 카메라 기준 이동, Space로 가까이 있는 드래곤을 공격한다
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float attackDamage = 10f;

    private Health health;
    private Health dragonHealth;
    private Transform dragon;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.Died += OnDied;
    }

    // 드래곤의 Health가 Awake에서 추가될 수도 있어서 Start에서 찾는다
    private void Start()
    {
        DragonAgent agent = FindFirstObjectByType<DragonAgent>();
        if (agent != null)
        {
            dragon = agent.transform;
            dragonHealth = agent.GetComponent<Health>();
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;

        Vector3 move = ToCameraSpace(input);
        transform.position += move * (moveSpeed * Time.deltaTime);

        // 이동하는 방향을 바라보게 회전 (3인칭 카메라가 뒤를 따라가기 좋도록)
        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Attack();
        }
    }

    // 화면 위쪽 = W 가 되도록 카메라 방향을 바닥 평면에 눕혀서 사용
    private Vector3 ToCameraSpace(Vector2 input)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return new Vector3(input.x, 0f, input.y);
        }

        // 카메라가 바로 아래를 보면 forward를 눕혔을 때 길이가 0이 되므로, 그때는 화면 위쪽(up)을 앞으로 쓴다
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
        }
        forward.Normalize();
        Vector3 right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
        return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
    }

    private void Attack()
    {
        if (dragonHealth == null || dragonHealth.IsDead)
        {
            return;
        }

        Vector3 offset = dragon.position - transform.position;
        offset.y = 0f;
        if (offset.magnitude > attackRange)
        {
            Debug.Log("[Player] 드래곤이 너무 멀다");
            return;
        }

        dragonHealth.TakeDamage(attackDamage);
        Debug.Log($"[Player] 드래곤 공격! 남은 HP {dragonHealth.Current:0}");
    }

    private void OnDied()
    {
        Debug.Log("[Player] 쓰러짐 → 체력 회복");
        health.ResetHealth();
    }
}
