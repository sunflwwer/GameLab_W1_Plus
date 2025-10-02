using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
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
    void OnLook(InputValue value) => lookInput = value.Get<Vector2>();

    void ProcessLook()
    {
        float yaw = transform.eulerAngles.y + lookInput.x * rotationSpeed;
        cameraPitch -= lookInput.y * cameraRotationSpeed;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
        transform.eulerAngles = new Vector3(0f, yaw, 0f);

        Quaternion camRot = Quaternion.Euler(cameraPitch, yaw, 0f);
        Vector3 pivot = transform.position + Vector3.up * 1.5f;

        Vector3 desiredDir = camRot * Vector3.back;
        float desiredDist = cameraDistance;

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

        currentCamDist = Mathf.Lerp(
            currentCamDist,
            targetDist,
            1f - Mathf.Exp(-cameraCollisionLerp * Time.deltaTime));

        Vector3 camPos = pivot + desiredDir * currentCamDist;
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
