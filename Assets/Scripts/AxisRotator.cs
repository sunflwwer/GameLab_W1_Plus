using UnityEngine;

[RequireComponent(typeof(Transform))]
public class AxisRotator : MonoBehaviour
{
    [Header("Axis")]
    [Tooltip("회전 기준 축 (단일 축)")]
    public Vector3 axis = Vector3.up;

    [Tooltip("축을 로컬 기준으로 해석할지 여부 (false면 월드 기준)")]
    public bool useLocalAxis = true;

    [Header("Motion")]
    [Tooltip("각속도 (도/초)")]
    public float angularSpeedDeg = 90f;

    [Tooltip("시계 방향 회전 여부")]
    public bool clockwise = true;

    [Header("Physics")]
    [Tooltip("Rigidbody 사용 (권장: isKinematic = true)")]
    public bool useRigidbody = false;

    Rigidbody _rb;

    void Awake()
    {
        if (useRigidbody) _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (useRigidbody && _rb != null) return; // 물리는 FixedUpdate에서
        RotateBy(Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (useRigidbody && _rb != null)
            RotateBy(Time.fixedDeltaTime);
    }

    void RotateBy(float dt)
    {
        if (dt <= 0f) return;

        // 회전 축(월드 기준으로 변환)
        Vector3 worldAxis = axis;
        if (worldAxis.sqrMagnitude < 1e-6f) worldAxis = Vector3.up;
        if (useLocalAxis) worldAxis = transform.TransformDirection(worldAxis);
        worldAxis.Normalize();

        float sign = clockwise ? -1f : 1f;
        float deltaDeg = angularSpeedDeg * sign * dt;
        Quaternion delta = Quaternion.AngleAxis(deltaDeg, worldAxis);

        if (useRigidbody && _rb != null)
        {
            // 월드 축 기준 회전
            _rb.MoveRotation(delta * _rb.rotation);
        }
        else
        {
            transform.rotation = delta * transform.rotation;
        }
    }

    void OnValidate()
    {
        angularSpeedDeg = Mathf.Max(0f, angularSpeedDeg);
    }
}
