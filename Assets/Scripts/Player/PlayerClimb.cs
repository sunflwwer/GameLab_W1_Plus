using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerClimb : MonoBehaviour
{
    [Header("Attach")]
    [SerializeField] private string climbLayerName = "Climb"; // 붙을 수 있는 레이어 이름
    [SerializeField] private float stickOffset = 0.10f;
    [SerializeField] private float snapLerp = 20f;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = false;
    [SerializeField] private bool logDebug = false;

    private Rigidbody rb;
    private PlayerController controller;

    private bool isClinging = false;
    private int climbContactCount = 0;

    private int climbLayer; // "Climb" 레이어 번호 저장

    private Vector3 anchorPos;
    private Vector3 hitNormal;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<PlayerController>();
        climbLayer = LayerMask.NameToLayer(climbLayerName);
        if (climbLayer == -1)
        {
            Debug.LogError("[PlayerClimb] 지정한 레이어가 존재하지 않습니다: " + climbLayerName);
        }
    }

    private void Update()
    {
        bool holdRight = Mouse.current != null && Mouse.current.rightButton.isPressed;
        if (isClinging && !holdRight)
        {
            Detach();
        }
    }

    private void FixedUpdate()
    {
        if (!isClinging) return;

        Vector3 next = Vector3.Lerp(rb.position, anchorPos, 1f - Mathf.Exp(-snapLerp * Time.fixedDeltaTime));
        rb.MovePosition(next);

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        bool holdRight = Mouse.current != null && Mouse.current.rightButton.isPressed;
        if (climbContactCount <= 0 || !holdRight)
        {
            Detach();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != climbLayer) return;
        climbContactCount++;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer != climbLayer) return;
        climbContactCount--;
        if (climbContactCount < 0) climbContactCount = 0;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!(Mouse.current != null && Mouse.current.rightButton.isPressed)) return;
        if (collision.gameObject.layer != climbLayer) return;

        Vector3 avgPoint = Vector3.zero;
        Vector3 avgNormal = Vector3.zero;
        int count = collision.contactCount;

        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var c = collision.GetContact(i);
            avgPoint += c.point;
            avgNormal += c.normal;
        }
        avgPoint /= count;
        avgNormal = avgNormal.sqrMagnitude > 1e-6f ? avgNormal.normalized : Vector3.forward;

        hitNormal = avgNormal;
        anchorPos = avgPoint + hitNormal * stickOffset;

        if (!isClinging)
        {
            isClinging = true;
            if (controller != null) controller.IsClinging = true;

            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (logDebug) Debug.Log("[PlayerClimb] 부착 시작: " + collision.collider.name);
        }

        if (drawDebug)
        {
            Debug.DrawRay(avgPoint, hitNormal * 0.4f, Color.green, 0.02f, false);
        }
    }

    private void Detach()
    {
        if (!isClinging) return;

        isClinging = false;
        if (controller != null) controller.IsClinging = false;

        rb.useGravity = true;

        if (logDebug) Debug.Log("[PlayerClimb] 해제");
    }

    public void ForceDetach() => Detach();
}
