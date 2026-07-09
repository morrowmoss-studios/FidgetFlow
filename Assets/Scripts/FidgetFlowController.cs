using UnityEngine;
using UnityEngine.UI;

public class FidgetFlowController : MonoBehaviour
{
    public Material fidgetMaterial;

    public Slider foldCountSlider;
    public Slider noiseScaleSlider;
    public Slider flowSpeedSlider;
    public Slider warpStrengthSlider;
    public Slider rotationSpeedSlider;
    public Slider octavesSlider;
    public Slider rampTilingSlider;
    public Slider rampContrastSlider;
    public Slider angleColorShiftSlider;

    void Start()
    {
        // set slider values to match current material defaults
        foldCountSlider.value = fidgetMaterial.GetFloat("_FoldCount");
        noiseScaleSlider.value = fidgetMaterial.GetFloat("_NoiseScale");
        flowSpeedSlider.value = fidgetMaterial.GetFloat("_FlowSpeed");
        warpStrengthSlider.value = fidgetMaterial.GetFloat("_WarpStrength");
        rotationSpeedSlider.value = fidgetMaterial.GetFloat("_RotationSpeed");
        octavesSlider.value = fidgetMaterial.GetFloat("_Octaves");
        rampTilingSlider.value = fidgetMaterial.GetFloat("_RampTiling");
        rampContrastSlider.value = fidgetMaterial.GetFloat("_RampContrast");
        angleColorShiftSlider.value = fidgetMaterial.GetFloat("_AngleColorShift");

        // wire up listeners
        foldCountSlider.onValueChanged.AddListener(v => fidgetMaterial.SetFloat("_FoldCount", v));
        noiseScaleSlider.onValueChanged.AddListener(v => fidgetMaterial.SetFloat("_NoiseScale", v));
        flowSpeedSlider.onValueChanged.AddListener(v => fidgetMaterial.SetFloat("_FlowSpeed", v));
        warpStrengthSlider.onValueChanged.AddListener(v => fidgetMaterial.SetFloat("_WarpStrength", v));
        rotationSpeedSlider.onValueChanged.AddListener(v => fidgetMaterial.SetFloat("_RotationSpeed", v));
        octavesSlider.onValueChanged.AddListener(v => fidgetMaterial.SetFloat("_Octaves", v));
        rampTilingSlider.onValueChanged.AddListener(v => fidgetMaterial.SetFloat("_RampTiling", v));
        rampContrastSlider.onValueChanged.AddListener(v => fidgetMaterial.SetFloat("_RampContrast", v));
        angleColorShiftSlider.onValueChanged.AddListener(v => fidgetMaterial.SetFloat("_AngleColorShift", v));
    }
}