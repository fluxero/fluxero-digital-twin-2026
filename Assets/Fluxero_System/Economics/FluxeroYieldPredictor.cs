using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Replaces the original GetProjectedH2/GetProjectedRevenue with a full
/// multi-timeframe forecast list that FluxeroDashboardV2 can iterate.
/// Field names match your actual FluxeroSiteProfile:
///   baseYearlyDegradationRate  (not baseYearlyDegradationRate)
///   h2SalePricePerKg           (not h2SalePricePerKg)
/// </summary>
public class FluxeroYieldPredictor : MonoBehaviour
{
    [Header("References")]
    public FluxeroSimulationEngine engine;

    public enum Timeframe
    {
        RealTime, FiveMins, OneMonth, OneYear, FiveYears
    }

    // kept for any existing UI dropdowns
    public Timeframe currentTimeframe;

    // ── New: full forecast list for dashboard table ───────────────────
    [System.Serializable]
    public struct YieldForecast
    {
        public string displayLabel;
        public float  projectedH2Kg;
        public float  projectedRevenueGBP;
        public float  projectedCapexPaybackPct;
        public string unit;
    }

    public List<YieldForecast> Forecasts { get; private set; } = new List<YieldForecast>();

    // ── Original methods — kept so nothing else breaks ────────────────

    public float GetProjectedH2()
    {
        float hours = TimeframeToHours(currentTimeframe);
        float rate  = engine.state.instantaneousH2RateKgHr;

        // use your actual field name: baseYearlyDegradationRate
        if (currentTimeframe == Timeframe.FiveYears)
            rate *= Mathf.Pow(1f - engine.profile.baseYearlyDegradationRate, 5f);

        return rate * hours;
    }

    // use your actual field name: h2SalePricePerKg
    public float GetProjectedRevenue() => GetProjectedH2() * engine.profile.h2SalePricePerKg;

    // ── New: called by dashboard Refresh button ───────────────────────

    public void RecalculatePredictions()
    {
        Forecasts.Clear();
        if (engine == null || engine.profile == null) return;

        Forecasts.Add(BuildForecast("5 Minutes",  5f / 60f,  0f));
        Forecasts.Add(BuildForecast("1 Hour",     1f,        0f));
        Forecasts.Add(BuildForecast("1 Month",    730f,      0f));
        Forecasts.Add(BuildForecast("1 Year",     8760f,     1f));
        Forecasts.Add(BuildForecast("5 Years",    43800f,    5f));
    }

    private YieldForecast BuildForecast(string label, float hours, float degradationYears)
    {
        float rate = engine.state.instantaneousH2RateKgHr;

        // baseYearlyDegradationRate — your actual field name
        if (degradationYears > 0f)
            rate *= Mathf.Pow(1f - engine.profile.baseYearlyDegradationRate, degradationYears);

        float kg  = rate * hours;

        // h2SalePricePerKg — your actual field name
        float rev = kg * engine.profile.h2SalePricePerKg
                  - (engine.profile.opexPerYearGBP * (hours / 8760f));

        float payback = engine.profile.capexGBP > 0f ? (rev / engine.profile.capexGBP) * 100f : 0f;

        return new YieldForecast
        {
            displayLabel             = label,
            projectedH2Kg            = kg,
            projectedRevenueGBP      = rev,
            projectedCapexPaybackPct = payback,
            unit                     = ScaleUnit(kg)
        };
    }

    private float TimeframeToHours(Timeframe tf)
    {
        switch (tf)
        {
            case Timeframe.FiveMins:  return 5f / 60f;
            case Timeframe.OneMonth:  return 730f;
            case Timeframe.OneYear:   return 8760f;
            case Timeframe.FiveYears: return 43800f;
            default:                  return Time.deltaTime / 3600f;
        }
    }

    private string ScaleUnit(float kg)
    {
        if      (kg < 0.001f) return "mg";
        else if (kg < 1f)     return "g";
        else if (kg < 1000f)  return "kg";
        else                  return "t";
    }
}