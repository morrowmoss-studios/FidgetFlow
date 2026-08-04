using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class GalaxyRotationController : MonoBehaviour
{
    [Header("Input")]
    [Min(0.001f)]
    [SerializeField] private float dragSensitivity = 0.18f;

    [Min(0f)]
    [SerializeField] private float dragThresholdPixels = 8f;

    [SerializeField] private bool ignoreInputOverUI = true;

    [Header("Rotation")]
    [SerializeField] private bool allowHorizontalRotation = true;
    [SerializeField] private bool allowVerticalRotation = true;
    [SerializeField] private bool invertHorizontal;
    [SerializeField] private bool invertVertical;

    [Header("Vertical Limits")]
    [SerializeField] private bool clampVerticalRotation = true;

    [Range(-89f, 0f)]
    [SerializeField] private float minimumVerticalAngle = -35f;

    [Range(0f, 89f)]
    [SerializeField] private float maximumVerticalAngle = 35f;

    [Header("Inertia")]
    [SerializeField] private bool useInertia = true;

    [Min(0f)]
    [SerializeField] private float velocityResponse = 18f;

    [Min(0f)]
    [SerializeField] private float deceleration = 110f;

    [Min(0f)]
    [SerializeField] private float maximumAngularSpeed = 420f;

    [Min(0f)]
    [SerializeField] private float stopSpeed = 0.5f;

    [Header("Tap Filtering")]
    [Min(0f)]
    [SerializeField] private float tapMovementTolerance = 12f;

    [Min(0f)]
    [SerializeField] private float maximumTapDuration = 0.35f;

    [Header("Automatic Rotation")]
    [SerializeField] private bool useAutomaticRotation = true;

    [Tooltip("Clockwise degrees per second while untouched.")]
    [Min(0f)]
    [SerializeField] private float automaticClockwiseSpeed = 2.25f;

    [Tooltip("How long after release before the idle rotation begins returning.")]
    [Min(0f)]
    [SerializeField] private float automaticRotationDelay = 1.25f;

    [Tooltip("How smoothly idle rotation returns after user input/inertia.")]
    [Min(0.01f)]
    [SerializeField] private float automaticRotationResponse = 2.5f;

    public Vector2 CurrentAngularVelocity => angularVelocity;
    public float CurrentAngularSpeed => angularVelocity.magnitude;
    public bool IsUserDragging => isDragging;

    private Vector2 angularVelocity;
    private Vector2 lastPointerPosition;
    private Vector2 pointerDownPosition;

    private float verticalAngle;
    private float pointerDownTime;
    private float lastUserInputTime = -100f;

    private bool pointerHeld;
    private bool isDragging;
    private bool pointerStartedOverUI;

    private void Awake()
    {
        Vector3 euler = transform.localEulerAngles;
        verticalAngle = NormalizeAngle(euler.x);
    }

    private void Update()
    {
        HandlePointerInput();

        if (!pointerHeld)
        {
            ApplyFreeMotion();
        }
    }

    private void HandlePointerInput()
    {
        bool down;
        bool held;
        bool up;
        Vector2 pointerPosition;

        ReadPointer(out down, out held, out up, out pointerPosition);

        if (down)
        {
            pointerHeld = true;
            isDragging = false;
            pointerDownPosition = pointerPosition;
            lastPointerPosition = pointerPosition;
            pointerDownTime = Time.unscaledTime;
            pointerStartedOverUI =
                ignoreInputOverUI &&
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject();

            angularVelocity = Vector2.zero;
            lastUserInputTime = Time.unscaledTime;
        }

        if (held && pointerHeld && !pointerStartedOverUI)
        {
            Vector2 totalMovement = pointerPosition - pointerDownPosition;

            if (!isDragging && totalMovement.magnitude >= dragThresholdPixels)
            {
                isDragging = true;
            }

            if (isDragging)
            {
                Vector2 delta = pointerPosition - lastPointerPosition;
                ApplyDrag(delta);
                lastUserInputTime = Time.unscaledTime;
            }

            lastPointerPosition = pointerPosition;
        }

        if (up && pointerHeld)
        {
            pointerHeld = false;

            float heldDuration = Time.unscaledTime - pointerDownTime;
            float movement = (pointerPosition - pointerDownPosition).magnitude;

            bool wasTap =
                heldDuration <= maximumTapDuration &&
                movement <= tapMovementTolerance;

            if (wasTap)
            {
                angularVelocity = Vector2.zero;
            }

            isDragging = false;
            pointerStartedOverUI = false;
            lastUserInputTime = Time.unscaledTime;
        }
    }

    private void ApplyDrag(Vector2 pixelDelta)
    {
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);

        float horizontalDelta =
            allowHorizontalRotation
                ? pixelDelta.x * dragSensitivity * (invertHorizontal ? -1f : 1f)
                : 0f;

        float verticalDelta =
            allowVerticalRotation
                ? pixelDelta.y * dragSensitivity * (invertVertical ? 1f : -1f)
                : 0f;

        ApplyRotation(horizontalDelta, verticalDelta);

        if (useInertia)
        {
            Vector2 measuredVelocity =
                new Vector2(horizontalDelta, verticalDelta) / dt;

            measuredVelocity =
                Vector2.ClampMagnitude(
                    measuredVelocity,
                    maximumAngularSpeed
                );

            float blend =
                1f -
                Mathf.Exp(
                    -velocityResponse *
                    dt
                );

            angularVelocity =
                Vector2.Lerp(
                    angularVelocity,
                    measuredVelocity,
                    blend
                );
        }
        else
        {
            angularVelocity = Vector2.zero;
        }
    }

    private void ApplyFreeMotion()
    {
        float dt = Time.unscaledDeltaTime;
        bool inertiaActive =
            useInertia &&
            angularVelocity.magnitude > stopSpeed;

        if (inertiaActive)
        {
            ApplyRotation(
                angularVelocity.x * dt,
                angularVelocity.y * dt
            );

            angularVelocity =
                Vector2.MoveTowards(
                    angularVelocity,
                    Vector2.zero,
                    deceleration * dt
                );

            return;
        }

        angularVelocity = Vector2.zero;

        if (
            !useAutomaticRotation ||
            Time.unscaledTime - lastUserInputTime < automaticRotationDelay
        )
        {
            return;
        }

        /*
         * Constant basketball-style spin around the vertical Y axis.
         * Negative Y produces the requested clockwise direction for
         * the current camera-facing layout.
         */
        float targetSpeed = -automaticClockwiseSpeed;
        float response =
            1f -
            Mathf.Exp(
                -automaticRotationResponse *
                dt
            );

        angularVelocity.x =
            Mathf.Lerp(
                angularVelocity.x,
                targetSpeed,
                response
            );

        transform.Rotate(
            0f,
            targetSpeed * dt,
            0f,
            Space.World
        );
    }

    private void ApplyRotation(float horizontalDegrees, float verticalDegrees)
    {
        if (Mathf.Abs(horizontalDegrees) > 0.0001f)
        {
            /*
             * Spin the galaxy like a basketball on a finger:
             * around its vertical Y axis.
             */
            transform.Rotate(
                0f,
                horizontalDegrees,
                0f,
                Space.World
            );
        }

        if (Mathf.Abs(verticalDegrees) > 0.0001f)
        {
            float requestedVertical = verticalAngle + verticalDegrees;

            if (clampVerticalRotation)
            {
                requestedVertical =
                    Mathf.Clamp(
                        requestedVertical,
                        minimumVerticalAngle,
                        maximumVerticalAngle
                    );
            }

            float appliedVertical = requestedVertical - verticalAngle;
            verticalAngle = requestedVertical;

            transform.Rotate(
                appliedVertical,
                0f,
                0f,
                Space.Self
            );
        }
    }

    private static void ReadPointer(
        out bool down,
        out bool held,
        out bool up,
        out Vector2 position
    )
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            position = touch.position;
            down = touch.phase == TouchPhase.Began;
            held =
                touch.phase == TouchPhase.Began ||
                touch.phase == TouchPhase.Moved ||
                touch.phase == TouchPhase.Stationary;
            up =
                touch.phase == TouchPhase.Ended ||
                touch.phase == TouchPhase.Canceled;
            return;
        }

        position = Input.mousePosition;
        down = Input.GetMouseButtonDown(0);
        held = Input.GetMouseButton(0);
        up = Input.GetMouseButtonUp(0);
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}