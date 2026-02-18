using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHybridMove2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Tilemap groundTilemap;

    [Tooltip("������ ������ ������ ������, ��� ���� �� ��������� �������� (��������� ��� ����������).")]
    [SerializeField] private int noMoveClickRadiusCells = 1;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float stopDistance = 0.05f;

    private Rigidbody2D rb;

    private Vector2 keyboardInput;
    private Vector2 targetWorld;
    private bool hasTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        ReadKeyboard();

        // ���� ����� ��� ������� � ���� �� �������� � ���������� ���� �����
        if (keyboardInput.sqrMagnitude > 0.001f)
        {
            hasTarget = false;
            return;
        }

        // ���� ������ ��� � ����� ������ ���� ������/�����
        TrySetTargetByPointer();
    }

    private void FixedUpdate()
    {
        if (keyboardInput.sqrMagnitude > 0.001f)
        {
            rb.linearVelocity = keyboardInput.normalized * moveSpeed;
            return;
        }

        if (!hasTarget)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 pos = rb.position;
        Vector2 toTarget = targetWorld - pos;
        float dist = toTarget.magnitude;

        if (dist <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            hasTarget = false;
            return;
        }

        rb.linearVelocity = (toTarget / dist) * moveSpeed;
    }

    private void ReadKeyboard()
    {
        // New Input System: ������� + WASD
        Vector2 v = Vector2.zero;
        var kb = Keyboard.current;
        if (kb == null)
        {
            keyboardInput = Vector2.zero;
            return;
        }

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1;

        keyboardInput = v;
    }

    private void TrySetTargetByPointer()
    {
        if (groundTilemap == null) return;

        if (!TryGetPointerDownScreenPos(out Vector2 screenPos))
            return;

        // ���� �������� �� UI � �� ������� ������ (����� �� �������)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Camera.main == null) return;

        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        Vector3Int cell = groundTilemap.WorldToCell(world);

        // ���� ��� ����� � �����
        if (!groundTilemap.HasTile(cell))
            return;

        // ���� �������� ����� � ������� � ��� ���� ����������, �������� �� ���������
        if (IsWithinNoMoveRadius(cell))
            return;

        targetWorld = groundTilemap.GetCellCenterWorld(cell);
        hasTarget = true;
    }

    private bool IsWithinNoMoveRadius(Vector3Int targetCell)
    {
        Vector3Int playerCell = groundTilemap.WorldToCell(transform.position);
        int dx = Mathf.Abs(targetCell.x - playerCell.x);
        int dy = Mathf.Abs(targetCell.y - playerCell.y);
        return dx <= noMoveClickRadiusCells && dy <= noMoveClickRadiusCells;
    }

    private bool TryGetPointerDownScreenPos(out Vector2 screenPos)
    {
        screenPos = default;

        // ����
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        // ���
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        return false;
    }
}
