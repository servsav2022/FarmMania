using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class ShopController : MonoBehaviour
{
    [Header("Shop Data")]
    [SerializeField] private ShopData shopData;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 1.5f;

    private Transform player;
    private Collider2D shopCollider;

    private void Awake()
    {
        shopCollider = GetComponent<Collider2D>();
        Debug.Log($"[ShopController] Awake on {name}");
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("[ShopController] Player with tag 'Player' NOT FOUND");
            return;
        }

        player = playerObj.transform;
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        TryClick();
    }

    private void TryClick()
    {
        if (Camera.main == null) return;

        Vector2 mouseWorld =
            Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        
        if (!shopCollider.OverlapPoint(mouseWorld)) return;

        if (!IsPlayerNear()) return;

        Debug.Log($"[ShopController] Clicked shop: {name}");
        OpenShop();
    }

    private bool IsPlayerNear()
    {
        if (player == null) return false;

        float dist = Vector2.Distance(player.position, transform.position);
        return dist <= interactDistance;
    }
    

    private void OpenShop()
    {
        Debug.Log($"[ShopController] OPEN SHOP {name}");

        if (shopData == null)
        {
            Debug.LogError("[ShopController] shopData is NULL");
            return;
        }

        if (ShopWindowController.Instance == null)
        {
            Debug.LogError("[ShopController] ShopWindowController.Instance is NULL");
            return;
        }

        ShopWindowController.Instance.Open(shopData);
    }
}
