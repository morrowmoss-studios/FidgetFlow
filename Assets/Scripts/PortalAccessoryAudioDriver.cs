using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PortalAccessoryAudioDriver : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private AudioReactivityManager audioManager;

    [Tooltip("Leave empty to automatically find every IPortalAccessory below this object.")]
    [SerializeField] private MonoBehaviour[] accessoryBehaviours;

    [Header("Response")]
    [Min(0f)]
    [SerializeField] private float responseSpeed = 14f;

    [Range(0f, 3f)]
    [SerializeField] private float lavaLampStrength = 1.25f;

    [Range(0f, 3f)]
    [SerializeField] private float plasmaStrength = 1.10f;

    [Range(0f, 3f)]
    [SerializeField] private float fibonacciStrength = 1.00f;

    [Range(0f, 3f)]
    [SerializeField] private float kaleidoscopeStrength = 1.15f;

    [Range(0f, 3f)]
    [SerializeField] private float defaultStrength = 1.00f;

    private readonly List<AccessoryEntry> accessories = new();

    private sealed class AccessoryEntry
    {
        public IPortalAccessory Accessory;
        public MonoBehaviour Behaviour;
        public float SmoothedEnergy;
    }

    private void Awake()
    {
        ResolveAudioManager();
        RebuildAccessoryList();
    }

    private void OnEnable()
    {
        ResolveAudioManager();

        if (accessories.Count == 0)
        {
            RebuildAccessoryList();
        }
    }

    private void Update()
    {
        if (audioManager == null)
        {
            ResolveAudioManager();

            if (audioManager == null)
            {
                return;
            }
        }

        float interpolation =
            1f -
            Mathf.Exp(
                -responseSpeed *
                Time.unscaledDeltaTime
            );

        for (int i = accessories.Count - 1; i >= 0; i--)
        {
            AccessoryEntry entry = accessories[i];

            if (
                entry == null ||
                entry.Behaviour == null ||
                entry.Accessory == null
            )
            {
                accessories.RemoveAt(i);
                continue;
            }

            float targetEnergy =
                GetTargetEnergy(
                    entry.Behaviour
                );

            entry.SmoothedEnergy =
                Mathf.Lerp(
                    entry.SmoothedEnergy,
                    targetEnergy,
                    interpolation
                );

            entry.Accessory.SetMotionEnergy(
                entry.SmoothedEnergy
            );
        }
    }

    [ContextMenu("Rebuild Accessory List")]
    public void RebuildAccessoryList()
    {
        accessories.Clear();

        if (
            accessoryBehaviours == null ||
            accessoryBehaviours.Length == 0
        )
        {
            accessoryBehaviours =
                GetComponentsInChildren<MonoBehaviour>(
                    true
                );
        }

        foreach (MonoBehaviour behaviour in accessoryBehaviours)
        {
            if (
                behaviour == null ||
                behaviour == this
            )
            {
                continue;
            }

            if (!(behaviour is IPortalAccessory accessory))
            {
                continue;
            }

            accessories.Add(
                new AccessoryEntry
                {
                    Accessory = accessory,
                    Behaviour = behaviour,
                    SmoothedEnergy = 0f
                }
            );
        }
    }

    private float GetTargetEnergy(
        MonoBehaviour behaviour
    )
    {
        string typeName =
            behaviour.GetType().Name;

        float value;
        float strength;

        switch (typeName)
        {
            case "LavaLampBlobs":
                value =
                    audioManager._bass * 0.70f +
                    audioManager._energy * 0.30f;

                strength =
                    lavaLampStrength;
                break;

            case "PlasmaOverflow":
                value =
                    audioManager._energy * 0.65f +
                    audioManager._mid * 0.20f +
                    audioManager._high * 0.15f;

                strength =
                    plasmaStrength;
                break;

            case "FibonacciRingSpirals":
                value =
                    audioManager._bass * 0.40f +
                    audioManager._mid * 0.40f +
                    audioManager._energy * 0.20f;

                strength =
                    fibonacciStrength;
                break;

            case "KaleidoscopeRingFacets":
                value =
                    audioManager._high * 0.50f +
                    audioManager._mid * 0.30f +
                    audioManager._energy * 0.20f;

                strength =
                    kaleidoscopeStrength;
                break;

            default:
                value =
                    audioManager._energy;

                strength =
                    defaultStrength;
                break;
        }

        return Mathf.Clamp01(
            value *
            strength
        );
    }

    private void ResolveAudioManager()
    {
        if (audioManager != null)
        {
            return;
        }

        audioManager =
            FindFirstObjectByType<AudioReactivityManager>();
    }
}