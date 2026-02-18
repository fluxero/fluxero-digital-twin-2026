using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FluxeroVisualizer : MonoBehaviour
{
    public FluxeroUniversalTwin twinBrain;
    public FluxeroFinances finances;

    [Header("Dashboard Display")]
    public TextMeshProUGUI timeframeLabel;
    public TextMeshProUGUI productionValueText;
    public TextMeshProUGUI financialReturnText;
    public Image fluxeroLogo;

    [Header("Branding")]
    public Material logoMaterial; // We will use your fluxero-google.png here

    void Update()
    {
        UpdateDashboard();
        PulseLogo();
    }

    void UpdateDashboard()
    {
        float output = twinBrain.GetPredictedOutput();
        string unit = (twinBrain.activeMode == FluxeroUniversalTwin.SimulationMode.LabBench) ? "mg" : "kg";
        
        // If lab mode, convert kg to mg for the display
        if (twinBrain.activeMode == FluxeroUniversalTwin.SimulationMode.LabBench) output *= 1000000f;

        timeframeLabel.text = $"TIME PREDICTION: {twinBrain.selectedTimeframe}";
        productionValueText.text = $"H2 YIELD: {output:F2} {unit}";
        
        float money = finances.CalculateFinancialReturn(twinBrain.GetPredictedOutput());
        financialReturnText.text = $"EST. REVENUE: £{money:N2}";
    }

    // Makes the logo "Glow" when production is active
    void PulseLogo()
    {
        if (logoMaterial != null)
        {
            float pulse = 1f + (Mathf.Sin(Time.time * 5f) * 0.2f);
            if (twinBrain.GetCurrentProductionRatePerHour() <= 0) pulse = 1f;
            logoMaterial.SetColor("_EmissionColor", Color.green * pulse);
        }
    }

    // Call this from a UI Dropdown to change timeframe
    public void SetTimeframe(int index) => twinBrain.selectedTimeframe = (FluxeroUniversalTwin.Timeframe)index;
    public void SetMode(int index) => twinBrain.activeMode = (FluxeroUniversalTwin.SimulationMode)index;
}