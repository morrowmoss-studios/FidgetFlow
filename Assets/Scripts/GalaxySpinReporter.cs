using UnityEngine;

[DisallowMultipleComponent]
public class GalaxySpinReporter : MonoBehaviour
{
    [Header("Normalization")]
    [Min(1f)]
    [SerializeField] private float fullEnergyDegreesPerSecond = 180f;

    [Min(0.01f)]
    [SerializeField] private float riseSpeed = 10f;

    [Min(0.01f)]
    [SerializeField] private float fallSpeed = 4f;

    public static Vector3 AngularVelocityLocal { get; private set; }
    public static float MotionEnergy { get; private set; }

    private Quaternion previousLocalRotation;

    private void OnEnable()
    {
        previousLocalRotation = transform.localRotation;
        AngularVelocityLocal = Vector3.zero;
        MotionEnergy = 0f;
    }

    private void LateUpdate()
    {
        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);

        Quaternion deltaRotation =
            transform.localRotation *
            Quaternion.Inverse(previousLocalRotation);

        deltaRotation.ToAngleAxis(
            out float angleDegrees,
            out Vector3 axis
        );

        if (angleDegrees > 180f)
        {
            angleDegrees -= 360f;
        }

        if (
            float.IsNaN(axis.x) ||
            float.IsNaN(axis.y) ||
            float.IsNaN(axis.z)
        )
        {
            axis = Vector3.zero;
        }

        AngularVelocityLocal =
            axis.normalized *
            (angleDegrees / deltaTime);

        float targetEnergy =
            Mathf.Clamp01(
                AngularVelocityLocal.magnitude /
                fullEnergyDegreesPerSecond
            );

        float smoothingSpeed =
            targetEnergy > MotionEnergy
                ? riseSpeed
                : fallSpeed;

        MotionEnergy =
            Mathf.MoveTowards(
                MotionEnergy,
                targetEnergy,
                smoothingSpeed * deltaTime
            );

        previousLocalRotation = transform.localRotation;
    }

    private void OnDisable()
    {
        AngularVelocityLocal = Vector3.zero;
        MotionEnergy = 0f;
    }
}
