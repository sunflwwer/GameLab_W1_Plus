using UnityEngine;

/// <summary>
/// 원 궤도를 따라 공전하는 이동 컨트롤러.
/// - center: 공전 중심(없으면 시작 위치가 중심)
/// - radius: 반지름
/// - angularSpeedDeg: 각속도(도/초)
/// - planeNormal: 회전 평면의 법선(회전축)
/// - referenceDirection: 시작 각도를 정의할 기준 방향(축에 수직이어야 이상적)
/// - startAngleDeg: 시작 각도(기준 방향을 0도로 봄)
/// - clockwise: 시계/반시계
/// - useLocalSpace: planeNormal/referenceDirection을 center(또는 자기자신) 로컬기준으로 해석
/// - useRigidbody: Rigidbody.MovePosition 사용(권장: kinematic)
/// </summary>
[RequireComponent(typeof(Transform))]
public class CircularMover : MonoBehaviour
{
    [Header("Orbit")]
    public Transform center;
    public float radius = 3f;
    public float angularSpeedDeg = 90f;
    public float startAngleDeg = 0f;
    public bool clockwise = true;

    [Header("Plane")]
    public Vector3 planeNormal = Vector3.up;
    public Vector3 referenceDirection = Vector3.forward;
    public bool useLocalSpace = true;

    [Header("Physics")]
    public bool useRigidbody = false;
    public bool lookAtCenter = false; // 선택: 항상 중심을 바라보게

    // 내부 상태
    Rigidbody _rb;
    Vector3 _computedPos;
    float _angleDeg;
    Vector3 _initialCenter;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        _initialCenter = center ? center.position : transform.position;
        _angleDeg = startAngleDeg;
        UpdatePosition(0f, true);
    }

    void Update()
    {
        float sign = clockwise ? -1f : 1f;
        _angleDeg += angularSpeedDeg * sign * Time.deltaTime;

        UpdatePosition(0f, false);

        if (!useRigidbody || _rb == null)
        {
            transform.position = _computedPos;
            if (lookAtCenter)
                transform.rotation = Quaternion.LookRotation((GetCenter() - transform.position).normalized, GetAxis());
        }
    }

    void FixedUpdate()
    {
        if (useRigidbody && _rb != null)
        {
            _rb.MovePosition(_computedPos);
            if (lookAtCenter)
                _rb.MoveRotation(Quaternion.LookRotation((GetCenter() - _rb.position).normalized, GetAxis()));
        }
    }

    void UpdatePosition(float dt, bool forceSet)
    {
        Vector3 axis = GetAxis();
        Vector3 basisX = GetBasisX(axis);           // 축에 수직인 기준 벡터
        Vector3 basisY = Vector3.Cross(axis, basisX).normalized;

        float rad = _angleDeg * Mathf.Deg2Rad;
        Vector3 centerPos = GetCenter();
        Vector3 offset = (Mathf.Cos(rad) * basisX + Mathf.Sin(rad) * basisY) * Mathf.Max(0f, radius);

        _computedPos = centerPos + offset;

        if (forceSet)
        {
            if (!useRigidbody || _rb == null) transform.position = _computedPos;
            else _rb.position = _computedPos;
        }
    }

    Vector3 GetCenter()
    {
        return center ? center.position : _initialCenter;
    }

    Vector3 GetAxis()
    {
        Vector3 n;
        if (useLocalSpace)
        {
            if (center) n = center.TransformDirection(planeNormal);
            else n = transform.TransformDirection(planeNormal);
        }
        else n = planeNormal;

        if (n.sqrMagnitude < 1e-6f) n = Vector3.up;
        return n.normalized;
    }

    Vector3 GetBasisX(Vector3 axis)
    {
        Vector3 refDir;
        if (useLocalSpace)
        {
            if (center) refDir = center.TransformDirection(referenceDirection);
            else refDir = transform.TransformDirection(referenceDirection);
        }
        else refDir = referenceDirection;

        // 평면 위로 정사영
        Vector3 x = Vector3.ProjectOnPlane(refDir, axis);
        if (x.sqrMagnitude < 1e-6f)
        {
            // 기준이 축과 거의 평행하면 임의의 수직 벡터 선택
            x = Vector3.Cross(axis, Vector3.right);
            if (x.sqrMagnitude < 1e-6f) x = Vector3.Cross(axis, Vector3.forward);
        }
        return x.normalized;
    }

    void OnValidate()
    {
        radius = Mathf.Max(0f, radius);
        angularSpeedDeg = Mathf.Max(0f, angularSpeedDeg);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 axis = GetAxis();
        Vector3 bx = GetBasisX(axis);
        Vector3 by = Vector3.Cross(axis, bx).normalized;

        Vector3 c = Application.isPlaying ? GetCenter() : (center ? center.position : transform.position);
        Gizmos.color = Color.cyan;

        const int seg = 64;
        Vector3 prev = c + (bx * radius);
        for (int i = 1; i <= seg; i++)
        {
            float t = (float)i / seg * Mathf.PI * 2f;
            Vector3 p = c + (Mathf.Cos(t) * bx + Mathf.Sin(t) * by) * radius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
        Gizmos.DrawSphere(c, 0.06f);
    }
#endif
}
