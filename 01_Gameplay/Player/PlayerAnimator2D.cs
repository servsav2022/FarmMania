using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimator2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;

    [Header("Tuning")]
    [SerializeField] private float moveEpsilon = 0.05f;   // когда считаем что "движется"
    [SerializeField] private float maxMoveSpeed = 4f;     // у тебя в инспекторе Move Speed = 4
    [SerializeField] private float minAnimSpeed = 0.35f;  // минимальная скорость анимации
    [SerializeField] private float maxAnimSpeed = 1.0f;   // максимальная

    private string _currentState = "";

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        Vector2 v = rb.linearVelocity;

        bool moving = v.sqrMagnitude > moveEpsilon * moveEpsilon;

        if (!moving)
        {
            // если стоим — "замораживаем" анимацию на текущем кадре (простое idle без отдельных клипов)
            animator.speed = 0f;
            return;
        }

        animator.speed = Mathf.Lerp(minAnimSpeed, maxAnimSpeed, Mathf.Clamp01(v.magnitude / maxMoveSpeed));

        // выбираем направление
        string state;
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
        {
            state = "WalkSide";
            spriteRenderer.flipX = v.x < 0f; // влево/вправо
        }
        else
        {
            if (v.y > 0f) state = "WalkBack";
            else state = "WalkFront";
        }

        PlayState(state);
    }

    private void PlayState(string stateName)
    {
        if (_currentState == stateName) return;
        _currentState = stateName;
        animator.speed = Mathf.Max(animator.speed, 0.01f);
        animator.Play(stateName, 0, 0f);
    }
}
