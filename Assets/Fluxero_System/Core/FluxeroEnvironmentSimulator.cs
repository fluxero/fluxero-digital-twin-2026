using UnityEngine;

/// <summary>
/// Simulates realistic renewable energy environments.
/// Drives the volatilityFactor and nominalPowerKW on the config at runtime.
/// Attach to [FLUXERO_MANAGER] alongside FluxeroSimulationEngine.
/// </summary>
public class FluxeroEnvironmentSimulator : MonoBehaviour
{
    public FluxeroSimulationEngine engine;

    public enum EnvironmentPreset
    {
        Lab_PSU_Flat,           // Programmable PSU — zero volatility
        Wind_Calm,              // ~3 m/s average, low gusts
        Wind_Moderate,          // ~7 m/s average, rolling gusts
        Wind_Gusty,             // ~12 m/s average, sharp spikes
        Wind_Storm,             // ~18 m/s, near-cutout volatility
        Solar_ClearSky,         // Stable irradiance, gentle cloud drift
        Solar_PartlyCloudy,     // Cloud-shadow events, moderate dips
        Solar_Overcast,         // Low flat output, minimal volatility
        Solar_Dynamic,          // Fast-moving clouds, sharp ramps
        Hybrid_WindSolar        // Combined — most complex profile
    }

    [Header("Environment Selection")]
    public EnvironmentPreset preset = EnvironmentPreset.Lab_PSU_Flat;

    [Header("Environmental Conditions")]
    [Range(0f, 25f)]    public float windSpeedMs = 0f;
    [Range(0f, 1000f)]  public float solarIrradianceWm2 = 0f;
    [Range(-10f, 45f)]  public float ambientTempC = 20f;
    [Range(0f, 1f)]     public float cloudCoverFraction = 0f;

    [Header("Site Capacity (overrides config)")]
    public float installedCapacityKW = 0.01f;   // Nameplate capacity of the energy asset

    // --- Derived outputs (read-only, shown in Inspector) ---
    [Header("Live Output (read-only)")]
    [SerializeField] private float _effectivePowerKW;
    [SerializeField] private float _capacityFactor;
    [SerializeField] private float _volatility;

    // Internal
    private float _simTime = 0f;
    private float _cloudShadow = 0f;
    private float _gustPhase = 0f;

    void Update()
    {
        _simTime += Time.deltaTime;

        ApplyPreset(preset);
        SimulateEnvironment();
        PushToEngine();
    }

    private void ApplyPreset(EnvironmentPreset p)
    {
        switch (p)
        {
            case EnvironmentPreset.Lab_PSU_Flat:
                windSpeedMs = 0f; solarIrradianceWm2 = 0f;
                cloudCoverFraction = 0f; break;

            case EnvironmentPreset.Wind_Calm:
                windSpeedMs = Mathf.Lerp(windSpeedMs, 3.5f + Mathf.Sin(_simTime * 0.08f) * 1f, Time.deltaTime);
                cloudCoverFraction = 0.1f; break;

            case EnvironmentPreset.Wind_Moderate:
                windSpeedMs = Mathf.Lerp(windSpeedMs, 7f + Mathf.PerlinNoise(_simTime * 0.15f, 0f) * 4f - 2f, Time.deltaTime * 0.5f);
                cloudCoverFraction = 0.3f; break;

            case EnvironmentPreset.Wind_Gusty:
                _gustPhase += Time.deltaTime * 0.3f;
                float gust = Mathf.PerlinNoise(_gustPhase, 0.7f) * 8f;
                windSpeedMs = Mathf.Lerp(windSpeedMs, 10f + gust, Time.deltaTime * 2f);
                cloudCoverFraction = 0.4f; break;

            case EnvironmentPreset.Wind_Storm:
                _gustPhase += Time.deltaTime * 0.5f;
                windSpeedMs = Mathf.Lerp(windSpeedMs, 16f + Mathf.PerlinNoise(_gustPhase, 0.3f) * 6f, Time.deltaTime * 3f);
                cloudCoverFraction = 0.8f; break;

            case EnvironmentPreset.Solar_ClearSky:
                solarIrradianceWm2 = 850f + Mathf.Sin(_simTime * 0.05f) * 30f;
                cloudCoverFraction = 0.05f; break;

            case EnvironmentPreset.Solar_PartlyCloudy:
                _cloudShadow = Mathf.PerlinNoise(_simTime * 0.1f, 0.5f);
                solarIrradianceWm2 = Mathf.Lerp(200f, 900f, 1f - _cloudShadow * cloudCoverFraction);
                cloudCoverFraction = 0.45f; break;

            case EnvironmentPreset.Solar_Overcast:
                solarIrradianceWm2 = 120f + Mathf.Sin(_simTime * 0.02f) * 40f;
                cloudCoverFraction = 0.9f; break;

            case EnvironmentPreset.Solar_Dynamic:
                _cloudShadow = Mathf.PerlinNoise(_simTime * 0.4f, 0.9f);
                solarIrradianceWm2 = Mathf.Lerp(80f, 950f, _cloudShadow);
                cloudCoverFraction = 0.6f; break;

            case EnvironmentPreset.Hybrid_WindSolar:
                _gustPhase += Time.deltaTime * 0.2f;
                windSpeedMs = 6f + Mathf.PerlinNoise(_gustPhase, 0.1f) * 5f;
                _cloudShadow = Mathf.PerlinNoise(_simTime * 0.15f, 0.5f);
                solarIrradianceWm2 = Mathf.Lerp(100f, 800f, _cloudShadow);
                cloudCoverFraction = 0.5f; break;
        }

        // Ambient temp affects electrolyzer performance slightly
        engine.config.ambientTempC = ambientTempC;
    }

