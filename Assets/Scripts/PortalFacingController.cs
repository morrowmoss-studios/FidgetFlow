using UnityEngine;

[DisallowMultipleComponent]
public sealed class PortalFacingController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Facing")]
    [Tooltip("Enable this if the portal's visible front points along local -Z instead of +Z.")]
    [SerializeField] private bool flipForward;

    [Tooltip("Keep the portal upright relative to the camera.")]
    [SerializeField] private bool useCameraUp = true;

    [Header("Smoothing")]
    [SerializeField] private bool smoothRotation = true;

    [Min(0.01f)]
    [SerializeField] private float rotationSpeed = 14f;

    private void Awake()
    {
        ResolveCamera();
    }

    private void OnEnable()
    {
        ResolveCamera();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            ResolveCamera();

            if (targetCamera == null)
            {
                return;
            }
        }

        Vector3 directionToCamera =
            targetCamera.transform.position -
            transform.position;

        if (directionToCamera.sqrMagnitude < 0.000001f)
        {
            return;
        }

        directionToCamera.Normalize();

        if (flipForward)
        {
            directionToCamera = -directionToCamera;
        }

        Vector3 up =
            useCameraUp
                ? targetCamera.transform.up
                : Vector3.up;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                directionToCamera,
                up
            );

        if (!smoothRotation)
        {
            transform.rotation = targetRotation;
            return;
        }

        float blend =
            1f -
            Mathf.Exp(
                -rotationSpeed *
                Time.unscaledDeltaTime
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                blend
            );
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }
}
