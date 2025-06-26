using UnityEngine;

public class Player : MonoBehaviour
{
    public void OnSpawn()
    {
        // Find the player UI container in the world (from tag because prefabs cant have references)
        GameObject playerUIContainerObject = GameObject.FindWithTag("PlayerUI");
        Debug.Assert(playerUIContainerObject != null, "Player UI container not found");
        playerUIContainer = playerUIContainerObject.GetComponent<RectTransform>();
        Debug.Assert(playerUIContainer != null, "Player UI container not found");

        // Initialize camera state
        camBaseRot = Quaternion.Euler(camBaseRotEuler);
        camZoom = camZoomBase;
        facingDir = transform.forward;
        FixedUpdateCameraPosition(true);
        UpdateCamera(true);

        // Setup inventory UIs
        inventoryUI = PlayerUI.InstantiateElement<GridInventoryUI>(gridInventoryUIPrefab, "Player Inventory UI");
        heldItemUI = PlayerUI.InstantiateElement<ItemUI>(itemUIPrefab, "Player Held Inventory Item UI");
        inventoryUI.SetInventory(PlayerManager.Instance.Inventory);
    }

    [Header("Prefab")]
    [SerializeField] private GameObject gridInventoryUIPrefab;
    [SerializeField] private GameObject itemUIPrefab;
    [SerializeField] private GameObject looseItemPrefab;

    [Header("Camera Config")]
    [SerializeField] private Vector3 camCentreOffset = new(0, 1, 0);
    [SerializeField] private Vector3 camOrbitDir = new(0, 4.0f, -5.4f);
    [SerializeField] private Vector3 camBaseRotEuler = new(30, 0, 0);
    [SerializeField] private float camFollowLerp = 8;

    [Header("Camera Zoom Config")]
    [SerializeField] private float camZoomStrength = 0.1f;
    [SerializeField] private float camZoomMin = 2f;
    [SerializeField] private float camZoomMax = 15f;
    [SerializeField] private float camZoomBase = 6;

    [Header("Camera Sway Config")]
    [SerializeField] private float camSwayEaseScale = 0.5f;
    [SerializeField] private float camSwayMouseAmount = 5;
    [SerializeField] private float camSwayPlayerAmount = 0.5f;
    [SerializeField] private float camSwayLerp = 10;
    [SerializeField] private float camSwayDeadzone = 0.05f;

