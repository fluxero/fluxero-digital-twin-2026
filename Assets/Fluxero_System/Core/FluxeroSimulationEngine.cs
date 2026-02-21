using UnityEngine;
using System;

public class FluxeroSimulationEngine : MonoBehaviour
{
    // ── Config & State ───────────────────────────────────────────────
    public FluxeroSiteProfile profile;
    public ElectrolyzerState  state = new ElectrolyzerState();

    // ── Runtime Control ──────────────────────────────────────────────
    public bool isRunning = false;

    // ── Events (all other scripts subscribe to these) ────────────────
    public static event Action<ElectrolyzerState>        OnStateUpdated;      // renamed from OnTelemetryUpdate for dashboard compat
    public static event Action<ElectrolyzerState>        OnTelemetryUpdate;   // kept for any existing subscribers
    public static event Action<string, AlertSeverity>    OnAlertRaised;
    public static event Action<float>                    OnPowerInputChanged; // feeds the graph

    // ── Convenience property so dashboard can do engine.CurrentState ─
    public ElectrolyzerState CurrentState => state;

    // ── Convenience property so InputConfigPanel can reach profile ───
    public FluxeroSiteProfile config => profile;

    // ── Private sim vars ─────────────────────────────────────────────
    private float _simTimer     = 0f;
    private const float _simStep = 0.1f;   // fixed 100ms physics tick
    private float _uiTimer      = 0f;
    private const float _uiStep  = 0.5f;   // fire UI events at 2 Hz

    private float _stackTempC   = 25f;
    private float _health       = 1.0f;
    private float _noisePhase   = 0f;

    // Alert throttle — don't spam the same alert every frame
    private float _lastThermalAlert  = -999f;
    private float _lastHealthAlert   = -999f;
    private const float AlertCooldown = 5f;

    // ────────────────────────────────────────────────────────────────

    void Start()
    {
        if (profile != null) _stackTempC = profile.ambientTempC > 0 ? profile.ambientTempC : 25f;
        state.healthIndex = 1f;
    }

    void Update()
    {
        if (!isRunning || profile == null) return;

        _simTimer += Time.deltaTime;
        _uiTimer  += Time.deltaTime;

        // Fixed-timestep physics loop
        while (_simTimer >= _simStep)
        {
            RunPhysicsStep(_simStep);
            _simTimer -= _simStep;
        }

        // Fire UI events at reduced rate to avoid jitter
        if (_uiTimer >= _uiStep)
        {
            OnStateUpdated?.Invoke(state);
            OnTelemetryUpdate?.Invoke(state);   // legacy
            _uiTimer = 0f;
        }

        // Power graph gets every frame sample (smoothed by graph's own timer)
        OnPowerInputChanged?.Invoke(state.inputPowerKW);
    }

    // ── Physics ─────────────────────────────────────────────────────

    void RunPhysicsStep(float dt)
    {
        // 1. Erratic power from renewable source
        _noisePhase += dt * 0.5f;
        float noise = Mathf.PerlinNoise(_noisePhase, 0f) * 2f - 1f;
        float rawPower = profile.nominalPowerKW * (1f + noise * profile.volatilityFactor);
        state.inputPowerKW = Mathf.Max(0f, rawPower);

        // 2. Electrochemistry — Faraday's Law
        state.stackVoltageV = profile.nominalCellVoltage * profile.cellCount;
        if (state.stackVoltageV > 0f)
            state.stackCurrentA = (state.inputPowerKW * 1000f) / state.stackVoltageV;

        // Faradaic efficiency — peaks near nominal current density
        float nomCurrent = profile.nominalCurrentDensityAcm2 * profile.cellActiveAreaCm2;
        float normalized = nomCurrent > 0f ? state.stackCurrentA / nomCurrent : 0f;
        state.faradaicEfficiency = 0.95f * Mathf.Exp(-0.5f * Mathf.Pow((normalized - 1f) / 0.6f, 2f));

        float molPerSec = (state.stackCurrentA * state.faradaicEfficiency * profile.cellCount) / (2f * 96485f);
        state.instantaneousH2RateKgHr = molPerSec * 0.002016f * 3600f * _health;
        state.cumulativeH2Kg += (state.instantaneousH2RateKgHr / 3600f) * dt;

        // 3. Thermal model
        float thermoV = profile.thermoNeutralVoltage * profile.cellCount;
        float heatW   = Mathf.Max(0f, (state.stackVoltageV - thermoV) * state.stackCurrentA);
        float dissW   = (_stackTempC - 25f) / Mathf.Max(0.01f, profile.thermalResistanceKperW);
        _stackTempC  += ((heatW - dissW) / Mathf.Max(1f, profile.thermalMassJperK)) * dt;
        state.stackTempC = Mathf.Clamp(_stackTempC, 20f, profile.maxSafeTempC + 20f);

        // 4. Degradation
        float yearSec  = 365f * 24f * 3600f;
        float baseRate = profile.baseYearlyDegradationRate / yearSec;
        float stress   = Mathf.Max(0f, (_stackTempC - profile.maxSafeTempC * 0.85f) / (profile.maxSafeTempC * 0.15f));
        _health -= baseRate * (1f + stress * 2f) * dt;
        _health  = Mathf.Max(0f, _health);

        state.healthIndex             = _health;
        state.stackTempC              = _stackTempC;
        state.sessionElapsedSeconds  += dt;
        state.timestamp               = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // 5. Alerts (throttled)
        float now = state.sessionElapsedSeconds;
        float tempRatio = _stackTempC / Mathf.Max(1f, profile.maxSafeTempC);
        if (tempRatio > 0.9f && now - _lastThermalAlert > AlertCooldown)
        {
            _lastThermalAlert = now;
            var sev = tempRatio >= 1f ? AlertSeverity.Critical : AlertSeverity.Warning;
            OnAlertRaised?.Invoke($"Stack temp {_stackTempC:F1}°C — {(sev == AlertSeverity.Critical ? "CRITICAL" : "warning")}", sev);
        }
        if (_health < 0.75f && now - _lastHealthAlert > AlertCooldown * 6f)
        {
            _lastHealthAlert = now;
            OnAlertRaised?.Invoke($"Cell health at {_health:P0} — schedule maintenance", AlertSeverity.Warning);
        }
    }

    // ── Public Control Methods ───────────────────────────────────────

    public void StartSimulation()
    {
        isRunning = true;
        OnAlertRaised?.Invoke("Simulation started", AlertSeverity.Info);
    }

    public void StopSimulation()
    {
        isRunning = false;
        OnAlertRaised?.Invoke("Simulation paused", AlertSeverity.Info);
    }

    public void ResetSession()
    {
        state.cumulativeH2Kg         = 0f;
        state.sessionElapsedSeconds  = 0f;
        _stackTempC                  = profile != null ? profile.ambientTempC : 25f;
        _health                      = 1.0f;
        _noisePhase                  = 0f;
        _lastThermalAlert            = -999f;
        _lastHealthAlert             = -999f;
        OnAlertRaised?.Invoke("Session reset", AlertSeverity.Info);
    }
}