using UnityEngine;

/// <summary>
/// 로컬 Z축을 기준으로 좌↔우로 왕복 회전.
/// - minZDeg / maxZDeg: 회전 범위(도)
/// - cyclesPerSecond: 1초당 좌↔우 왕복 횟수
/// - phaseOffsetDeg: 시작 위상(여러 개체 타이밍 다르게)
/// - useRigidbody: 있으면 MoveRotation 사용(권장: isKinematic)
/// 씬 뷰에서 선택하면 회전 구간(아크)을 Gizmo로 그려줌.
/// </summary>
[RequireComponent(typeof(Transform))]
public class ZAxisOscillator : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("최소 Z각(도)")]
    public float minZDeg = -15f;
    [Tooltip("최대 Z각(도)")]
    public float maxZDeg = 15f;
    [Tooltip("1초에 몇 번 좌↔우 왕복할지")]
    public float cyclesPerSecond = 0.5f;
    [Tooltip("시작 위상(도)")]
    public float phaseOffsetDeg = 0f;
    public bool useRigidbody = false;
    public bool resetToCenterOnDisable = true;

    [Header("Gizmo")]
    public bool drawGizmoArc = true;
    public float gizmoRadius = 0.5f;
    public Color arcColor = new Color(1f, 0.9f, 0.2f);
    public Color edgeColor = Color.red;
    public Color centerColor = Color.white;

    Rigidbody _rb;
    Quaternion _baseLocalRot;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        _baseLocalRot = transform.localRotation;
    }

    void OnDisable()
    {
        if (resetToCenterOnDisable)
            transform.localRotation = _baseLocalRot;
    }

    void Update()
    {
        if (!useRigidbody) ApplyRotation(Time.time);
    }

    void FixedUpdate()
    {
        if (useRigidbody) ApplyRotation(Time.time);
    }

    void ApplyRotation(float t)
    {
        // 사인파로 min~max 사이 왕복
        float s = Mathf.Sin(Mathf.PI * 2f * cyclesPerSecond * t + phaseOffsetDeg * Mathf.Deg2Rad);
        float angle = Mathf.Lerp(minZDeg, maxZDeg, (s + 1f) * 0.5f);

        Quaternion targetLocal = _baseLocalRot * Quaternion.AngleAxis(angle, Vector3.forward);

        if (useRigidbody && _rb != null)
        {
            Quaternion targetWorld = transform.parent ? transform.parent.rotation * targetLocal : targetLocal;
            _rb.MoveRotation(targetWorld);
        }
        else
        {
            transform.localRotation = targetLocal;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmoArc) return;

        // 로컬 공간에서 아크 그리기
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        float start = Mathf.Min(minZDeg, maxZDeg) * Mathf.Deg2Rad;
        float end = Mathf.Max(minZDeg, maxZDeg) * Mathf.Deg2Rad;

        // 중앙(0도) 방향선
        Gizmos.color = centerColor;
        Vector3 cDir = new Vector3(Mathf.Cos(0f), Mathf.Sin(0f), 0f);
        Gizmos.DrawLine(Vector3.zero, cDir * gizmoRadius);

        // 양 끝 방향선
        Gizmos.color = edgeColor;
        Vector3 aDir = new Vector3(Mathf.Cos(start), Mathf.Sin(start), 0f);
        Vector3 bDir = new Vector3(Mathf.Cos(end), Mathf.Sin(end), 0f);
        Gizmos.DrawLine(Vector3.zero, aDir * gizmoRadius);
        Gizmos.DrawLine(Vector3.zero, bDir * gizmoRadius);

        // 아크
        Gizmos.color = arcColor;
        const int seg = 32;
        Vector3 prev = new Vector3(Mathf.Cos(start), Mathf.Sin(start), 0f) * gizmoRadius;
        for (int i = 1; i <= seg; i++)
        {
            float u = (float)i / seg;
            float ang = Mathf.Lerp(start, end, u);
            Vector3 p = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * gizmoRadius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        Gizmos.matrix = old;
    }
#endif

    void OnValidate()
    {
        gizmoRadius = Mathf.Max(0.01f, gizmoRadius);
        cyclesPerSecond = Mathf.Max(0f, cyclesPerSecond);
    }
}
