using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController_6 : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 0.1f;
    [SerializeField] float cameraRotationSpeed = 0.05f;
    [SerializeField] float minPitch = -30f;
    [SerializeField] float maxPitch = 60f;
    [SerializeField] float cameraDistance = 10f;

    [SerializeField] LayerMask cameraCollisionMask = ~0;
    [SerializeField] float minCameraDistance = 0.3f;
    [SerializeField] float cameraProbeRadius = 0.2f;
    [SerializeField] float cameraCollisionLerp = 20f;

    [SerializeField] float camForwardOffset = 0.5f; // 앞쪽(+forward)으로 조금
    [SerializeField] float camUpOffset = 0.3f; // 위쪽(+up)으로 조금

    float currentCamDist;

    [SerializeField] ParticleSystem starParticlePrefab;

    public Camera playerCamera;

    GameManager gameManager;
    Rigidbody rb;
    Vector2 moveInput;
    Vector2 lookInput;
    float cameraPitch = 0f;

    bool isTouchingChangeToGlass = false;

    public bool IsClinging { get; set; }

    Player playerCore; // Player 컴포넌트 캐시

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCore = GetComponent<Player>();
    }

    void Start()
    {
        gameManager = GameManager.Instance;
        currentCamDist = cameraDistance;
    }

    void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    void ProcessLook()
    {
        // 플레이어 기준 피벗(시선 중앙)
        Vector3 pivot = transform.position + Vector3.up * 1.5f;

        // 오른쪽 측면 방향(플레이어의 +right 방향으로 떨어져서 본다)
        Vector3 desiredDir = transform.right;

        float desiredDist = cameraDistance;

        // 충돌 보정 (기존 로직 유지)
        float targetDist = desiredDist;
        if (Physics.SphereCast(
                pivot,
                cameraProbeRadius,
                desiredDir,
                out RaycastHit hit,
                desiredDist,
                cameraCollisionMask,
                QueryTriggerInteraction.Ignore))
        {
            targetDist = Mathf.Max(minCameraDistance, hit.distance - cameraProbeRadius);
        }

        // 거리 스무딩
        currentCamDist = Mathf.Lerp(
            currentCamDist,
            targetDist,
            1f - Mathf.Exp(-cameraCollisionLerp * Time.deltaTime));

        // 실제 카메라 위치/시선 적용
        Vector3 camPos = pivot
                       + desiredDir * currentCamDist
                       + transform.forward * camForwardOffset   // 앞쪽으로 살짝
                       + Vector3.up * camUpOffset;              // 위로 살짝

        playerCamera.transform.position = camPos;
        playerCamera.transform.LookAt(pivot);
    }


    void FixedUpdate()
    {
        if (gameManager.isRestarting || gameManager.isClearing) return;
        ProcessMove();
    }

    void LateUpdate()
    {
        if (gameManager.isRestarting || gameManager.isClearing) return;
        ProcessLook();
    }

    public Vector3 GetMoveDirection()
    {
        Vector3 camForward = playerCamera.transform.forward;
        Vector3 camRight = playerCamera.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        return camRight * moveInput.x + camForward * moveInput.y;
    }

    void ProcessMove()
    {
        if (IsClinging) return;

        Vector3 moveDir = GetMoveDirection();
        Vector3 targetPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);
    }

    void OnCollisionEnter(Collision collision)
    {


        if (collision.gameObject.layer == LayerMask.NameToLayer("Clear"))
        {
            GameManager.Instance.StageClear();
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ChangeToGlass"))
        {
            isTouchingChangeToGlass = true;

            if (playerCore != null && playerCore.ConsumeShield())
            {
                StartCoroutine(CheckChangeToGlassAfterDelay(0.5f));
            }
            else
            {
                GameManager.Instance.SpawnPlayer();
            }
            return;
        }

        if (other.CompareTag("Item"))
        {
            Vector3 spawnPos = other.transform.position;
            if (starParticlePrefab != null)
            {
                var ps = Instantiate(starParticlePrefab, spawnPos, Quaternion.identity);
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            GameManager.Instance.CollectStar(other.gameObject);
        }
    }


    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("ChangeToGlass"))
        {
            isTouchingChangeToGlass = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("ChangeToGlass"))
        {
            isTouchingChangeToGlass = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("ChangeToGlass"))
        {
            isTouchingChangeToGlass = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ChangeToGlass"))
        {
            isTouchingChangeToGlass = false;
        }
    }


    System.Collections.IEnumerator CheckChangeToGlassAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 아직도 ChangeToGlass에 닿아있다면 사망
        if (isTouchingChangeToGlass)
        {
            GameManager.Instance.SpawnPlayer();
        }
    }

}
