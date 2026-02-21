using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Left panel — Lab PSU controls vs Commercial renewable controls.
/// All field names match FluxeroSiteProfile exactly (cellCount, not numberOfCells).
/// </summary>
public class InputConfigPanel : MonoBehaviour
{
    [Header("Engine + Environment")]
    public FluxeroSimulationEngine      engine;
    public FluxeroEnvironmentSimulator  envSim;

    // ── Mode tabs ────────────────────────────────────────────────────
    [Header("Mode Tabs")]
    public Button labModeBtn;
    public Button commercialModeBtn;
    public Color  tabActiveColor   = new Color(0.10f, 0.80f, 0.50f);
    public Color  tabInactiveColor = new Color(0.20f, 0.22f, 0.28f);

    // ── Lab PSU panel ────────────────────────────────────────────────
    [Header("Lab PSU Panel")]
    public GameObject      labPSUPanel;
    public Slider          voltageSlider;
    public Slider          currentSlider;
    public TMP_InputField  voltageInput;
    public TMP_InputField  currentInput;
    public TextMeshProUGUI powerReadout;

    // ── Commercial panel ─────────────────────────────────────────────
    [Header("Commercial Panel")]
    public GameObject      commercialPanel;
    public TMP_Dropdown    environmentDropdown;
    public Slider          capacitySlider;
    public TextMeshProUGUI capacityLabel;
    public Slider          ambientTempSlider;
    public TextMeshProUGUI ambientTempLabel;

    // ── Stack config ──────────────────────────────────────────────────
    [Header("Stack Configuration")]
    public TMP_InputField  numCellsInput;
    public TMP_InputField  cellAreaInput;
    public TextMeshProUGUI stackSpecsReadout;

    // ── Controls ─────────────────────────────────────────────────────
    [Header("Controls")]
    public TextMeshProUGUI modeStatusLabel;
    public Button          startStopBtn;
    public TextMeshProUGUI startStopLabel;
    public Button          resetBtn;

    private bool _isLabMode = true;
    private bool _isRunning = false;

    void Start()
    {
        BuildEnvironmentDropdown();
        SetLabMode(true);

        voltageSlider.onValueChanged.AddListener(OnVoltageSlider);
        currentSlider.onValueChanged.AddListener(OnCurrentSlider);
        capacitySlider.onValueChanged.AddListener(OnCapacitySlider);
        ambientTempSlider.onValueChanged.AddListener(OnAmbientTempSlider);
        voltageInput.onEndEdit.AddListener(OnVoltageInput);
        currentInput.onEndEdit.AddListener(OnCurrentInput);
        numCellsInput.onEndEdit.AddListener(OnNumCellsInput);
        cellAreaInput.onEndEdit.AddListener(OnCellAreaInput);
        if (resetBtn) resetBtn.onClick.AddListener(OnResetClicked);

        // Seed UI from profile
        var p = engine?.profile;
        if (p != null)
        {
            voltageSlider.value = p.voltageSetpoint;
            currentSlider.value = p.currentSetpoint;
            voltageInput.text   = p.voltageSetpoint.ToString("F1");
            currentInput.text   = p.currentSetpoint.ToString("F1");
            numCellsInput.text  = p.cellCount.ToString();          // cellCount ✓
            cellAreaInput.text  = p.cellActiveAreaCm2.ToString("F0");
        }
    }

    // ── Mode switching ───────────────────────────────────────────────

    public void OnLabModeClicked()        => SetLabMode(true);
    public void OnCommercialModeClicked() => SetLabMode(false);

    private void SetLabMode(bool isLab)
    {
        _isLabMode = isLab;
        if (labPSUPanel)     labPSUPanel.SetActive(isLab);
        if (commercialPanel) commercialPanel.SetActive(!isLab);
        TintBtn(labModeBtn,        isLab ? tabActiveColor : tabInactiveColor);
        TintBtn(commercialModeBtn, isLab ? tabInactiveColor : tabActiveColor);
        if (modeStatusLabel)
            modeStatusLabel.text = isLab ? "MODE: Lab Bench / PSU" : "MODE: Commercial / Renewable";

        if (isLab) { if (envSim) envSim.preset = FluxeroEnvironmentSimulator.EnvironmentPreset.Lab_PSU_Flat; UpdateLabPower(); }
        else        { if (environmentDropdown) OnEnvironmentChanged(environmentDropdown.value); }
        RefreshStackReadout();
    }

