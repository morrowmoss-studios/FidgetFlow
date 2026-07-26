using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PortalMotionController : MonoBehaviour
{
    [Header("Motion Target")]
    [Tooltip("Assign PortalVisualRoot. Leave empty only if you want this whole object animated.")]
    [SerializeField] private Transform motionTarget;

    [Header("Idle Motion")]
    [SerializeField] private Vector3 idleRotationAxis = Vector3.forward;

    [Min(0f)]
    [SerializeField] private float idleRotationSpeed = 2.5f;

    [Min(0f)]
    [SerializeField] private float floatAmplitude = 0.025f;

    [Min(0f)]
    [SerializeField] private float floatSpeed = 0.65f;

    [Header("Galaxy Spin Reaction")]
    [Range(0f, 35f)]
    [SerializeField] private float maximumTiltDegrees = 10f;

    [Min(0.01f)]
    [SerializeField] private float tiltResponsiveness = 7f;

    [Range(0f, 25f)]
    [SerializeField] private float maximumTwistDegrees = 5f;

    [Range(0f, 0.25f)]
    [SerializeField] private float motionScaleBoost = 0.04f;

    [Header("Selection")]
    [Min(1f)]
    [SerializeField] private float selectedScaleMultiplier = 1.08f;

    [Min(0.01f)]
    [SerializeField] private float selectionResponsiveness = 7f;

    [Header("Accessories")]
    [SerializeField] private MonoBehaviour[] accessoryBehaviours;

    private IPortalAccessory[] accessories;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;

    private float idleAngle;
    private float randomPhase;
    private bool isSelected;
    private bool isOpening;

    public float MotionEnergy { get; private set; }

    private void Awake()
    {
        if (motionTarget == null)
        {
            motionTarget = transform;
        }

        baseLocalPosition = motionTarget.localPosition;
        baseLocalScale = motionTarget.localScale;
        baseLocalRotation = motionTarget.localRotation;
        randomPhase = Random.Range(0f, Mathf.PI * 2f);

        CacheAccessories();
    }

    private void CacheAccessories()
    {
        List<IPortalAccessory> found =
            new List<IPortalAccessory>();

        if (
            accessoryBehaviours == null ||
            accessoryBehaviours.Length == 0
        )
        {
            accessoryBehaviours =
                GetComponentsInChildren<MonoBehaviour>(true);
        }

        foreach (MonoBehaviour behaviour in accessoryBehaviours)
        {
            if (
                behaviour != null &&
                behaviour is IPortalAccessory accessory
            )
            {
                found.Add(accessory);
            }
        }

        accessories = found.ToArray();
    }

    private void LateUpdate()
    {
        if (isOpening)
        {
            return;
        }

        MotionEnergy = GalaxySpinReporter.MotionEnergy;

        UpdateMotion();
        PushAccessoryState();
    }

    private void UpdateMotion()
    {
        float deltaTime = Time.deltaTime;

        idleAngle +=
            idleRotationSpeed *
            deltaTime;

        Vector3 angularVelocity =
            GalaxySpinReporter.AngularVelocityLocal;

        Vector3 planarVelocity =
            new Vector3(
                angularVelocity.x,
                angularVelocity.y,
                0f
            );

        Vector3 tiltAxis =
            new Vector3(
                -planarVelocity.y,
                planarVelocity.x,
                0f
            );

        if (tiltAxis.sqrMagnitude > 0.0001f)
        {
            tiltAxis.Normalize();
        }

        Quaternion inertialTilt =
            Quaternion.AngleAxis(
                maximumTiltDegrees * MotionEnergy,
                tiltAxis
            );

        float signedTwist =
            Mathf.Sign(
                angularVelocity.y +
                angularVelocity.x * 0.5f
            );

        Quaternion spinTwist =
            Quaternion.AngleAxis(
                maximumTwistDegrees *
                MotionEnergy *
                signedTwist,
                Vector3.forward
            );

        Quaternion idleRotation =
            Quaternion.AngleAxis(
                idleAngle,
                idleRotationAxis.normalized
            );

        Quaternion targetRotation =
            baseLocalRotation *
            inertialTilt *
            spinTwist *
            idleRotation;

        float rotationBlend =
            1f -
            Mathf.Exp(
                -tiltResponsiveness *
                deltaTime
            );

        motionTarget.localRotation =
            Quaternion.Slerp(
                motionTarget.localRotation,
                targetRotation,
                rotationBlend
            );

        float bob =
            Mathf.Sin(
                Time.time *
                floatSpeed *
                Mathf.PI *
                2f +
                randomPhase
            ) *
            floatAmplitude;

        motionTarget.localPosition =
            baseLocalPosition +
            Vector3.up * bob;

        float selectedScale =
            isSelected
                ? selectedScaleMultiplier
                : 1f;

        float spinScale =
            1f +
            MotionEnergy *
            motionScaleBoost;

        Vector3 targetScale =
            baseLocalScale *
            selectedScale *
            spinScale;

        float scaleBlend =
            1f -
            Mathf.Exp(
                -selectionResponsiveness *
                deltaTime
            );

        motionTarget.localScale =
            Vector3.Lerp(
                motionTarget.localScale,
                targetScale,
                scaleBlend
            );
    }

    private void PushAccessoryState()
    {
        if (accessories == null)
        {
            return;
        }

        foreach (IPortalAccessory accessory in accessories)
        {
            accessory.SetMotionEnergy(MotionEnergy);
            accessory.SetSelected(isSelected);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        PushAccessoryState();
    }

    public void BeginOpening()
    {
        if (isOpening)
        {
            return;
        }

        isOpening = true;

        if (accessories == null)
        {
            return;
        }

        foreach (IPortalAccessory accessory in accessories)
        {
            accessory.OnPortalOpening();
        }
    }

    public void ResetMotion()
    {
        isOpening = false;
        isSelected = false;
        MotionEnergy = 0f;

        motionTarget.localPosition = baseLocalPosition;
        motionTarget.localRotation = baseLocalRotation;
        motionTarget.localScale = baseLocalScale;
    }
}
