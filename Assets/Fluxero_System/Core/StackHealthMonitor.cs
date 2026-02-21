using UnityEngine;
using System.Collections.Generic;

public class StackHealthMonitor : MonoBehaviour
{
    [Header("References")]
    public FluxeroSimulationEngine engine;

    [Range(0f, 1f)] public float criticalTempRatio = 0.90f;
    [Range(0f, 1f)] public float warningTempRatio  = 0.80f;
    [Range(0f, 1f)] public float minHealthForAlert = 0.75f;

    [System.Serializable]
    public struct StressEvent
    {
        public float  elapsedSeconds;
        public float  tempC;
        public float  powerKW;
        public string description;
    }

    public List<StressEvent> StressHistory     { get; private set; } = new List<StressEvent>();
    public float             EstimatedLifeYears { get; private set; } = 7f;
    public int               TotalStressEvents  { get; private set; } = 0;

    private bool _healthAlertFired = false;

    void OnEnable()  => FluxeroSimulationEngine.OnStateUpdated += HandleStateUpdate;
    void OnDisable() => FluxeroSimulationEngine.OnStateUpdated -= HandleStateUpdate;

    private void HandleStateUpdate(ElectrolyzerState state)
    {
        if (engine == null || engine.profile == null) return;

        float maxTemp = engine.profile.maxSafeTempC;

        // healthIndex — matches the field name in ElectrolyzerState
        EstimatedLifeYears = 7f * state.healthIndex;

        // Record thermal stress events
        if (state.stackTempC > maxTemp * warningTempRatio)
        {
            bool isCritical = state.stackTempC > maxTemp * criticalTempRatio;
            StressHistory.Add(new StressEvent
            {
                elapsedSeconds = state.sessionElapsedSeconds,
                tempC          = state.stackTempC,
                powerKW        = state.inputPowerKW,
                description    = isCritical ? "CRITICAL thermal event" : "Thermal warning"
            });
            TotalStressEvents++;
            if (StressHistory.Count > 200) StressHistory.RemoveAt(0);
        }

        // Log to console only — engine handles its own alert events
        if (!_healthAlertFired && state.healthIndex < minHealthForAlert)
        {
            _healthAlertFired = true;
            Debug.LogWarning($"[StackHealth] Cell health at {state.healthIndex:P0} — maintenance recommended");
        }
    }

    public string GetMaintenanceRecommendation(float health)
    {
        if (health > 0.90f) return "✓  Stack nominal — no action required";
        if (health > 0.75f) return "⚠  Inspection within 3 months";
        if (health > 0.50f) return "⚠  Replace membrane within 1 month";
        return                      "🔴  CRITICAL — halt and replace membrane";
    }

    public void ResetHistory()
    {
        StressHistory.Clear();
        TotalStressEvents  = 0;
        _healthAlertFired  = false;
    }
}