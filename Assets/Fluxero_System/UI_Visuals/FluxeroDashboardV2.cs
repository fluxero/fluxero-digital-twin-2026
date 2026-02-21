using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Main live dashboard. Subscribes to engine events only — no Update() polling.
/// All ElectrolyzerState field names match the actual class (healthIndex, not cellHealthIndex).
/// </summary>
public class FluxeroDashboardV2 : MonoBehaviour
{
    [Header("Engine References")]
    public FluxeroSimulationEngine      engine;
    public FluxeroYieldPredictor        predictor;
    public StackHealthMonitor           healthMonitor;
    public FluxeroEnvironmentSimulator  envSim;

    // ── Gauges ───────────────────────────────────────────────────────
    [Header("Power Gauge")]
    public TextMeshProUGUI powerValueText;
    public TextMeshProUGUI powerUnitText;

    [Header("H2 Rate Gauge")]
    public TextMeshProUGUI h2RateValueText;
    public TextMeshProUGUI h2RateUnitText;

    [Header("Temp & Efficiency")]
    public TextMeshProUGUI stackTempText;
    public TextMeshProUGUI faradaicEffText;
    public Slider          tempSlider;
    public Image           tempFill;

    [Header("Stack Health")]
    public Slider          healthSlider;
    public Image           healthFill;
    public TextMeshProUGUI healthPercentText;
    public TextMeshProUGUI maintenanceText;

    [Header("Session Totals")]
    public TextMeshProUGUI sessionH2Text;
    public TextMeshProUGUI sessionTimeText;
    public TextMeshProUGUI cumulativeRevenueText;

    // ── Power Graph ───────────────────────────────────────────────────
    [Header("Power Graph")]
    public RectTransform   graphContainer;
    public Image           graphLinePrefab;
    public TextMeshProUGUI graphMaxLabel;
    public TextMeshProUGUI graphMinLabel;
    public int             graphMaxPoints  = 120;
    public float           graphUpdateRate = 0.25f;

    // ── Forecast ──────────────────────────────────────────────────────
    [Header("Forecast Panel")]
    public Transform       forecastTableBody;
    public GameObject      forecastRowPrefab;
    public TextMeshProUGUI forecastH2TotalText;
    public TextMeshProUGUI forecastRevenueText;
    public TextMeshProUGUI forecastPaybackText;
    public Button          refreshForecastBtn;

    // ── Alerts ────────────────────────────────────────────────────────
    [Header("Alerts")]
    public Transform       alertsContainer;
    public GameObject      alertRowPrefab;
    public int             maxAlertRows = 6;
    public GameObject      noAlertsLabel;

    // ── Environment strip ─────────────────────────────────────────────
    [Header("Environment Strip")]
    public TextMeshProUGUI envPresetLabel;
    public TextMeshProUGUI windSpeedLabel;
    public TextMeshProUGUI solarLabel;
    public TextMeshProUGUI capacityFactorLabel;

    // ── Branding ──────────────────────────────────────────────────────
    [Header("Branding")]
    public Material logoGlowMaterial;
    public Image    statusDot;

    // ── Colours ───────────────────────────────────────────────────────
    private static readonly Color ColGood     = new Color(0.18f, 0.85f, 0.45f);
    private static readonly Color ColWarning  = new Color(1.00f, 0.76f, 0.03f);
    private static readonly Color ColCritical = new Color(0.92f, 0.25f, 0.25f);
    private static readonly Color ColIdle     = new Color(0.40f, 0.42f, 0.48f);

    // ── Private ───────────────────────────────────────────────────────
    private Queue<float>      _graphData     = new Queue<float>();
    private Queue<Image>      _graphSegments = new Queue<Image>();
    private Queue<GameObject> _alertRows     = new Queue<GameObject>();
    private float             _graphTimer    = 0f;
    private float             _sessionStart  = -1f;
    private float             _graphMax      = 0.01f;

