using UnityEngine;

/// <summary>
/// 플레이어가 '아래찍기'로 위에서 내려찍을 때만 깨지는 오브젝트.
/// - PlayerAbility.IsGroundPounding 이 true인 유효창 동안
/// - 플레이어의 Y 속도가 일정 이하(하강)이고
/// - (옵션) 윗면에서 맞았을 때만 파괴
/// </summary>
public class BreakableOnGroundPound : MonoBehaviour
{
    [Header("조건")]
    [SerializeField] float minDownSpeed = 10f;   // 이 속도 이상으로 하강 중일 때만
    [SerializeField] bool requireHitFromTop = true; // 윗면에서 맞아야만(true)

    [Header("연출/제거")]
    [SerializeField] GameObject breakVFX;        // 파괴 이펙트(선택)
    [SerializeField] AudioClip breakSFX;         // 파괴 사운드(선택)
    [SerializeField] float sfxVolume = 1f;
    [SerializeField] float destroyDelay = 0.8f;  // 콜라이더/렌더러 끄고 파괴까지 딜레이

    bool broken = false;

    void OnCollisionEnter(Collision col)
    {
        // 가장 첫 접점 기준
        var contact = col.GetContact(0);
        TryBreak(col.collider, contact.point, contact.normal);
    }

    void OnTriggerEnter(Collider other)
    {
        // 트리거일 경우 접점 노말이 없으니 '윗면' 판정만 완화
        Vector3 assumNormal = Vector3.up;
        TryBreak(other, other.ClosestPoint(transform.position), assumNormal);
    }

    void TryBreak(Component hitter, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (broken) return;

        // 플레이어 & 능력 컴포넌트 찾기
        var ability = hitter.GetComponentInParent<PlayerAbility>();
        if (ability == null || !ability.IsGroundPounding) return;

        var prb = ability.GetComponent<Rigidbody>();
        if (prb != null)
        {
            // 충분히 빠른 하강이어야 함
            if (prb.linearVelocity.y > -minDownSpeed) return;
        }

        // 윗면에서 맞아야 한다면, 노말이 위를 향하는지 확인
        if (requireHitFromTop && hitNormal.y < 0.2f) return;

        // 조건 만족 → 파괴 처리
        BreakNow(hitPoint, hitNormal, ability);
    }

    void BreakNow(Vector3 hitPoint, Vector3 hitNormal, PlayerAbility ability)
    {
        broken = true;

        // 체인 파괴 방지: 아래찍기 유효창 강제 종료(선택)
        ability.ForceEndGroundPound();

        // 충돌 즉시 충돌 막기
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

        // 시각적으로 숨김
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        if (breakVFX) Instantiate(breakVFX, hitPoint, Quaternion.LookRotation(hitNormal));
        if (breakSFX) AudioSource.PlayClipAtPoint(breakSFX, hitPoint, sfxVolume);

        Destroy(gameObject, Mathf.Max(0.01f, destroyDelay));
    }
}
