using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 3인칭 카메라. 마우스로 플레이어 주위를 돌고, 휠로 거리를 조절한다.
/// 시작하면 커서가 잠기며, Esc로 풀고 화면을 클릭하면 다시 잠긴다
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("거리")]
    [SerializeField] private float distance = 9f;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 25f;
    [SerializeField] private float zoomStep = 1.5f;

    [Header("각도")]
    [SerializeField] private float pivotHeight = 1.5f;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch = 5f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private float minCameraHeight = 0.5f;

    private float yaw;
    private float pitch = 20f;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        yaw = GetStartYaw();
        SetCursorLocked(true);
    }

    // 카메라는 캐릭터가 움직인 뒤에 따라가야 떨리지 않으므로 LateUpdate에서 처리
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateCursorLock();
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            ReadMouse();
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * pivotHeight;
        Vector3 position = pivot - rotation * Vector3.forward * distance;
        position.y = Mathf.Max(position.y, minCameraHeight);

        transform.position = position;
        transform.LookAt(pivot);
    }

    private void ReadMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        yaw += delta.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, minPitch, maxPitch);

        // 휠 값의 크기는 플랫폼마다 달라서 방향(부호)만 사용
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f)
        {
            distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * zoomStep, minDistance, maxDistance);
        }
    }

    private void UpdateCursorLock()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
        }
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLocked(true);
        }
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // 시작할 때 플레이어 뒤에서 드래곤 쪽을 바라보도록
    private float GetStartYaw()
    {
        DragonAgent dragon = FindFirstObjectByType<DragonAgent>();
        if (target == null || dragon == null)
        {
            return transform.eulerAngles.y;
        }

        Vector3 toDragon = dragon.transform.position - target.position;
        toDragon.y = 0f;
        return toDragon.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toDragon).eulerAngles.y : transform.eulerAngles.y;
    }
}
