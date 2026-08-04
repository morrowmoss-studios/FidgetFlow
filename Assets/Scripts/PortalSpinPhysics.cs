using UnityEngine;

[DisallowMultipleComponent]
public sealed class PortalSpinPhysics : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private GalaxyRotationController galaxyRotation;

    [Tooltip("The PortalVisualRoot that should lean and lag. Leave empty to use this transform.")]
    [SerializeField] private Transform visualRoot;

    [Header("Inertial Lean")]
    [Tooltip("Maximum visible lean caused by galaxy angular velocity.")]
    [Range(0f, 24f)]
    [SerializeField] private float maximumLeanDegrees = 8f;

    [Tooltip("Angular speed required to reach maximum lean.")]
    [Min(1f)]
    [SerializeField] private float fullLeanSpeed = 180f;

    [Tooltip("How quickly the portal moves toward the target lean.")]
    [Min(0.01f)]
    [SerializeField] private float leanResponse = 7f;

    [Header("Tangential Lag")]
    [Tooltip("Small local offset opposite the current direction of motion.")]
    [Range(0f, 0.25f)]
    [SerializeField] private float maximumLagDistance = 0.055f;

    [Tooltip("How quickly the portal catches up with its base position.")]
    [Min(0.01f)]
    [SerializeField] private float lagResponse = 5f;

    [Header("Spin Compression")]
    [Tooltip("Subtle stretch across the motion axis at high speed.")]
    [Range(0f, 0.2f)]
    [SerializeField] private float maximumStretch = 0.045f;

    [Min(0.01f)]
    [SerializeField] private float scaleResponse = 8f;

    [Header("Settling")]
    [Tooltip("Small secondary wobble after abrupt changes in spin.")]
    [Range(0f, 12f)]
    [SerializeField] private float wobbleDegrees = 2.5f;

    [Min(0.1f)]
    [SerializeField] private float wobbleFrequency = 4.5f;

    [Min(0.1f)]
    [SerializeField] private float wobbleDamping = 3.5f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 baseLocalScale;

    private Vector2 previousVelocity;
    private float wobbleStrength;
    private float wobblePhase;

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (galaxyRotation == null)
        {
            galaxyRotation =
                GetComponentInParent<GalaxyRotationController>();
        }

        baseLocalPosition = visualRoot.localPosition;
        baseLocalRotation = visualRoot.localRotation;
        baseLocalScale = visualRoot.localScale;
    }

    private void LateUpdate()
    {
        if (galaxyRotation == null || visualRoot == null)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        Vector2 velocity = galaxyRotation.CurrentAngularVelocity;
        float speed01 =
            Mathf.Clamp01(
                velocity.magnitude /
                fullLeanSpeed
            );

        Vector2 acceleration =
            (velocity - previousVelocity) /
            Mathf.Max(dt, 0.0001f);

        wobbleStrength =
            Mathf.Clamp(
                wobbleStrength +
                acceleration.magnitude * 0.00006f,
                0f,
                1f
            );

        wobbleStrength =
            Mathf.MoveTowards(
                wobbleStrength,
                0f,
                wobbleDamping * dt
            );

        wobblePhase +=
            wobbleFrequency *
            Mathf.PI *
            2f *
            dt;

        float wobble =
            Mathf.Sin(wobblePhase) *
            wobbleDegrees *
            wobbleStrength;

        /*
         * A rotating object visually lags opposite the acceleration.
         * Horizontal galaxy velocity produces roll; vertical velocity
         * produces pitch. This is a restrained UI approximation rather
         * than a literal rigid-body simulation.
         */
        float targetPitch =
            Mathf.Clamp(
                velocity.y / fullLeanSpeed,
                -1f,
                1f
            ) *
            maximumLeanDegrees;

        float targetRoll =
            Mathf.Clamp(
                -velocity.x / fullLeanSpeed,
                -1f,
                1f
            ) *
            maximumLeanDegrees;

        Quaternion targetRotation =
            baseLocalRotation *
            Quaternion.Euler(
                targetPitch + wobble * 0.45f,
                0f,
                targetRoll + wobble
            );

        float rotationBlend =
            1f -
            Mathf.Exp(
                -leanResponse *
                dt
            );

        visualRoot.localRotation =
            Quaternion.Slerp(
                visualRoot.localRotation,
                targetRotation,
                rotationBlend
            );

        Vector3 targetLag =
            baseLocalPosition +
            new Vector3(
                -velocity.x,
                -velocity.y,
                0f
            ).normalized *
            maximumLagDistance *
            speed01;

        float positionBlend =
            1f -
            Mathf.Exp(
                -lagResponse *
                dt
            );

        visualRoot.localPosition =
            Vector3.Lerp(
                visualRoot.localPosition,
                targetLag,
                positionBlend
            );

        float stretch = maximumStretch * speed01;

        Vector3 targetScale =
            new Vector3(
                baseLocalScale.x * (1f + stretch),
                baseLocalScale.y * (1f - stretch * 0.55f),
                baseLocalScale.z
            );

        float scaleBlend =
            1f -
            Mathf.Exp(
                -scaleResponse *
                dt
            );

        visualRoot.localScale =
            Vector3.Lerp(
                visualRoot.localScale,
                targetScale,
                scaleBlend
            );

        previousVelocity = velocity;
    }

    private void OnDisable()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition = baseLocalPosition;
        visualRoot.localRotation = baseLocalRotation;
        visualRoot.localScale = baseLocalScale;
    }
}