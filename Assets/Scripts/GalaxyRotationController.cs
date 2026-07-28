using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class GalaxyRotationController : MonoBehaviour
{
    [Header("Input")]
    [Min(0.001f)] [SerializeField] private float dragSensitivity = 0.18f;
    [Min(0f)] [SerializeField] private float dragThresholdPixels = 8f;
    [SerializeField] private bool ignoreInputOverUI = true;

    [Header("Rotation")]
    [SerializeField] private bool allowHorizontalRotation = true;
    [SerializeField] private bool allowVerticalRotation = true;
    [SerializeField] private bool invertHorizontal;
    [SerializeField] private bool invertVertical;

    [Header("Vertical Limits")]
    [SerializeField] private bool clampVerticalRotation = true;
    [Range(-89f, 0f)] [SerializeField] private float minimumVerticalAngle = -35f;
    [Range(0f, 89f)] [SerializeField] private float maximumVerticalAngle = 35f;

    [Header("Inertia")]
    [SerializeField] private bool useInertia = true;
    [Min(0.01f)] [SerializeField] private float velocityResponse = 18f;
    [Min(0f)] [SerializeField] private float deceleration = 110f;
    [Min(1f)] [SerializeField] private float maximumAngularSpeed = 420f;
    [Min(0f)] [SerializeField] private float stopSpeed = 0.5f;

    [Header("Tap Filtering")]
    [Min(0f)] [SerializeField] private float tapMovementTolerance = 12f;
    [Min(0f)] [SerializeField] private float maximumTapDuration = 0.35f;

    public bool IsPointerDown => pointerDown;
    public bool IsDragging => dragging;
    public Vector2 AngularVelocity => angularVelocity;
    public bool LastGestureWasTap { get; private set; }

    private bool pointerDown;
    private bool dragging;
    private int activePointerId = int.MinValue;
    private Vector2 pressPosition;
    private Vector2 previousPointerPosition;
    private float pressTime;
    private Vector2 angularVelocity;
    private float currentVerticalAngle;

    private void Awake()
    {
        currentVerticalAngle = NormalizeSignedAngle(transform.localEulerAngles.x);
    }

    private void OnEnable()
    {
        currentVerticalAngle = NormalizeSignedAngle(transform.localEulerAngles.x);
        pointerDown = false;
        dragging = false;
        activePointerId = int.MinValue;
        angularVelocity = Vector2.zero;
        LastGestureWasTap = false;
    }

    private void Update()
    {
        if (TryReadPointer(out PointerState pointer))
        {
            ProcessPointer(pointer);
        }
        else
        {
            if (pointerDown)
            {
                EndPointer(previousPointerPosition);
            }

            ApplyInertia();
        }
    }

    private void ProcessPointer(PointerState pointer)
    {
        if (pointer.PressedThisFrame)
        {
            BeginPointer(pointer);
        }

        if (pointerDown && pointer.PointerId == activePointerId && pointer.IsPressed)
        {
            ContinuePointer(pointer);
        }

        if (pointerDown && pointer.PointerId == activePointerId && pointer.ReleasedThisFrame)
        {
            EndPointer(pointer.Position);
        }
    }

    private void BeginPointer(PointerState pointer)
    {
        if (ignoreInputOverUI && IsPointerOverUI(pointer.PointerId))
        {
            return;
        }

        pointerDown = true;
        dragging = false;
        activePointerId = pointer.PointerId;
        pressPosition = pointer.Position;
        previousPointerPosition = pointer.Position;
        pressTime = Time.unscaledTime;
        angularVelocity = Vector2.zero;
        LastGestureWasTap = false;
    }

    private void ContinuePointer(PointerState pointer)
    {
        Vector2 totalMovement = pointer.Position - pressPosition;

        if (!dragging && totalMovement.sqrMagnitude >= dragThresholdPixels * dragThresholdPixels)
        {
            dragging = true;
        }

        Vector2 frameDelta = pointer.Position - previousPointerPosition;
        previousPointerPosition = pointer.Position;

        if (!dragging)
        {
            return;
        }

        float horizontalSign = invertHorizontal ? -1f : 1f;
        float verticalSign = invertVertical ? -1f : 1f;

        float yawDelta = allowHorizontalRotation
            ? frameDelta.x * dragSensitivity * horizontalSign
            : 0f;

        float pitchDelta = allowVerticalRotation
            ? -frameDelta.y * dragSensitivity * verticalSign
            : 0f;

        ApplyRotation(pitchDelta, yawDelta);

        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        Vector2 targetVelocity = new Vector2(pitchDelta, yawDelta) / deltaTime;
        targetVelocity = Vector2.ClampMagnitude(targetVelocity, maximumAngularSpeed);

        float blend = 1f - Mathf.Exp(-velocityResponse * deltaTime);
        angularVelocity = Vector2.Lerp(angularVelocity, targetVelocity, blend);
    }

    private void EndPointer(Vector2 releasePosition)
    {
        float duration = Time.unscaledTime - pressTime;
        float movement = Vector2.Distance(pressPosition, releasePosition);

        LastGestureWasTap =
            !dragging &&
            duration <= maximumTapDuration &&
            movement <= tapMovementTolerance;

        pointerDown = false;
        dragging = false;
        activePointerId = int.MinValue;

        if (!useInertia)
        {
            angularVelocity = Vector2.zero;
        }
    }

    private void ApplyInertia()
    {
        if (!useInertia || angularVelocity.sqrMagnitude <= 0f)
        {
            return;
        }

        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);

        ApplyRotation(
            angularVelocity.x * deltaTime,
            angularVelocity.y * deltaTime
        );

        float speed = Mathf.MoveTowards(
            angularVelocity.magnitude,
            0f,
            deceleration * deltaTime
        );

        if (speed <= stopSpeed)
        {
            angularVelocity = Vector2.zero;
            return;
        }

        angularVelocity = angularVelocity.normalized * speed;
    }

    private void ApplyRotation(float pitchDelta, float yawDelta)
    {
        if (allowVerticalRotation && !Mathf.Approximately(pitchDelta, 0f))
        {
            float requestedAngle = currentVerticalAngle + pitchDelta;
            float appliedPitch = pitchDelta;

            if (clampVerticalRotation)
            {
                float clampedAngle = Mathf.Clamp(
                    requestedAngle,
                    minimumVerticalAngle,
                    maximumVerticalAngle
                );

                appliedPitch = clampedAngle - currentVerticalAngle;
                currentVerticalAngle = clampedAngle;
            }
            else
            {
                currentVerticalAngle = NormalizeSignedAngle(requestedAngle);
            }

            transform.Rotate(Vector3.right, appliedPitch, Space.Self);
        }

        if (allowHorizontalRotation && !Mathf.Approximately(yawDelta, 0f))
        {
            Vector3 yawAxis = transform.parent != null
                ? transform.parent.up
                : Vector3.up;

            transform.Rotate(yawAxis, yawDelta, Space.World);
        }
    }

    private bool TryReadPointer(out PointerState pointer)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            pointer = new PointerState
            {
                PointerId = touch.fingerId,
                Position = touch.position,
                IsPressed = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled,
                PressedThisFrame = touch.phase == TouchPhase.Began,
                ReleasedThisFrame = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled
            };

            return true;
        }

        bool mouseHeld = Input.GetMouseButton(0);
        bool mouseDown = Input.GetMouseButtonDown(0);
        bool mouseUp = Input.GetMouseButtonUp(0);

        if (mouseHeld || mouseDown || mouseUp)
        {
            pointer = new PointerState
            {
                PointerId = -1,
                Position = Input.mousePosition,
                IsPressed = mouseHeld,
                PressedThisFrame = mouseDown,
                ReleasedThisFrame = mouseUp
            };

            return true;
        }

        pointer = default;
        return false;
    }

    private static bool IsPointerOverUI(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(pointerId);
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
        {
            angle -= 360f;
        }
        else if (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    private struct PointerState
    {
        public int PointerId;
        public Vector2 Position;
        public bool IsPressed;
        public bool PressedThisFrame;
        public bool ReleasedThisFrame;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maximumVerticalAngle = Mathf.Max(maximumVerticalAngle, 0f);
        minimumVerticalAngle = Mathf.Min(minimumVerticalAngle, 0f);
        maximumAngularSpeed = Mathf.Max(maximumAngularSpeed, 1f);
        tapMovementTolerance = Mathf.Max(tapMovementTolerance, dragThresholdPixels);
    }
#endif
}