using UnityEngine;

/// <summary>
/// ScriptableObject — one asset per site/configuration.
/// Create via right-click → Fluxero → Site Profile.
/// </summary>
[CreateAssetMenu(fileName = "NewSiteProfile", menuName = "Fluxero/Site Profile")]
public class FluxeroSiteProfile : ScriptableObject
{
    [Header("Site Identity")]
    public string siteName     = "Lab Bench MVP";
    public string siteLocation = "Bench A";

    // ── Power Source ────────────────────────────────────────────────
    [Header("Power Source")]
    public float nominalPowerKW     = 0.01f;    // 10 W lab default
    [Range(0f, 1f)]
    public float volatilityFactor   = 0f;       // 0 = flat PSU, 0.8 = gusty wind
    public float voltageSetpoint    = 12f;      // V  (lab PSU)
    public float currentSetpoint    = 0.83f;    // A  (lab PSU)

    // ── Electrolyzer Stack ──────────────────────────────────────────
    [Header("Electrolyzer Stack")]
    public int   cellCount                    = 1;
    public float cellActiveAreaCm2            = 25f;
    public float nominalCellVoltage           = 1.8f;
    public float thermoNeutralVoltage         = 1.48f;
    public float nominalCurrentDensityAcm2   = 1f;

    // ── Thermal ─────────────────────────────────────────────────────
    [Header("Thermal Properties")]
    public float ambientTempC            = 25f;
    public float maxSafeTempC            = 80f;
    public float thermalMassJperK        = 500f;
    public float thermalResistanceKperW  = 0.15f;

    // ── Degradation ─────────────────────────────────────────────────
    [Header("Degradation")]
    public float baseYearlyDegradationRate = 0.005f;   // 0.5 %/year

    // ── Financial ───────────────────────────────────────────────────
    [Header("Financial")]
    public float capexGBP            = 5000f;
    public float opexPerYearGBP      = 500f;
    public float h2SalePricePerKg    = 8.50f;
}