using UnityEngine;

/// <summary>
/// 일정 거리만큼 한 방향으로 갔다가 되돌아오는 왕복 이동 컨트롤러.
/// - distance: 이동 거리(미터)
/// - speed: 이동 속도(미터/초)
/// - localDirection: 기준 방향 (로컬/월드 전환 가능)
/// - endPause: 끝점에서 잠깐 멈추는 시간(초), 0이면 멈춤 없음
/// - useRigidbody: 참이면 Rigidbody.MovePosition 사용(권장: kinematic)
/// </summary>
[RequireComponent(typeof(Transform))]
public class BackAndForthMover : MonoBehaviour
{
    [Header("Path")]
    [Tooltip("이동 기본 방향(단위벡터로 정규화됨)")]
    public Vector3 localDirection = Vector3.right;

    [Tooltip("왕복 이동 거리(미터)")]
    public float distance = 5f;

    [Tooltip("이동 속도(미터/초)")]
    public float speed = 2f;

    [Tooltip("방향을 로컬 기준으로 해석할지 여부 (false면 월드 기준)")]
    public bool useLocalDirection = true;

    [Header("Behavior")]
    [Tooltip("끝점에서 멈추는 시간(초). 0이면 바로 반전")]
    public float endPause = 0f;

    [Tooltip("시작 시 가운데에서 시작할지 여부")]
    public bool startFromMiddle = false;

    [Tooltip("Rigidbody를 사용하여 이동(권장: isKinematic=true)")]
    public bool useRigidbody = false;

    [Tooltip("속도 가감속 곡선(0~1). 기본은 선형")]
    public AnimationCurve ease = AnimationCurve.Linear(0, 0, 1, 1);

    // 내부 상태
    private Vector3 _pointA;
    private Vector3 _pointB;
    private float _travelTime;          // A→B 또는 B→A 소요 시간(초)
    private float _phaseTimer;          // 현재 페이즈 경과 시간
    private Phase _phase = Phase.MoveAB;
    private Rigidbody _rb;
    private Vector3 _lastComputedPos;

    private enum Phase { MoveAB, PauseB, MoveBA, PauseA }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        InitPath();
        if (startFromMiddle)
        {
            // 가운데에서 시작
            _phase = Phase.MoveAB;
            _phaseTimer = _travelTime * 0.5f;
            UpdatePosition(0f, true);
        }
        else
        {
            // A에서 시작
            _phase = Phase.MoveAB;
            _phaseTimer = 0f;
            UpdatePosition(0f, true);
        }
    }

    /// <summary> 방향/거리/시작 위치 기준으로 A,B 지점 계산 </summary>
    public void InitPath()
    {
        Vector3 dir = useLocalDirection
            ? transform.TransformDirection(localDirection)
            : localDirection;

        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;
        dir.Normalize();

        _pointA = transform.position;
        _pointB = _pointA + dir * Mathf.Max(0f, distance);
        _travelTime = (speed > 1e-4f) ? (distance / speed) : Mathf.Infinity;
    }

    private void Update()
    {
        // 물리 이동은 FixedUpdate에서 MovePosition만 실행하므로,
        // 위치 계산은 매 프레임 동일하게 수행한다.
        float dt = Time.deltaTime;
        UpdatePosition(dt, false);

        if (!useRigidbody || _rb == null)
        {
            transform.position = _lastComputedPos;
        }
    }

    private void FixedUpdate()
    {
        if (useRigidbody && _rb != null)
        {
            _rb.MovePosition(_lastComputedPos);
        }
    }

    private void UpdatePosition(float dt, bool forceSet)
    {
        if (_travelTime == Mathf.Infinity)
        {
            // 속도가 0이면 정지
            _lastComputedPos = transform.position;
            return;
        }

        _phaseTimer += dt;

        switch (_phase)
        {
            case Phase.MoveAB:
                {
                    float t = Mathf.Clamp01(_phaseTimer / _travelTime);
                    float eased = ease.Evaluate(t);
                    _lastComputedPos = Vector3.Lerp(_pointA, _pointB, eased);

                    if (_phaseTimer >= _travelTime)
                    {
                        _phaseTimer = 0f;
                        _lastComputedPos = _pointB;
                        _phase = (endPause > 0f) ? Phase.PauseB : Phase.MoveBA;
                    }
                    break;
                }
            case Phase.PauseB:
                {
                    _lastComputedPos = _pointB;
                    if (_phaseTimer >= endPause)
                    {
                        _phaseTimer = 0f;
                        _phase = Phase.MoveBA;
                    }
                    break;
                }
            case Phase.MoveBA:
                {
                    float t = Mathf.Clamp01(_phaseTimer / _travelTime);
                    float eased = ease.Evaluate(t);
                    _lastComputedPos = Vector3.Lerp(_pointB, _pointA, eased);

                    if (_phaseTimer >= _travelTime)
                    {
                        _phaseTimer = 0f;
                        _lastComputedPos = _pointA;
                        _phase = (endPause > 0f) ? Phase.PauseA : Phase.MoveAB;
                    }
                    break;
                }
            case Phase.PauseA:
                {
                    _lastComputedPos = _pointA;
                    if (_phaseTimer >= endPause)
                    {
                        _phaseTimer = 0f;
                        _phase = Phase.MoveAB;
                    }
                    break;
                }
        }

        if (forceSet)
        {
            if (!useRigidbody || _rb == null)
                transform.position = _lastComputedPos;
            else
                _rb.position = _lastComputedPos;
        }
    }

    private void OnValidate()
    {
        distance = Mathf.Max(0f, distance);
        speed = Mathf.Max(0f, speed);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 에디터에서 경로 미리보기
        Vector3 dir = useLocalDirection
            ? transform.TransformDirection(localDirection)
            : localDirection;

        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;
        dir.Normalize();

        Vector3 a = Application.isPlaying ? _pointA : transform.position;
        Vector3 b = Application.isPlaying ? _pointB : transform.position + dir * Mathf.Max(0f, distance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.08f);
        Gizmos.DrawSphere(b, 0.08f);
    }
#endif
}
