using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGrapple : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Camera cam;                 // 레이 기준 카메라
    [SerializeField] LineRenderer rope;          // 로프 표시(선택)
    [SerializeField] PlayerInput playerInput;    // PlayerInput (없으면 동일 오브젝트에서 자동 획득)

    [Header("Grapple Settings")]
    [SerializeField] float maxDistance = 30f;      // 레이 사거리
    [SerializeField] float stopDistance = 2.0f;    // 이 거리 이하면 자동 종료
    [SerializeField] LayerMask blockMask = ~0;     // 시야 차단 체크
    [SerializeField] string targetLayerName = "Rope"; // 맞아야 하는 레이어 이름

    [Header("Grapple Speed Control")]
    [SerializeField] float approachSpeed = 10f;   // 앵커 방향 목표 속도
    [SerializeField] float accelLerp = 12f;       // 목표 속도로 수렴 강도
    [SerializeField] float maxGrappleSpeed = 12f; // 로프 당김 중 속도 상한
    [SerializeField] float grappleDrag = 4f;      // 로프 당김 중 임시 drag

    Rigidbody rb;
    GameManager gm;

    bool isGrappling = false;
    Vector3 anchor;               // 맞춘 지점
    InputAction grappleAction;    // "Grapple" 액션 캐시
    float originalDrag;           // 드래그 복원용
    int targetLayer;              // Rope 레이어 번호

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gm = GameManager.Instance;

        targetLayer = LayerMask.NameToLayer(targetLayerName);

        if (rope)
        {
            rope.positionCount = 2;
            rope.useWorldSpace = true;
            rope.enabled = false;
        }

        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            grappleAction = playerInput.actions["Grapple"];
            if (grappleAction != null)
            {
                grappleAction.started += OnGrappleStarted;
                grappleAction.canceled += OnGrappleCanceled;
            }
        }
    }

    void OnDestroy()
    {
        if (grappleAction != null)
        {
            grappleAction.started -= OnGrappleStarted;
            grappleAction.canceled -= OnGrappleCanceled;
        }
    }

    void FixedUpdate()
    {
        if (!isGrappling) return;

        if (grappleAction != null && !grappleAction.IsPressed())
        {
            StopGrapple();
            return;
        }

        Vector3 toAnchor = anchor - transform.position;
        float dist = toAnchor.magnitude;
        Vector3 dir = (dist > 0.0001f) ? toAnchor / dist : Vector3.zero;

        // 시야 차단 체크
        if (Physics.Raycast(transform.position, dir, out var hit, dist, blockMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject.layer != targetLayer)
            {
                StopGrapple();
                return;
            }
        }

        if (dist <= stopDistance)
        {
            StopGrapple();
            return;
        }

        // 목표 속도 기반 스티어링
        Vector3 v = rb.linearVelocity;

        float vAlong = Vector3.Dot(v, dir);
        Vector3 vAlongVec = dir * vAlong;
        Vector3 vSide = v - vAlongVec;

        Vector3 desiredVel = dir * approachSpeed;
        Vector3 targetVel = desiredVel + vSide * 0.2f;

        float k = 1f - Mathf.Exp(-accelLerp * Time.fixedDeltaTime);
        rb.linearVelocity = Vector3.Lerp(v, targetVel, k);

        if (rb.linearVelocity.sqrMagnitude > maxGrappleSpeed * maxGrappleSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxGrappleSpeed;

        if (rope)
        {
            rope.enabled = true;
            rope.SetPosition(0, transform.position);
            rope.SetPosition(1, anchor);
        }
    }

    void OnGrappleStarted(InputAction.CallbackContext ctx) => TryStartGrapple();
    void OnGrappleCanceled(InputAction.CallbackContext ctx) => StopGrapple(); 

    void TryStartGrapple()
    {
        if (gm != null && (gm.isRestarting || gm.isClearing)) return;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.gameObject.layer == targetLayer)
            {
                anchor = hit.point;
                isGrappling = true;

                originalDrag = rb.linearDamping;
                rb.linearDamping = grappleDrag;
            }
        }
    }

    void StopGrapple()
    {
        isGrappling = false;
        if (rope) rope.enabled = false;

        rb.linearDamping = originalDrag;
    }
}
