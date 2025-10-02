using UnityEngine;
using UnityEngine.UI;   // 쿨타임 바(Image)
using TMPro;            // 남은 시간 텍스트(선택)
using System.Collections;

public class PlayerAbility_6 : MonoBehaviour
{
    [SerializeField] PlayerEffect playerEffect;
    [SerializeField] PlayerController_6 playerController;
    [SerializeField] Material[] abilityMaterials;
    [SerializeField] float jumpForce = 20.0f;
    [SerializeField] float flashDistance = 5.0f;

    [Header("Ground Pound")]
    [SerializeField] float groundPoundSpeed = 40f;      // 얼마나 빠르게 내리꽂을지
    [SerializeField] float groundCheckDistance = 0.6f;  // 공중 판정용 레이캐스트 거리
    [SerializeField] float groundPoundWindow = 0.25f;   // 바닥 충돌 유효창(초)

    // 내리꽂기 상태(외부 조회용)
    bool isGroundPounding = false;
    public bool IsGroundPounding => isGroundPounding;

    // ===== Dash 쿨타임 UI =====
    [Header("Dash Cooldown UI")]
    [SerializeField] float dashCooldown = 0.5f;         // 대시 쿨타임(초)
    [SerializeField] Image dashFillImage;               // Filled / Horizontal / Left
    [SerializeField] TMP_Text dashRemainText;           // 선택
    float lastDashTime = -999f;                         // 마지막 대시 시각

    // ===== Flash 쿨타임 UI =====
    [Header("Flash Cooldown UI")]
    [SerializeField] float flashCooldown = 2.0f;        // 점멸 쿨타임(초)
    [SerializeField] Image flashFillImage;              // Filled / Horizontal / Left
    [SerializeField] TMP_Text flashRemainText;          // 선택
    float lastFlashTime = -999f;                        // 마지막 점멸 시각

    MeshRenderer playerMaterial;
    Rigidbody rb;
    GameManager gameManager;

    bool hasDoubleJump = true;
    bool hasDash = true;
    bool hasFlash = true;
    bool usedDoubleJump = false; // 공중에서 한 번만 허용

    float initSpeedTime = 1.0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMaterial = GetComponent<MeshRenderer>();
        gameManager = GameManager.Instance;

        // 시작 시 쿨타임 바 가득
        if (dashFillImage) dashFillImage.fillAmount = 1f;
        if (dashRemainText) dashRemainText.text = "";
        if (flashFillImage) flashFillImage.fillAmount = 1f;
        if (flashRemainText) flashRemainText.text = "";
    }

    private void Update()
    {
        UpdateDashUI();
        UpdateFlashUI();
    }

    // ===== Dash 쿨타임 보조 =====
    void UpdateDashUI()
    {
        float elapsed = Time.time - lastDashTime;            // 대시 사용 후 경과 시간
        float t = Mathf.Clamp01(elapsed / dashCooldown);     // 0~1

        if (dashFillImage) dashFillImage.fillAmount = t;

        if (dashRemainText)
        {
            if (t < 1f)
            {
                float remain = Mathf.Max(0f, dashCooldown - elapsed);
                dashRemainText.text = remain.ToString("0.0") + "s";
            }
            else dashRemainText.text = "";
        }
    }

    bool IsDashReady() => (Time.time - lastDashTime) >= dashCooldown;
    void ConsumeDashCooldown() => lastDashTime = Time.time;

    // ===== Flash 쿨타임 보조 =====
    void UpdateFlashUI()
    {
        float elapsed = Time.time - lastFlashTime;           // 점멸 사용 후 경과 시간
        float t = Mathf.Clamp01(elapsed / flashCooldown);    // 0~1

        if (flashFillImage) flashFillImage.fillAmount = t;

        if (flashRemainText)
        {
            if (t < 1f)
            {
                float remain = Mathf.Max(0f, flashCooldown - elapsed);
                flashRemainText.text = remain.ToString("0.0") + "s";
            }
            else flashRemainText.text = "";
        }
    }

    bool IsFlashReady() => (Time.time - lastFlashTime) >= flashCooldown;
    void ConsumeFlashCooldown() => lastFlashTime = Time.time;

    // ====== 능력들 ======
    void OnGroundPound()
    {
        if (gameManager.isRestarting || gameManager.isClearing) return;
        if (IsGrounded()) return; // 공중에서만 발동

        // 수직 속도 리셋 후 즉시 강하게 하강
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.down * groundPoundSpeed, ForceMode.VelocityChange);

        // 내리꽂기 유효창 시작
        isGroundPounding = true;
        StopAllCoroutines();               // 중복 코루틴 방지
        StartCoroutine(EndGroundPoundWindow());
    }

    IEnumerator EndGroundPoundWindow()
    {
        yield return new WaitForSeconds(groundPoundWindow);
        isGroundPounding = false;
    }

    // 필요 시 외부(발판 등)에서 강제로 종료할 수 있도록 공개 메서드 제공
    public void ForceEndGroundPound()
    {
        isGroundPounding = false;
        StopAllCoroutines();
    }

    bool IsGrounded()
    {
        // 트리거는 무시하고 간단히 바닥 체크
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance,
                               ~0, QueryTriggerInteraction.Ignore);
    }

    void OnJump()
    {
        if (gameManager.isRestarting || gameManager.isClearing) return;

        if (IsGrounded())
        {
            // 1단 점프 (지상)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            usedDoubleJump = false; // 이륙했으니 곧바로 이중점프 허용 준비
            playerEffect.TriggerParticle(EffectType.Jump);
        }
        else if (!usedDoubleJump)
        {
            // 2단 점프 (공중 1회)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            usedDoubleJump = true; // 이중점프 소모
            playerEffect.TriggerParticle(EffectType.Jump);
        }
    }

    void OnDash()
    {
        if (gameManager.isRestarting || gameManager.isClearing) return;

        // 쿨타임 체크
        if (!IsDashReady())
        {
            return;
        }

        Vector3 dashDir = playerController.GetMoveDirection();  // ← 타입에 맞게
        if (dashDir == Vector3.zero) dashDir = transform.forward;

        rb.linearVelocity = dashDir * jumpForce;
        playerEffect.TriggerParticle(EffectType.Dash);

        // 쿨타임 시작
        ConsumeDashCooldown();
    }

    void OnFlash()
    {
        if (gameManager.isRestarting || gameManager.isClearing) return;

        // 쿨타임 체크(5초)
        if (!IsFlashReady())
        {
            return;
        }

        Vector3 flashDir = playerController.GetMoveDirection(); // ← 타입에 맞게
        if (flashDir == Vector3.zero) flashDir = transform.forward;

        playerEffect.TriggerParticle(EffectType.Flash);
        transform.position += flashDir * flashDistance;

        // 쿨타임 시작
        ConsumeFlashCooldown();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Platform"))
        {
            usedDoubleJump = false;
        }

        // 선택: 어떤 접촉이든 발생하면 유효창을 즉시 종료하고 싶을 때 활성화
        // isGroundPounding = false;
    }
}