    private void SimulateEnvironment()
    {
        float powerKW = 0f;

        if (preset == EnvironmentPreset.Lab_PSU_Flat)
        {
            // PSU: perfectly flat, user-controlled
            powerKW = installedCapacityKW;
            _volatility = 0f;
        }
        else if (preset.ToString().StartsWith("Wind"))
        {
            // Wind power curve: P ∝ v³, cut-in=3m/s, rated=12m/s, cut-out=25m/s
            powerKW = WindPowerCurve(windSpeedMs, installedCapacityKW);
            _volatility = Mathf.Clamp01((windSpeedMs - 3f) / 15f) * 0.6f
                        + (preset == EnvironmentPreset.Wind_Storm ? 0.3f : 0f);
        }
        else if (preset.ToString().StartsWith("Solar"))
        {
            // Solar: P = irradiance * panel_area * efficiency / 1000
            float panelEfficiency = 0.20f; // 20% typical
            float panelAreaM2 = (installedCapacityKW * 1000f) / (1000f * panelEfficiency); // back-calc area
            powerKW = (solarIrradianceWm2 * panelAreaM2 * panelEfficiency) / 1000f;
            _volatility = cloudCoverFraction * 0.7f;
        }
        else // Hybrid
        {
            float windKW  = WindPowerCurve(windSpeedMs, installedCapacityKW * 0.6f);
            float solarKW = (solarIrradianceWm2 / 1000f) * (installedCapacityKW * 0.4f) * 0.20f;
            powerKW = windKW + solarKW;
            _volatility = 0.5f;
        }

        _effectivePowerKW = Mathf.Max(0f, powerKW);
        _capacityFactor = installedCapacityKW > 0f ? _effectivePowerKW / installedCapacityKW : 0f;
    }

    /// <summary>
    /// IEC standard wind turbine power curve approximation.
    /// </summary>
    private float WindPowerCurve(float windMs, float ratedKW)
    {
        if (windMs < 3f || windMs > 25f) return 0f;     // cut-in / cut-out
        if (windMs >= 12f) return ratedKW;               // rated power plateau
        float t = (windMs - 3f) / (12f - 3f);
        return ratedKW * t * t * t;                      // cubic power curve
    }

    private void PushToEngine()
    {
        if (engine == null || engine.config == null) return;
        engine.config.nominalPowerKW    = _effectivePowerKW;
        engine.config.volatilityFactor  = _volatility;
    }

    // Called by UI
    public void SetPreset(int index) => preset = (EnvironmentPreset)index;
    public void SetInstalledCapacity(float kw) => installedCapacityKW = kw;
    public void SetAmbientTemp(float t) => ambientTempC = t;
    public void SetWindSpeed(float v) => windSpeedMs = v;
    public void SetSolarIrradiance(float w) => solarIrradianceWm2 = w;
}