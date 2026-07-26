using UnityEngine;

[DisallowMultipleComponent]
public class StarloomVineMotionAccessory :
    MonoBehaviour,
    IPortalAccessory
{
    [Header("Motion Output")]
    [Range(0f, 2f)]
    [SerializeField] private float motionMultiplier = 1f;

    [Tooltip("Global shader float available to StarLoom accessories.")]
    [SerializeField] private string motionPropertyName =
        "_PortalMotionEnergy";

    private int motionPropertyId;
    private float motionEnergy;
    private bool selected;

    private void Awake()
    {
        motionPropertyId =
            Shader.PropertyToID(motionPropertyName);
    }

    private void Update()
    {
        float output =
            Mathf.Clamp01(
                motionEnergy *
                motionMultiplier
            );

        if (selected)
        {
            output *= 0.65f;
        }

        Shader.SetGlobalFloat(
            motionPropertyId,
            output
        );
    }

    public void SetMotionEnergy(float value)
    {
        motionEnergy = Mathf.Clamp01(value);
    }

    public void SetSelected(bool value)
    {
        selected = value;
    }

    public void OnPortalOpening()
    {
        motionEnergy = 1f;
    }
}