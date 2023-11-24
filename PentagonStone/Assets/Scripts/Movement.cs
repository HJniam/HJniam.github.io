using System;
using UnityEngine;


public class Movement : MonoBehaviour, IMovement3D
{
    #region Variables
    public class MoveComponent //이동 제어에 필요한 컴포넌트
    {
        public CapsuleCollider capsule;
        public Rigidbody rBody;
    }
    [Serializable]
    public class MoveCheck //이동 시 체크해야 되는 사항
    {
        [Tooltip("지면으로 체크할 레이어 설정")]
        public LayerMask groundLayerMask = -1;

        [Range(0.01f, 0.5f), Tooltip("전방 감지 거리")]
        public float forwardCheckDistance = 0.1f;

        [Range(0.1f, 10.0f), Tooltip("지면 감지 거리")]
        public float groundCheckDistance = 2.0f;

        [Range(0.0f, 3.0f), Tooltip("지면 허용 거리")]
        public float groundAllowDistance = 0.01f;
    }
    [Serializable]
    public class MoveOption //이동 옵션
    {
        [Range(1f, 10f), Tooltip("이동속도")]
        public float speed = 5f;

        [Range(1f, 3f), Tooltip("달리기 이동속도 증가 계수")]
        public float runningCoef = 1.5f;

        [Range(1f, 100f), Tooltip("등반 가능한 경사각")]
        public float maxSlopeAngle = 50f;

        [Range(0f, 4f), Tooltip("경사로 이동속도 변화율(가속/감속)")]
        public float slopeAccel = 1f;

        [Range(-9.81f, 0f), Tooltip("중력")]
        public float gravity = -9.81f;
    }
    [Serializable]
    public class MoveState //이동 시 상태
    {
        public bool isMove;
        public bool isRun;
        public bool isBlocked;
        public bool onGround;
        public bool onSteepSlope;           //급경사에 있을 때
        public bool isOutOfControl;
    }
    [Serializable]
    public class MoveValue //이동 계산에 필요한 값들
    {
        public Vector3 worldMoveDir;
        public Vector3 groundNormal;
        public Vector3 groundCross;         //월드 이동 벡터 회전 축
        public Vector3 horizonVelocity;

        [Space]
        public float groundDistance;
        public float groundAngle;           // 현재 바닥의 경사각
        public float groundVerticalAngle;   // 수직으로 재측정한 경사각
        public float forwardGroundAngle;    // 캐릭터가 바라보는 방향의 경사각
        
        public float outOfControllDuration;
        public float slopeAccel;        // 경사로 인한 가속/감속 비율
        public float gravity;
    }

    #endregion

    #region Declares
    //테스트시 값 변경할 값들
    [SerializeField] private MoveComponent  _component = new();
    [SerializeField] private MoveCheck      _check     = new();
    [SerializeField] private MoveOption     _option    = new();
    [SerializeField] private MoveState      _state     = new();
    [SerializeField] private MoveValue      _value     = new();

    protected MoveComponent   mv_component => _component;
    protected MoveCheck       mv_check => _check;
    protected MoveOption      mv_option => _option;
    protected MoveState       mv_state => _state;
    protected MoveValue       mv_value => _value;


    private float _fixedDeltaTime;

    private float _castRadius; // Sphere 레이캐스트 반지름
    private Vector3 CapsuleBottomCenterPoint
        => new Vector3(transform.position.x, transform.position.y + mv_component.capsule.radius, transform.position.z);

    #endregion

    #region Unity Events
    private void Start()
    {
        InitRigidbody();
        InitCapsuleCollider();
    }

    private void FixedUpdate()
    {
        _fixedDeltaTime = Time.fixedDeltaTime;

        CheckGround();
        CheckForward();

        UpdatePhysics();
        UpdateValues();

        Rotate();

        CalculateMovements();
        ApplyMovementsToRigidbody();
    }

    #endregion

    #region Init Methods

    private void InitRigidbody()
    {
        TryGetComponent(out mv_component.rBody);
        if (mv_component.rBody == null) return;

        // 회전은 트랜스폼을 통해 직접 제어할 것이기 때문에 리지드바디 회전은 제한
        mv_component.rBody.constraints = RigidbodyConstraints.FreezeRotation;
        mv_component.rBody.interpolation = RigidbodyInterpolation.Interpolate;
        mv_component.rBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        mv_component.rBody.useGravity = false; // 중력 직접 제어
    }

    private void InitCapsuleCollider()
    {
        TryGetComponent(out mv_component.capsule);
        if (mv_component.rBody == null) return;

        _castRadius = mv_component.capsule.radius;
    }

    #endregion

    #region Public Methods

    bool IMovement3D.IsMove() => mv_state.isMove;
    bool IMovement3D.OnGround() => mv_state.onGround;
    float IMovement3D.GetDistanceFromGround() => mv_value.groundDistance;

    void IMovement3D.SetMovement(in Vector3 worldMoveDir, bool isRunning)
    {
        mv_value.worldMoveDir = worldMoveDir;
        mv_state.isMove = worldMoveDir.sqrMagnitude > 0.01f;
        mv_state.isRun = isRunning;
    }

