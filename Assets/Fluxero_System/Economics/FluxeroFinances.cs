using UnityEngine;

public class FluxeroFinances : MonoBehaviour
{
    public FluxeroUniversalTwin twinBrain;

    [Header("Financial Settings")]
    public float capexInvestment = 50000f;
    public float h2PricePerKg = 8.50f;
    public float opexMaintenanceYearly = 1500f;

    public float CalculateFinancialReturn(float h2Amount)
    {
        float grossRevenue = h2Amount * h2PricePerKg;
        
        // Add OPEX deduction if timeframe is long
        if (twinBrain.selectedTimeframe == FluxeroUniversalTwin.Timeframe.OneYear)
            grossRevenue -= opexMaintenanceYearly;
        if (twinBrain.selectedTimeframe == FluxeroUniversalTwin.Timeframe.FiveYears)
            grossRevenue -= opexMaintenanceYearly * 5;

        return grossRevenue;
    }

    public float GetROI(float h2Amount)
    {
        float earnings = CalculateFinancialReturn(h2Amount);
        return (earnings / capexInvestment) * 100f;
    }
}