    private void TintBtn(Button b, Color c) { if (b) b.GetComponent<Image>().color = c; }

    // ── Lab PSU ──────────────────────────────────────────────────────

    private void OnVoltageSlider(float v)
    {
        if (engine.profile) engine.profile.voltageSetpoint = v;
        if (voltageInput)   voltageInput.text = v.ToString("F1");
        UpdateLabPower();
    }

    private void OnCurrentSlider(float v)
    {
        if (engine.profile) engine.profile.currentSetpoint = v;
        if (currentInput)   currentInput.text = v.ToString("F1");
        UpdateLabPower();
    }

    private void OnVoltageInput(string s)
    {
        if (float.TryParse(s, out float v))
            voltageSlider.value = Mathf.Clamp(v, voltageSlider.minValue, voltageSlider.maxValue);
    }

    private void OnCurrentInput(string s)
    {
        if (float.TryParse(s, out float v))
            currentSlider.value = Mathf.Clamp(v, currentSlider.minValue, currentSlider.maxValue);
    }

    private void UpdateLabPower()
    {
        if (!_isLabMode || engine.profile == null) return;
        float w = engine.profile.voltageSetpoint * engine.profile.currentSetpoint;
        engine.profile.nominalPowerKW = w / 1000f;
        if (powerReadout) powerReadout.text = $"Power: {w:F2} W";
    }

    // ── Commercial ───────────────────────────────────────────────────

    private void BuildEnvironmentDropdown()
    {
        if (!environmentDropdown) return;
        environmentDropdown.ClearOptions();
        environmentDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "🌬  Wind — Calm",
            "🌬  Wind — Moderate",
            "🌬  Wind — Gusty",
            "⛈  Wind — Storm",
            "☀  Solar — Clear Sky",
            "⛅  Solar — Partly Cloudy",
            "☁  Solar — Overcast",
            "🌤  Solar — Dynamic",
            "🌬☀  Hybrid"
        });
        environmentDropdown.onValueChanged.AddListener(OnEnvironmentChanged);
    }

    private void OnEnvironmentChanged(int i)
    {
        if (envSim) envSim.SetPreset(i + 1);
    }

    private void OnCapacitySlider(float v)
    {
        if (envSim) envSim.SetInstalledCapacity(v);
        if (capacityLabel) capacityLabel.text = v >= 1000f ? $"{v/1000f:F1} MW" : $"{v:F0} kW";
    }

    private void OnAmbientTempSlider(float v)
    {
        if (envSim) envSim.SetAmbientTemp(v);
        if (ambientTempLabel) ambientTempLabel.text = $"{v:F0}°C";
    }

    // ── Stack config ─────────────────────────────────────────────────

    private void OnNumCellsInput(string s)
    {
        if (int.TryParse(s, out int n) && n > 0 && engine.profile != null)
        {
            engine.profile.cellCount = n;   // cellCount ✓
            RefreshStackReadout();
        }
    }

    private void OnCellAreaInput(string s)
    {
        if (float.TryParse(s, out float a) && a > 0f && engine.profile != null)
        {
            engine.profile.cellActiveAreaCm2 = a;
            RefreshStackReadout();
        }
    }

    private void RefreshStackReadout()
    {
        if (!stackSpecsReadout || engine.profile == null) return;
        var p = engine.profile;
        stackSpecsReadout.text =
            $"{p.cellCount} cell{(p.cellCount != 1 ? "s" : "")}  ×  {p.cellActiveAreaCm2:F0} cm²\n" +
            $"Total area: {p.cellCount * p.cellActiveAreaCm2:F0} cm²   " +
            $"Max I: {p.nominalCurrentDensityAcm2 * p.cellActiveAreaCm2:F1} A";
    }

    // ── Start / Stop / Reset ─────────────────────────────────────────

    public void OnStartStopClicked()
    {
        _isRunning = !_isRunning;
        if (_isRunning) { engine.StartSimulation(); if (startStopLabel) startStopLabel.text = "⏹  STOP";  TintBtn(startStopBtn, new Color(0.9f, 0.3f, 0.3f)); }
        else             { engine.StopSimulation();  if (startStopLabel) startStopLabel.text = "▶  START"; TintBtn(startStopBtn, new Color(0.1f, 0.8f, 0.5f)); }
    }

    public void OnResetClicked() => engine.ResetSession();
}