    void IMovement3D.StopMove()
    {
        mv_value.worldMoveDir = Vector3.zero;
        mv_state.isMove = false;
        mv_state.isRun = false;
    }

    void IMovement3D.KnockBack(in Vector3 force, float time)
    {
        SetOutOfControl(time);
        mv_component.rBody.AddForce(force, ForceMode.Impulse);
    }

    public void SetOutOfControl(float time)
    {
        mv_value.outOfControllDuration = time;
    }

    #endregion

    #region Private Methods

    /// <summary> 하단 지면 검사 </summary>
    private void CheckGround()
    {
        mv_value.groundDistance = float.MaxValue;
        mv_value.groundNormal = Vector3.up;
        mv_value.groundAngle = 0f;
        mv_value.forwardGroundAngle = 0f;

        bool cast = 
            Physics.SphereCast(CapsuleBottomCenterPoint, _castRadius * 0.9f, Vector3.down, out var hit, mv_check.groundCheckDistance, mv_check.groundLayerMask, QueryTriggerInteraction.Ignore);

        mv_state.onGround = false;

        if (cast)
        {
            // 지면 노멀벡터 초기화
            mv_value.groundNormal = hit.normal;

            // 현재 위치한 지면의 경사각 구하기(캐릭터 이동방향 고려)
            mv_value.groundAngle = Vector3.Angle(mv_value.groundNormal, Vector3.up);
            mv_value.forwardGroundAngle = Vector3.Angle(mv_value.groundNormal, mv_value.worldMoveDir) - 90f;

            mv_state.onSteepSlope = mv_value.groundAngle >= mv_option.maxSlopeAngle;
            mv_value.groundDistance = Mathf.Max(hit.distance - _castRadius * 0.1f, 0f);

            mv_state.onGround =
                (mv_value.groundDistance <= mv_check.groundAllowDistance) && !mv_state.onSteepSlope;
        }

        // 월드 이동벡터 회전축
        mv_value.groundCross = Vector3.Cross(mv_value.groundNormal, Vector3.up);
    }

    /// <summary> 전방 장애물 검사 : 레이어 관계 없이 trigger가 아닌 모든 장애물 검사 </summary>
    private void CheckForward()
    {
        bool cast =
            Physics.SphereCast(CapsuleBottomCenterPoint, _castRadius, mv_value.worldMoveDir, out var hit, mv_check.forwardCheckDistance, mv_check.groundLayerMask, QueryTriggerInteraction.Ignore);
       

        mv_state.isBlocked = false;
        if (cast)
        {
            mv_state.isBlocked = Vector3.Angle(hit.normal, Vector3.up) >= mv_option.maxSlopeAngle;
        }
    }

    private void UpdatePhysics()
    {
        if (mv_state.onGround)
        {
            mv_value.gravity = 0f;
        }
        else
        {
            mv_value.gravity += _fixedDeltaTime * mv_option.gravity;
        }
    }

    private void UpdateValues()
    {
        // Out Of Control
        mv_state.isOutOfControl = mv_value.outOfControllDuration > 0f;

        if (mv_state.isOutOfControl)
        {
            mv_value.outOfControllDuration -= _fixedDeltaTime;
            mv_value.worldMoveDir = Vector3.zero;
        }
    }

    private void CalculateMovements()
    {
        if (mv_state.isOutOfControl)
        {
            mv_value.horizonVelocity = Vector3.zero;
            return;
        }

        // XZ 이동속도 계산
        // 공중에서 전방이 막힌 경우 제한 (지상에서는 벽에 붙어서 이동할 수 있도록 허용)
        if (mv_state.isBlocked && !mv_state.onGround)
        {
            mv_value.horizonVelocity = Vector3.zero;
        }
        else // 이동 가능한 경우 : 지상 or 전방이 막히지 않음
        {
            float speed = !mv_state.isMove ? 0f :
                            !mv_state.isRun ? mv_option.speed :
                                                mv_option.speed * mv_option.runningCoef;

            mv_value.horizonVelocity = mv_value.worldMoveDir * speed;
        }

        // 3. XZ 벡터 회전
        if (mv_state.onGround)
        {
            if (mv_state.isMove && !mv_state.isBlocked)
            {
                // 벡터 회전 (경사로)
                mv_value.horizonVelocity = Quaternion.AngleAxis(-mv_value.groundAngle, mv_value.groundCross) * mv_value.horizonVelocity;
            }
        }
    }

    /// <summary> 리지드바디 최종 속도 적용 </summary>
    private void ApplyMovementsToRigidbody()
    {
        if (mv_state.isOutOfControl)
        {
            mv_component.rBody.velocity = new Vector3(mv_component.rBody.velocity.x, mv_value.gravity, mv_component.rBody.velocity.z);
            return;
        }

        mv_component.rBody.velocity = mv_value.horizonVelocity + Vector3.up * (mv_value.gravity);
    }


    private void Rotate()
    {
        if (mv_value.worldMoveDir != Vector3.zero)
        {
            mv_component.rBody.rotation = Quaternion.RotateTowards(mv_component.rBody.rotation, Quaternion.LookRotation(mv_value.worldMoveDir), 20f);
        }
    }

    #endregion
}
