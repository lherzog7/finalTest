
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class TrackPieceInteraction : MonoBehaviour
{
    [Header("Dragging")]
    [SerializeField] private float holdHeight = 1.5f;

    [Header("Snapping")]
    [SerializeField] private float snapDistance = 0.6f;

    private Rigidbody rb;
    private Camera mainCamera;
    private TrackSnapSlot currentSlot;

    private static TrackPieceInteraction heldPiece;

    private bool isHeld;
    private float dragPlaneHeight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
            return;

        if (isHeld)
        {
            DragPiece();

            if (Input.GetMouseButtonDown(0))
                DropPiece();

            return;
        }

        if (heldPiece == null && Input.GetMouseButtonDown(0))
            TryPickup();
    }

    private void TryPickup()
    {
        Ray ray = mainCamera.ScreenPointToRay(
            Input.mousePosition
        );

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        TrackPieceInteraction piece =
            hit.collider.GetComponentInParent<TrackPieceInteraction>();

        if (piece != this)
            return;

        if (heldPiece != null)
            return;

        if (currentSlot != null)
        {
            currentSlot.SetOccupied(false);
            currentSlot = null;
        }

        heldPiece = this;
        isHeld = true;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        dragPlaneHeight = holdHeight;
        transform.position = new Vector3(
            transform.position.x,
            holdHeight,
            transform.position.z
        );
    }

    private void DragPiece()
    {
        Ray ray = mainCamera.ScreenPointToRay(
            Input.mousePosition
        );

        Plane dragPlane = new Plane(
            Vector3.up,
            new Vector3(0f, dragPlaneHeight, 0f)
        );

        if (!dragPlane.Raycast(ray, out float distance))
            return;

        Vector3 point = ray.GetPoint(distance);

        TrackSnapSlot nearestSlot = null;
        float nearestDistance = snapDistance;

        foreach (TrackSnapSlot slot in
                 FindObjectsByType<TrackSnapSlot>(
                     FindObjectsSortMode.None))
        {
            if (slot.IsOccupied)
                continue;

            Vector3 slotPosition = slot.transform.position;

            float distanceToSlot = Vector2.Distance(
                new Vector2(point.x, point.z),
                new Vector2(slotPosition.x, slotPosition.z)
            );

            if (distanceToSlot <= nearestDistance)
            {
                nearestDistance = distanceToSlot;
                nearestSlot = slot;
            }
        }

        if (nearestSlot != null)
        {
            transform.position = nearestSlot.transform.position;
            transform.rotation = nearestSlot.transform.rotation;
        }
        else
        {
            // Follow the mouse while held.
            transform.position = new Vector3(
                point.x,
                holdHeight,
                point.z
            );
        }
    }

    private void DropPiece()
    {
        TrackSnapSlot nearestSlot = null;
        float nearestDistance = snapDistance;

        foreach (TrackSnapSlot slot in
                 FindObjectsByType<TrackSnapSlot>(
                     FindObjectsSortMode.None))
        {
            if (slot.IsOccupied)
                continue;

            Vector3 a = transform.position;
            Vector3 b = slot.transform.position;

            float distance = Vector2.Distance(
                new Vector2(a.x, a.z),
                new Vector2(b.x, b.z)
            );

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestSlot = slot;
            }
        }

        isHeld = false;
        heldPiece = null;

        if (nearestSlot != null)
        {
            transform.position = nearestSlot.transform.position;
            transform.rotation = nearestSlot.transform.rotation;

            currentSlot = nearestSlot;
            currentSlot.SetOccupied(true);

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        else
        {
            // It wasn't placed in a slot, so it falls.
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}