    // ─────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        FluxeroSimulationEngine.OnStateUpdated     += OnStateUpdate;
        FluxeroSimulationEngine.OnAlertRaised      += OnAlert;
        FluxeroSimulationEngine.OnPowerInputChanged += OnPowerSample;
    }

    void OnDisable()
    {
        FluxeroSimulationEngine.OnStateUpdated     -= OnStateUpdate;
        FluxeroSimulationEngine.OnAlertRaised      -= OnAlert;
        FluxeroSimulationEngine.OnPowerInputChanged -= OnPowerSample;
    }

    void Start()
    {
        if (refreshForecastBtn) refreshForecastBtn.onClick.AddListener(RefreshForecast);
        if (noAlertsLabel)      noAlertsLabel.SetActive(true);
    }

    void Update()
    {
        // Session timer
        if (engine != null)
        {
            if (engine.isRunning && _sessionStart < 0f) _sessionStart = Time.time;
            if (!engine.isRunning) _sessionStart = -1f;
        }
        if (_sessionStart > 0f && sessionTimeText)
        {
            float e = Time.time - _sessionStart;
            sessionTimeText.text = $"{(int)(e/3600):D2}:{(int)(e/60)%60:D2}:{(int)(e%60):D2}";
        }

        // Status dot
        if (statusDot)
            statusDot.color = (engine != null && engine.isRunning)
                ? Color.Lerp(ColGood, Color.white, Mathf.PingPong(Time.time * 2f, 1f))
                : ColIdle;

        // Logo glow
        if (logoGlowMaterial)
        {
            float p = (engine != null && engine.isRunning) ? 1f + Mathf.Sin(Time.time * 4f) * 0.3f : 0.15f;
            logoGlowMaterial.SetColor("_EmissionColor", ColGood * p);
        }

        UpdateEnvStrip();
    }

    // ── Engine event handlers ─────────────────────────────────────────

    private void OnStateUpdate(ElectrolyzerState s)
    {
        UpdatePowerGauge(s);
        UpdateH2Gauge(s);
        UpdateTempGauge(s);
        UpdateHealthBar(s);   // uses s.healthIndex — correct field name
        UpdateSessionTotals(s);
    }

    private void OnPowerSample(float powerKW)
    {
        _graphTimer += Time.deltaTime;
        if (_graphTimer < graphUpdateRate) return;
        _graphTimer = 0f;

        _graphData.Enqueue(powerKW);
        _graphMax = Mathf.Max(_graphMax, powerKW * 1.15f);
        if (_graphData.Count > graphMaxPoints) _graphData.Dequeue();
        RedrawGraph();

        if (graphMaxLabel) graphMaxLabel.text = _graphMax >= 1f ? $"{_graphMax:F2} kW" : $"{_graphMax*1000f:F1} W";
        if (graphMinLabel) graphMinLabel.text = "0";
    }

    // AlertSeverity is now a top-level enum in FluxeroEnums.cs — no prefix needed
    private void OnAlert(string message, AlertSeverity severity)
    {
        if (!alertsContainer || !alertRowPrefab) return;
        if (noAlertsLabel) noAlertsLabel.SetActive(false);

        var row = Instantiate(alertRowPrefab, alertsContainer);
        var lbl = row.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl)
        {
            string icon = severity == AlertSeverity.Critical ? "🔴"
                        : severity == AlertSeverity.Warning  ? "🟡" : "🟢";
            lbl.text  = $"{icon} {System.DateTime.Now:HH:mm:ss}  {message}";
            lbl.color = severity == AlertSeverity.Critical ? ColCritical
                      : severity == AlertSeverity.Warning  ? ColWarning : ColGood;
        }

        _alertRows.Enqueue(row);
        if (_alertRows.Count > maxAlertRows) Destroy(_alertRows.Dequeue());
    }

    // ── Gauge updaters ────────────────────────────────────────────────

    private void UpdatePowerGauge(ElectrolyzerState s)
    {
        if (!powerValueText) return;
        bool   watts = s.inputPowerKW < 1f;
        float  v     = watts ? s.inputPowerKW * 1000f : s.inputPowerKW;
        powerValueText.text = v.ToString(v < 10f ? "F2" : "F1");
        if (powerUnitText) powerUnitText.text = watts ? "W" : "kW";
    }

    private void UpdateH2Gauge(ElectrolyzerState s)
    {
        if (!h2RateValueText) return;
        float  r = s.instantaneousH2RateKgHr;
        string val, unit;
        if      (r < 0.001f) { val = (r * 1e6f).ToString("F2");  unit = "mg/hr"; }
        else if (r < 1f)     { val = (r * 1000f).ToString("F3"); unit = "g/hr";  }
        else                 { val = r.ToString("F3");             unit = "kg/hr"; }
        h2RateValueText.text  = val;
        h2RateValueText.color = r > 0f ? ColGood : ColIdle;
        if (h2RateUnitText) h2RateUnitText.text = unit;
    }

    private void UpdateTempGauge(ElectrolyzerState s)
    {
        float maxT  = engine?.profile != null ? engine.profile.maxSafeTempC : 80f;
        float ratio = s.stackTempC / maxT;
        if (stackTempText)  stackTempText.text    = $"{s.stackTempC:F1}°C";
        if (tempSlider)     tempSlider.value       = ratio;
        if (tempFill)       tempFill.color         = ratio < 0.7f ? ColGood : ratio < 0.9f ? ColWarning : ColCritical;
        if (faradaicEffText) faradaicEffText.text  = $"{s.faradaicEfficiency:P1}";
    }

    private void UpdateHealthBar(ElectrolyzerState s)
    {
        float h = s.healthIndex;   // healthIndex — correct field name ✓
        if (healthSlider)      healthSlider.value    = h;
        if (healthFill)        healthFill.color       = h > 0.75f ? ColGood : h > 0.5f ? ColWarning : ColCritical;
        if (healthPercentText) healthPercentText.text = $"{h:P0}";
        if (maintenanceText && healthMonitor)
            maintenanceText.text = healthMonitor.GetMaintenanceRecommendation(h);
    }

    private void UpdateSessionTotals(ElectrolyzerState s)
    {
        if (sessionH2Text)
        {
            float kg = s.cumulativeH2Kg;
            if      (kg < 0.001f) sessionH2Text.text = $"{kg * 1e6f:F2} mg";
            else if (kg < 1f)     sessionH2Text.text = $"{kg * 1000f:F3} g";
            else                  sessionH2Text.text = $"{kg:F4} kg";
        }
        if (cumulativeRevenueText && engine?.profile != null)
            cumulativeRevenueText.text = $"£{s.cumulativeH2Kg * engine.profile.h2SalePricePerKg:F4}";
    }

    // ── Graph ─────────────────────────────────────────────────────────

    private void RedrawGraph()
    {
        if (!graphContainer || !graphLinePrefab) return;
        foreach (var seg in _graphSegments) if (seg) Destroy(seg.gameObject);
        _graphSegments.Clear();

        float[] pts = _graphData.ToArray();
        if (pts.Length < 2) return;

        float w = graphContainer.rect.width, h = graphContainer.rect.height;
        float stepX = w / graphMaxPoints;

        for (int i = 1; i < pts.Length; i++)
        {
            float x0 = (i-1)*stepX, y0 = (pts[i-1]/_graphMax)*h;
            float x1 =  i   *stepX, y1 = (pts[i]  /_graphMax)*h;
            float dx = x1-x0, dy = y1-y0;

            var seg = Instantiate(graphLinePrefab, graphContainer);
            var rt  = seg.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x0, y0);
            rt.sizeDelta = new Vector2(Mathf.Sqrt(dx*dx+dy*dy), 2f);
            rt.localRotation = Quaternion.Euler(0,0, Mathf.Atan2(dy,dx)*Mathf.Rad2Deg);
            seg.color = Color.Lerp(ColGood, ColCritical, pts[i]/_graphMax);
            _graphSegments.Enqueue(seg);
        }
    }

    // ── Forecast ──────────────────────────────────────────────────────

    public void RefreshForecast()
    {
        if (!predictor) return;
        predictor.RecalculatePredictions();
        foreach (Transform c in forecastTableBody) Destroy(c.gameObject);

        float totalKg = 0, totalRev = 0;
        foreach (var f in predictor.Forecasts)
        {
            totalKg += f.projectedH2Kg; totalRev += f.projectedRevenueGBP;
            var row   = Instantiate(forecastRowPrefab, forecastTableBody);
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 4)
            {
                texts[0].text  = f.displayLabel;
                texts[1].text  = FormatH2(f.projectedH2Kg);
                texts[2].text  = $"£{f.projectedRevenueGBP:N0}";
                texts[3].text  = $"{f.projectedCapexPaybackPct:F1}%";
                texts[3].color = f.projectedCapexPaybackPct >= 100f ? ColGood : ColWarning;
            }
        }
        if (forecastH2TotalText) forecastH2TotalText.text = FormatH2(totalKg);
        if (forecastRevenueText) forecastRevenueText.text = $"£{totalRev:N0}";
        if (forecastPaybackText && engine?.profile != null)
            forecastPaybackText.text = totalRev >= engine.profile.capexGBP
                ? "✓ CAPEX Recovered" : $"{totalRev/engine.profile.capexGBP:P0} of CAPEX";
    }

    private string FormatH2(float kg)
    {
        if      (kg < 0.001f) return $"{kg*1e6f:F2} mg";
        else if (kg < 1f)     return $"{kg*1000f:F3} g";
        else if (kg < 1000f)  return $"{kg:F3} kg";
        else                  return $"{kg/1000f:F2} t";
    }

    public void ClearAlerts()
    {
        foreach (var r in _alertRows) if (r) Destroy(r);
        _alertRows.Clear();
        if (noAlertsLabel) noAlertsLabel.SetActive(true);
    }

    // ── Env strip ─────────────────────────────────────────────────────

    private void UpdateEnvStrip()
    {
        if (!envSim) return;
        if (envPresetLabel)  envPresetLabel.text  = envSim.preset.ToString().Replace("_", " ");
        if (windSpeedLabel)  windSpeedLabel.text  = envSim.windSpeedMs > 0 ? $"💨 {envSim.windSpeedMs:F1} m/s" : "💨 —";
        if (solarLabel)      solarLabel.text      = envSim.solarIrradianceWm2 > 0 ? $"☀ {envSim.solarIrradianceWm2:F0} W/m²" : "☀ —";
        if (capacityFactorLabel && engine?.profile != null && envSim.installedCapacityKW > 0f)
        {
            float cf = engine.CurrentState.inputPowerKW / envSim.installedCapacityKW;
            capacityFactorLabel.text  = $"CF: {cf:P0}";
            capacityFactorLabel.color = cf > 0.35f ? ColGood : cf > 0.15f ? ColWarning : ColIdle;
        }
    }
}