using UnityEngine;

public class FluxeroUniversalTwin : MonoBehaviour
{
    public enum SimulationMode { LabBench, CommercialSite }
    public enum Timeframe { RealTime, FiveMins, TenMins, OneMonth, ThreeMonths, OneYear, FiveYears }

    [Header("System Configuration")]
    public SimulationMode activeMode = SimulationMode.LabBench;
    public Timeframe selectedTimeframe = Timeframe.RealTime;

    [Header("Input Energy Source")]
    public float inputCapacity = 10f; // Watts for Lab, kW for Commercial
    [Range(0, 1)] public float environmentLoad = 0.5f; 
    public float assetEfficiency = 0.80f; // Your 80% panel efficiency

    [Header("Technical Specs")]
    public float kwhPerKgH2 = 52.5f; // Industry standard
    public float yearlyDegradation = 0.005f; // 0.5% loss per year

    // Calculations
    public float GetCurrentProductionRatePerHour()
    {
        // Convert Lab Watts to kW for calculation if necessary
        float powerInKW = (activeMode == SimulationMode.LabBench) ? inputCapacity / 1000f : inputCapacity;
        float effectivePower = powerInKW * environmentLoad * assetEfficiency;
        
        return effectivePower / kwhPerKgH2; // Returns kg/hr
    }

    public float GetPredictedOutput()
    {
        float ratePerHour = GetCurrentProductionRatePerHour();
        float hours = 0;

        switch (selectedTimeframe)
        {
            case Timeframe.FiveMins: hours = 5f / 60f; break;
            case Timeframe.TenMins: hours = 10f / 60f; break;
            case Timeframe.OneMonth: hours = 730f; break;
            case Timeframe.ThreeMonths: hours = 2190f; break;
            case Timeframe.OneYear: hours = 8760f; break;
            case Timeframe.FiveYears: hours = 43800f; break;
            default: hours = Time.deltaTime / 3600f; break;
        }

        // Apply degradation for long-term predictions
        if (selectedTimeframe == Timeframe.FiveYears) {
            ratePerHour *= Mathf.Pow(1 - yearlyDegradation, 5);
        }

        return ratePerHour * hours;
    }
}