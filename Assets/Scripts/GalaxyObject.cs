using UnityEngine;

public class GalaxyObject : MonoBehaviour
{
    [Header("Stable Identity")]
    [Tooltip("Give every object a unique permanent ID. Do not change it after release.")]
    [SerializeField] private string stableId;

    [Header("Spacing")]
    [Tooltip("Multiplier for how much personal space this object needs.")]
    [Min(0.25f)]
    [SerializeField] private float spacingMultiplier = 1f;

    [Header("Placement")]
    [Tooltip("Prevents this object from being repositioned by the galaxy generator.")]
    [SerializeField] private bool lockPosition;

    public string StableId => stableId;
    public float SpacingMultiplier => spacingMultiplier;
    public bool LockPosition => lockPosition;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(stableId))
        {
            stableId = gameObject.name;
        }
    }
#endif
}