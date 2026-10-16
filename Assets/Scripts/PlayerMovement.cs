using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Half-Life (GoldSrc) style movement on a Rigidbody + CapsuleCollider.
/// Based on pm_shared.c: PM_Friction, PM_Accelerate, PM_AirAccelerate, PM_Jump.
/// Reference: https://github.com/TheAsuro/VelocityMovement/blob/master/Assets/Scripts/StrafeMovement.cs
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class QuakeMovement : MonoBehaviour
{
    private const float GroundProbeSkin = 0.01f;
    private const float MinFrictionSpeed = 0.1f;
    private const float CrouchSpeedMultiplier = 0.5f;

    [Header("Ground")]
    [SerializeField] private float _maxSpeed = 10f;
    [Tooltip("Ground acceleration in m/s². Equivalent to sv_accelerate * maxSpeed.")]
    [SerializeField] private float _acceleration = 100f;
    [Tooltip("Like sv_friction.")]
    [SerializeField] private float _friction = 4f;
    [Tooltip("Below this speed friction uses this value instead of the current speed, so you stop quickly (sv_stopspeed).")]
    [SerializeField] private float _stopSpeed = 1.5f;

    [Header("Air")]
    [Tooltip("Air acceleration in m/s². Equivalent to sv_airaccelerate * maxSpeed.")]
    [SerializeField] private float _airAcceleration = 100f;
    [Tooltip("Cap applied to the wish speed while airborne. This is what makes air strafing work.")]
    [SerializeField] private float _maxAirSpeed = 0.76f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _jumpBufferTime = 0.1f;

    [Header("Ground detection")]
    [SerializeField] private LayerMask _groundLayers = ~0;
    [SerializeField] private float _slopeLimit = 45f;
    [SerializeField] private float _groundCheckDistance = 0.01f;

    [Header("References")]
    [SerializeField] private GameObject _cameraObject;
    [SerializeField] private InputActionReference _move;
    [SerializeField] private InputActionReference _jump;
    [SerializeField] private InputActionReference _crouch;

    [Header("Debug (read-only)")]
    [SerializeField] private bool _isGrounded;
    [SerializeField] private float _slopeAngle;
    [SerializeField] private bool _isCrouched;

    private CapsuleCollider _collider;
    private Rigidbody _rigidBody;

    private float _standingHeight;
    private Vector3 _standingCenter;
    private float _lastJumpPressTime = float.NegativeInfinity;

    // ---------------------------------------------------------------- Public API

    public GameObject Camera => _cameraObject;
    public Rigidbody RigidBodyProp => _rigidBody;

    public float Speed => _rigidBody.linearVelocity.magnitude;
    public float HorizontalSpeed => HorizontalOf(_rigidBody.linearVelocity).magnitude;

    /// <summary>Touching any ground surface, walkable or not.</summary>
    public bool IsGrounded => _isGrounded;
    public float SlopeAngle => _slopeAngle;

    /// <summary>Max wish speed for the current state (ground or air, crouched or not).</summary>
    public float CurrentMaxSpeed => (IsOnWalkableGround ? _maxSpeed : _maxAirSpeed) * SpeedMultiplier;

    public Vector2 InputVector() => _move.action.ReadValue<Vector2>();

    // ---------------------------------------------------------------- Derived state

    private bool IsOnWalkableGround => _isGrounded && _slopeAngle <= _slopeLimit;
    private float SpeedMultiplier => _isCrouched ? CrouchSpeedMultiplier : 1f;

    // ---------------------------------------------------------------- Unity messages

    private void Awake()
    {
        _collider = GetComponent<CapsuleCollider>();
        _rigidBody = GetComponent<Rigidbody>();

        _standingHeight = _collider.height;
        _standingCenter = _collider.center;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        _crouch.action.performed += OnCrouchPerformed;
        _crouch.action.canceled += OnCrouchCanceled;
    }

    private void OnDisable()
    {
        _crouch.action.performed -= OnCrouchPerformed;
        _crouch.action.canceled -= OnCrouchCanceled;
        SetCrouched(false);
    }

    private void Update()
    {
        // Holding jump keeps the buffer topped up, so you re-jump the moment you land.
        if (_jump.action.IsPressed())
            _lastJumpPressTime = Time.time;
    }

    private void FixedUpdate()
    {
        UpdateGroundState();

        Vector3 velocity = _rigidBody.linearVelocity;
        Vector3 wishDir = GetWishDirection(out float wishSpeed);

        if (IsOnWalkableGround)
        {
            velocity = ApplyFriction(velocity);
            velocity = Accelerate(velocity, wishDir, _acceleration, speedLimit: wishSpeed);
        }
        else
        {
            float airLimit = Mathf.Min(wishSpeed, _maxAirSpeed * SpeedMultiplier);
            velocity = Accelerate(velocity, wishDir, _airAcceleration, speedLimit: airLimit);
        }

        velocity = TryJump(velocity);

        _rigidBody.linearVelocity = velocity;

        // Stops the player sliding down walkable slopes.
        _rigidBody.useGravity = !(IsOnWalkableGround && _slopeAngle > 0f);
    }

    // ---------------------------------------------------------------- Input

    private void OnCrouchPerformed(InputAction.CallbackContext _) => SetCrouched(true);
    private void OnCrouchCanceled(InputAction.CallbackContext _) => SetCrouched(false);

    private void SetCrouched(bool crouched)
    {
        if (_collider == null) return;

        _isCrouched = crouched;

        float height = crouched ? _standingHeight * 0.5f : _standingHeight;
        float heightLost = _standingHeight - height;

        // Shift the center down by half the lost height so the feet stay put.
        _collider.height = height;
        _collider.center = _standingCenter + Vector3.down * (heightLost * 0.5f);
    }

    /// <summary>World-space horizontal wish direction (unit length or zero) and the speed we want to reach.</summary>
    private Vector3 GetWishDirection(out float wishSpeed)
    {
        Vector2 input = Vector2.ClampMagnitude(InputVector(), 1f);
        Quaternion yaw = Quaternion.Euler(0f, _cameraObject.transform.eulerAngles.y, 0f);
        Vector3 wishDir = yaw * new Vector3(input.x, 0f, input.y);

        wishSpeed = input.magnitude * _maxSpeed * SpeedMultiplier;
        return wishDir.sqrMagnitude > 0f ? wishDir.normalized : Vector3.zero;
    }

    // ---------------------------------------------------------------- Ground detection

    private void UpdateGroundState()
    {
        float radius = _collider.radius;
        Bounds bounds = _collider.bounds;

        // Sphere sitting in the bottom cap of the capsule, cast slightly downward.
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius, bounds.center.z);

        _isGrounded = Physics.SphereCast(
            origin,
            radius - GroundProbeSkin,
            Vector3.down,
            out RaycastHit hit,
            _groundCheckDistance + GroundProbeSkin,
            _groundLayers,
            QueryTriggerInteraction.Ignore);

        _slopeAngle = _isGrounded ? Vector3.Angle(Vector3.up, hit.normal) : 0f;
    }

    // ---------------------------------------------------------------- Movement (pm_shared.c)

    /// <summary>PM_Friction. Horizontal only; skipped while jump is held so you can bunny hop.</summary>
    private Vector3 ApplyFriction(Vector3 velocity)
    {
        if (_jump.action.IsPressed())
            return velocity;

        Vector3 horizontal = HorizontalOf(velocity);
        float speed = horizontal.magnitude;
        if (speed < MinFrictionSpeed)
            return velocity;

        float control = Mathf.Max(speed, _stopSpeed);
        float drop = control * _friction * Time.fixedDeltaTime;
        float scale = Mathf.Max(speed - drop, 0f) / speed;

        return new Vector3(velocity.x * scale, velocity.y, velocity.z * scale);
    }

    /// <summary>
    /// PM_Accelerate / PM_AirAccelerate. Only adds speed along wishDir up to speedLimit,
    /// so turning while airborne gains speed instead of being capped by the vector length.
    /// </summary>
    private static Vector3 Accelerate(Vector3 velocity, Vector3 wishDir, float acceleration, float speedLimit)
    {
        float currentSpeed = Vector3.Dot(velocity, wishDir);
        float addSpeed = speedLimit - currentSpeed;
        if (addSpeed <= 0f)
            return velocity;

        float accelSpeed = Mathf.Min(acceleration * Time.fixedDeltaTime, addSpeed);
        return velocity + wishDir * accelSpeed;
    }

    /// <summary>PM_Jump. Uses a short input buffer so a press just before landing still counts.</summary>
    private Vector3 TryJump(Vector3 velocity)
    {
        bool buffered = Time.time < _lastJumpPressTime + _jumpBufferTime;
        if (!buffered || !IsOnWalkableGround || velocity.y >= _jumpForce)
            return velocity;

        _lastJumpPressTime = float.NegativeInfinity;
        velocity.y = _jumpForce;
        return velocity;
    }

    private static Vector3 HorizontalOf(Vector3 v) => new Vector3(v.x, 0f, v.z);
}