    [Header("Movement Config")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.2f;
    [SerializeField] private float rotationSpeed = 200f;

    private RectTransform playerUIContainer;
    private Quaternion camBaseRot = Quaternion.identity;
    private Vector3 moveInputDir = Vector3.zero;
    private Vector3 facingDir = Vector3.zero;
    private float camZoom = 1.0f;
    private LooseItem hoveredLooseItem = null;
    private GridInventoryUI inventoryUI;
    private ItemUI heldItemUI;
    private IItemContainerUI lastHoveredItemContainerUI;
    private bool isMousePressed = false;
    private bool isHoveringUI = false;

    private Camera Cam => Camera.main;
    private bool IsInputtingSprint => Input.GetKey(KeyCode.LeftShift);
    private bool IsInputtingMovement => moveInputDir.sqrMagnitude > 0.01f;
    private bool IsHoldingItemUI => heldItemUI.State != ItemUI.StateType.EMPTY;

    private void Update()
    {
        isHoveringUI = false;
        HandleInput();
        UpdateInteractingItemContainers();
        UpdateInteractingWorld();
        UpdateAnimation();
        UpdateCamera();
    }

    private void HandleInput()
    {
        // Handle mouse input
        isMousePressed = Input.GetMouseButtonDown(0);

        // Cast camera transform onto flat plane
        Vector3 forwardDir = Vector3.ProjectOnPlane(Cam.transform.forward, Vector3.up).normalized;

        // Receive input from the player and cast on the flat plane
        moveInputDir = Vector3.zero;
        moveInputDir += Input.GetAxisRaw("Horizontal") * Cam.transform.right;
        moveInputDir += Input.GetAxisRaw("Vertical") * forwardDir;
        moveInputDir = moveInputDir.normalized;

        // Only update the facing direction if there is movement input
        if (IsInputtingMovement) facingDir = moveInputDir;

        // Zoom with mouse wheel
        float scroll = Input.mouseScrollDelta.y;
        camZoom *= 1.0f - scroll * camZoomStrength;
        camZoom = Mathf.Clamp(camZoom, camZoomMin, camZoomMax);
    }

    private void UpdateAnimation()
    {
        // Squish character to show sprinting
        float squishAmount = IsInputtingSprint ? 0.9f : 1.0f;
        transform.localScale = new Vector3(1, squishAmount, 1);
    }

    private void UpdateCamera(bool force = false)
    {
        float xSway = 0;
        float ySway = 0;

        // Sway with the mouse
        float yOffset = Mathf.Clamp01(Input.mousePosition.y / Screen.height) - 0.5f;
        if (Mathf.Abs(yOffset) > camSwayDeadzone)
            xSway = -Easing.EaseOutQuad(camSwayEaseScale * (Mathf.Abs(yOffset) - camSwayDeadzone)) * Mathf.Sign(yOffset) * camSwayMouseAmount;
        float xOffset = Mathf.Clamp01(Input.mousePosition.x / Screen.width) - 0.5f;
        if (Mathf.Abs(xOffset) > camSwayDeadzone)
            ySway = Easing.EaseOutQuad(camSwayEaseScale * (Mathf.Abs(xOffset) - camSwayDeadzone)) * Mathf.Sign(xOffset) * camSwayMouseAmount;

        // Sway with player movement
        if (IsInputtingMovement)
        {
            Vector3 swayDir = Vector3.zero;
            swayDir += moveInputDir.z * Cam.transform.forward;
            swayDir += moveInputDir.x * Cam.transform.right;
            Vector3 swayDirLocal = Cam.transform.InverseTransformDirection(swayDir);
            xSway += -swayDirLocal.z * camSwayPlayerAmount;
            ySway += swayDirLocal.x * camSwayPlayerAmount;
        }

        // Rotate the camera based on the sway
        Quaternion targetRotation = camBaseRot * Quaternion.Euler(xSway, ySway, 0);
        if (!force) Cam.transform.rotation = Quaternion.Lerp(Cam.transform.rotation, targetRotation, camSwayLerp * Time.deltaTime);
        else Cam.transform.rotation = targetRotation;
    }

    private void UpdateInteractingItemContainers()
    {
        // Find what container and item UIs are being hovered
        var raycastResults = UIUtility.GetEventSystemRaycastResults();
        IItemContainerUI hoveredItemContainerUI = null;
        ItemUI hoveredItemUI = null;
        foreach (var result in raycastResults)
        {
            if (result.gameObject.TryGetComponent(out IItemContainerUI newHoveredInventoryUI))
            {
                hoveredItemContainerUI = newHoveredInventoryUI;
            }
            if (result.gameObject.TryGetComponent(out ItemUI newHoveredItemUI) && newHoveredItemUI != heldItemUI)
            {
                hoveredItemUI = newHoveredItemUI;
            }
        }
        bool isHoveringItemContainerUI = hoveredItemContainerUI != null;
        bool isHoveringItemUI = hoveredItemUI != null;
        isHoveringUI = isHoveringItemContainerUI || isHoveringItemUI;

        // Disable preview when stopping hovering inventory
        if (lastHoveredItemContainerUI != hoveredItemContainerUI && lastHoveredItemContainerUI != null) lastHoveredItemContainerUI.DisablePreview();
        lastHoveredItemContainerUI = hoveredItemContainerUI;

        // Handle interacting while holding an item
        if (isMousePressed)
        {
            if (IsHoldingItemUI)
            {
                // Item was clicked onto an inventory
                if (isHoveringItemContainerUI)
                {
                    hoveredItemContainerUI.PlaceOrStackHeldItem(heldItemUI, hoveredItemUI);
                }
                // Item was dropped outside any inventory
                else
                {
                    Vector3 droppedPosition = transform.position + facingDir * 0.5f + Vector3.up * 0.5f;
                    Quaternion droppedRotation = Quaternion.LookRotation(facingDir, Vector3.up);
                    GameObject looseItemObject = Instantiate(looseItemPrefab, droppedPosition, droppedRotation);
                    LooseItem looseItem = looseItemObject.GetComponent<LooseItem>();
                    looseItem.SetItemInstance(heldItemUI.ItemInstance);
                    heldItemUI.SetItem(null);
                }
                isMousePressed = false;
            }

            // Clicked onto an item
            else if (isHoveringItemUI)
            {
                hoveredItemUI.ContainerUI.PickupItem(heldItemUI, hoveredItemUI);
                isMousePressed = false;
            }
        }
        else
        {
            // Tell the hovered inventory the needed information for preview
            if (isHoveringItemContainerUI)
            {
                hoveredItemContainerUI.HoverPreview(heldItemUI, hoveredItemUI);
            }
        }
    }

    private void UpdateInteractingWorld()
    {
        if (!IsHoldingItemUI && !isHoveringUI)
        {
            // Raycast to check for loose items
            Ray ray = Cam.ScreenPointToRay(Input.mousePosition);
            LooseItem newHoveredLooseItem = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.rigidbody) newHoveredLooseItem = hit.rigidbody.GetComponent<LooseItem>();
            }

            // If we have a new item, update the hovered item
            if (newHoveredLooseItem != hoveredLooseItem)
            {
                if (hoveredLooseItem != null) hoveredLooseItem.OnHoverExit();
                hoveredLooseItem = newHoveredLooseItem;
                if (hoveredLooseItem != null) hoveredLooseItem.OnHoverEnter();
            }

            // If we are hovering an item and click then try to pick it up
            if (hoveredLooseItem != null && isMousePressed)
            {
                ItemInstance itemInstance = hoveredLooseItem.Pickup();
                heldItemUI.SetItem(itemInstance);
                Vector2 offset = new(heldItemUI.Rect.sizeDelta.x / 2, -heldItemUI.Rect.sizeDelta.y / 2);
                heldItemUI.SetHeldByMouse(offset);
                isMousePressed = false;
            }
        }
    }

    private void FixedUpdate()
    {
        FixedUpdateMovement();
        FixedUpdateCameraPosition();
    }

    private void FixedUpdateMovement()
    {
        // Rotate towards input direction
        if (facingDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(facingDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        if (!IsInputtingMovement) return;

        // Move in input direction
        float speed = IsInputtingSprint ? movementSpeed * sprintMultiplier : movementSpeed;
        transform.position += speed * Time.fixedDeltaTime * moveInputDir;
    }

    private void FixedUpdateCameraPosition(bool force = false)
    {
        // Move camera based on player position and zoom
        Vector3 centrePosition = transform.position + camCentreOffset;
        Vector3 targetPosition = centrePosition + camOrbitDir.normalized * camZoom;
        if (!force) Cam.transform.position = Vector3.Lerp(Cam.transform.position, targetPosition, camFollowLerp * Time.deltaTime);
        else Cam.transform.position = targetPosition;
    }
}
