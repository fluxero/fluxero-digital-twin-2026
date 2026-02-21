using UnityEngine;

public class LogoGlow : MonoBehaviour
{
    public FluxeroSimulationEngine engine;
    public Material logoMaterial;

    void Update()
    {
        if (logoMaterial == null) return;
        float intensity = engine.state.instantaneousH2RateKgHr > 0 ? 1f + Mathf.Sin(Time.time * 5f) * 0.5f : 0.2f;
        logoMaterial.SetColor("_EmissionColor", Color.green * intensity);
    }
}