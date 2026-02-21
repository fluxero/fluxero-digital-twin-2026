// FluxeroDashboardBuilder.cs
// Place in Assets/Editor/ (create the folder if it doesn't exist)
// Use via: Unity menu → Fluxero → Build Dashboard UI
// This auto-generates the entire Canvas hierarchy with correct layout, colours, and component wiring.

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class FluxeroDashboardBuilder : EditorWindow
{
    [MenuItem("Fluxero/Build Dashboard UI")]
    public static void BuildDashboard()
    {
        // ── Colour palette ──────────────────────────────────────────
        Color bg0        = Hex("#0A0C10");   // deepest background
        Color bg1        = Hex("#0F1218");   // panel background
        Color bg2        = Hex("#161B24");   // card background
        Color bg3        = Hex("#1E2530");   // raised element
        Color accent     = Hex("#00E5A0");   // Fluxero green
        Color accentDim  = Hex("#00A86B");   // darker green
        Color warn       = Hex("#FFB800");   // amber warning
        Color danger     = Hex("#FF3D3D");   // red alert
        Color textPri    = Hex("#F0F4FF");   // primary text
        Color textSec    = Hex("#8B95A8");   // secondary text
        Color border     = Hex("#252D3A");   // subtle border

        // ── Find or create Canvas ───────────────────────────────────
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null && existingCanvas.name == "FluxeroCanvas")
        {
            if (!EditorUtility.DisplayDialog("Rebuild Dashboard",
                "FluxeroCanvas already exists. Rebuild from scratch?", "Rebuild", "Cancel"))
                return;
            DestroyImmediate(existingCanvas.gameObject);
        }

        // ── Root Canvas ─────────────────────────────────────────────
        GameObject canvasGO = new GameObject("FluxeroCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        SetBg(canvasGO, bg0);

        // ── Root horizontal layout ───────────────────────────────────
        GameObject root = MakePanel("Root", canvasGO, bg0);
        StretchFull(root);
        var rootHL = root.AddComponent<HorizontalLayoutGroup>();
        rootHL.spacing = 2;
        rootHL.padding = new RectOffset(0, 0, 0, 0);
        rootHL.childControlWidth = true;
        rootHL.childControlHeight = true;
        rootHL.childForceExpandWidth = false;
        rootHL.childForceExpandHeight = true;

        // ══════════════════════════════════════════════════════
        // LEFT PANEL — 340px — Input Configuration
        // ══════════════════════════════════════════════════════
        GameObject leftPanel = MakePanel("LeftPanel", root, bg1);
        SetLayoutElement(leftPanel, 340, -1, false, true);

        var leftVL = leftPanel.AddComponent<VerticalLayoutGroup>();
        leftVL.padding = new RectOffset(12, 12, 12, 12);
        leftVL.spacing = 8;
        leftVL.childControlWidth = true;
        leftVL.childControlHeight = false;
        leftVL.childForceExpandWidth = true;
        leftVL.childForceExpandHeight = false;

        // Logo + title header
        GameObject logoRow = MakePanel("LogoRow", leftPanel, Color.clear);
        SetLayoutElement(logoRow, -1, 64, true, false);
        var logoHL = logoRow.AddComponent<HorizontalLayoutGroup>();
        logoHL.childAlignment = TextAnchor.MiddleLeft;
        logoHL.spacing = 10;
        logoHL.childControlHeight = true;
        logoHL.childControlWidth = false;
        logoHL.childForceExpandHeight = true;

        // Status dot
        GameObject dot = MakePanel("StatusDot", logoRow, accent);
        SetLayoutElement(dot, 10, 10, false, false);
        dot.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 10);
        // Make it round via aspect ratio fitter
        dot.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;

        MakeTMP("SiteTitle", logoRow, "FLUXERO DIGITAL TWIN", 18, textPri, FontStyles.Bold);

        AddSpacer(leftPanel, 4);

        // Mode tabs
        GameObject modeTabs = MakePanel("ModeTabs", leftPanel, bg3);
        modeTabs.GetComponent<Image>().pixelsPerUnitMultiplier = 1;
        SetLayoutElement(modeTabs, -1, 44, true, false);
        var tabHL = modeTabs.AddComponent<HorizontalLayoutGroup>();
        tabHL.childControlWidth = true;
        tabHL.childControlHeight = true;
        tabHL.childForceExpandWidth = true;
        tabHL.childForceExpandHeight = true;
        tabHL.padding = new RectOffset(4, 4, 4, 4);
        tabHL.spacing = 4;

        MakeButton("LabModeBtn",        modeTabs, "🔬  LAB MODE",        accent,    bg0);
        MakeButton("CommercialModeBtn", modeTabs, "🏭  COMMERCIAL",       bg3,       textSec);

        AddSpacer(leftPanel, 4);

        // Section: Power Source
        MakeSectionLabel("LabPSUPanel", leftPanel, "POWER SOURCE", textSec);

        // Voltage row
        MakeSliderRow(leftPanel, "Voltage", "VoltageSlider", "VoltageInput", "V", 0, 30, 12, bg2, accent, textPri, textSec);
        MakeSliderRow(leftPanel, "Current", "CurrentSlider", "CurrentInput", "A", 0, 10, 0.83f, bg2, accent, textPri, textSec);

        // Power readout card
        GameObject powerCard = MakePanel("PowerReadoutCard", leftPanel, bg2);
        SetLayoutElement(powerCard, -1, 48, true, false);
        AddRoundedBorder(powerCard, accentDim);
        var powerHL = powerCard.AddComponent<HorizontalLayoutGroup>();
        powerHL.padding = new RectOffset(16, 16, 0, 0);
        powerHL.childAlignment = TextAnchor.MiddleCenter;
        powerHL.childControlHeight = true;
        powerHL.childForceExpandHeight = true;
        MakeTMP("PowerLabel", powerCard, "INPUT POWER", 10, textSec, FontStyles.Normal);
        MakeTMP("PowerReadout", powerCard, "—  W", 20, accent, FontStyles.Bold);

        AddSpacer(leftPanel, 8);

        // Section: Stack Config
        MakeSectionLabel("StackSection", leftPanel, "STACK CONFIGURATION", textSec);

        GameObject stackCard = MakePanel("StackConfigCard", leftPanel, bg2);
        SetLayoutElement(stackCard, -1, 110, true, false);
        var stackVL = stackCard.AddComponent<VerticalLayoutGroup>();
        stackVL.padding = new RectOffset(12, 12, 10, 10);
        stackVL.spacing = 8;
        stackVL.childControlWidth = true;
        stackVL.childControlHeight = false;
        stackVL.childForceExpandWidth = true;

        MakeInputRow(stackCard, "Cells", "NumCellsInput", "1", textPri, textSec, bg3);
        MakeInputRow(stackCard, "Cell Area (cm²)", "CellAreaInput", "25", textPri, textSec, bg3);

        GameObject stackReadout = MakePanel("StackSpecsReadout_Container", stackCard, Color.clear);
        SetLayoutElement(stackReadout, -1, 28, true, false);
        var sr = MakeTMP("StackSpecsReadout", stackReadout, "1 cell  ×  25 cm²", 11, textSec, FontStyles.Normal);
        StretchFull(sr.gameObject);

        AddSpacer(leftPanel, 8);

        // Section: Controls
        MakeSectionLabel("ControlSection", leftPanel, "SIMULATION CONTROL", textSec);

        GameObject modeStatus = MakePanel("ModeStatus", leftPanel, Color.clear);
        SetLayoutElement(modeStatus, -1, 20, true, false);
        MakeTMP("ModeStatusLabel", modeStatus, "MODE: Lab Bench / PSU", 10, accentDim, FontStyles.Normal);

        GameObject ctrlRow = MakePanel("ControlRow", leftPanel, Color.clear);
        SetLayoutElement(ctrlRow, -1, 52, true, false);
        var ctrlHL = ctrlRow.AddComponent<HorizontalLayoutGroup>();
        ctrlHL.spacing = 8;
        ctrlHL.childControlWidth = true;
        ctrlHL.childControlHeight = true;
        ctrlHL.childForceExpandWidth = true;
        ctrlHL.childForceExpandHeight = true;

        MakeButton("StartStopBtn", ctrlRow, "▶  START",  accent, bg0);
        MakeButton("ResetBtn",     ctrlRow, "↺  RESET",  bg3,    textSec);

        // ══════════════════════════════════════════════════════
        // CENTRE PANEL — flexible — Live Gauges + Graph
        // ══════════════════════════════════════════════════════
        GameObject centrePanel = MakePanel("CentrePanel", root, bg0);
        SetLayoutElement(centrePanel, -1, -1, false, true);

        var centreVL = centrePanel.AddComponent<VerticalLayoutGroup>();
        centreVL.padding = new RectOffset(12, 12, 12, 12);
        centreVL.spacing = 8;
        centreVL.childControlWidth = true;
        centreVL.childControlHeight = false;
        centreVL.childForceExpandWidth = true;
        centreVL.childForceExpandHeight = false;

        // ── Top gauges row (4 cards) ─────────────────────────
        GameObject gaugesRow = MakePanel("GaugesRow", centrePanel, Color.clear);
        SetLayoutElement(gaugesRow, -1, 120, true, false);
        var gaugesHL = gaugesRow.AddComponent<HorizontalLayoutGroup>();
        gaugesHL.spacing = 8;
        gaugesHL.childControlWidth = true;
        gaugesHL.childControlHeight = true;
        gaugesHL.childForceExpandWidth = true;
        gaugesHL.childForceExpandHeight = true;

        MakeGaugeCard(gaugesRow, "PowerGauge",    "INPUT POWER",    "PowerValueText",    "PowerUnitText",    "—",    "W",     accent,  bg2, textPri, textSec);
        MakeGaugeCard(gaugesRow, "H2Gauge",       "H₂ RATE",        "H2RateValueText",   "H2RateUnitText",   "—",    "mg/hr", accent,  bg2, textPri, textSec);
        MakeGaugeCard(gaugesRow, "TempGauge",     "STACK TEMP",     "StackTempText",     null,               "25.0", "°C",    warn,    bg2, textPri, textSec);
        MakeGaugeCard(gaugesRow, "FaradaicGauge", "FARADAIC EFF.",  "FaradaicEffText",   null,               "—",    "%",     accentDim, bg2, textPri, textSec);

        // ── Health bar card ──────────────────────────────────
        GameObject healthCard = MakePanel("HealthCard", centrePanel, bg2);
        SetLayoutElement(healthCard, -1, 72, true, false);
        var healthVL = healthCard.AddComponent<VerticalLayoutGroup>();
        healthVL.padding = new RectOffset(16, 16, 10, 10);
        healthVL.spacing = 6;
        healthVL.childControlWidth = true;
        healthVL.childForceExpandWidth = true;

        // Health label row
        GameObject healthLabelRow = MakePanel("HealthLabelRow", healthCard, Color.clear);
        SetLayoutElement(healthLabelRow, -1, 20, true, false);
        var hlr = healthLabelRow.AddComponent<HorizontalLayoutGroup>();
        hlr.childControlWidth = true; hlr.childControlHeight = true;
        hlr.childForceExpandWidth = true; hlr.childForceExpandHeight = true;
        MakeTMP("HealthLabel_L", healthLabelRow, "CELL HEALTH INDEX", 10, textSec, FontStyles.Normal);
        MakeTMP("HealthPercentText", healthLabelRow, "100%", 13, accent, FontStyles.Bold);

        // Slider track
        GameObject healthSliderGO = MakeSliderElement("HealthSlider", healthCard, accent, bg3);
        SetLayoutElement(healthSliderGO, -1, 16, true, false);

        // Maintenance text
        MakeTMP("MaintenanceText", healthCard, "✓  Stack nominal — no action required", 10, accentDim, FontStyles.Normal);

        // ── Power Graph ──────────────────────────────────────
        GameObject graphCard = MakePanel("GraphCard", centrePanel, bg2);
        SetLayoutElement(graphCard, -1, 160, true, false);
        var graphVL = graphCard.AddComponent<VerticalLayoutGroup>();
        graphVL.padding = new RectOffset(16, 16, 12, 8);
        graphVL.spacing = 4;
        graphVL.childControlWidth = true; graphVL.childForceExpandWidth = true;

        // Graph header
        GameObject graphHeader = MakePanel("GraphHeader", graphCard, Color.clear);
        SetLayoutElement(graphHeader, -1, 20, true, false);
        var ghHL = graphHeader.AddComponent<HorizontalLayoutGroup>();
        ghHL.childControlWidth = true; ghHL.childControlHeight = true;
        ghHL.childForceExpandWidth = true; ghHL.childForceExpandHeight = true;
        MakeTMP("GraphTitle", graphHeader, "POWER INPUT  —  LIVE", 10, textSec, FontStyles.Normal);
        MakeTMP("GraphMaxLabel", graphHeader, "— W", 10, accent, FontStyles.Bold);

        // Graph drawing area
        GameObject graphContainer = MakePanel("GraphContainer", graphCard, Hex("#0D1117"));
        SetLayoutElement(graphContainer, -1, -1, true, false);
        graphContainer.AddComponent<LayoutElement>().flexibleHeight = 1;

        MakeTMP("GraphMinLabel", graphCard, "0", 9, textSec, FontStyles.Normal);

        // ── Session totals row ───────────────────────────────
        GameObject totalsRow = MakePanel("SessionTotalsRow", centrePanel, Color.clear);
        SetLayoutElement(totalsRow, -1, 64, true, false);
        var totalsHL = totalsRow.AddComponent<HorizontalLayoutGroup>();
        totalsHL.spacing = 8;
        totalsHL.childControlWidth = true; totalsHL.childControlHeight = true;
        totalsHL.childForceExpandWidth = true; totalsHL.childForceExpandHeight = true;

        MakeTotalCard(totalsRow, "TotalH2Card",   "SESSION H₂",   "SessionH2Text",          "—",    accent,   bg2, textPri, textSec);
        MakeTotalCard(totalsRow, "TotalTimeCard", "ELAPSED TIME", "SessionTimeText",         "00:00:00", textPri, bg2, textPri, textSec);
        MakeTotalCard(totalsRow, "TotalRevCard",  "EST. REVENUE", "CumulativeRevenueText",   "£0.0000", accentDim, bg2, textPri, textSec);

        // ══════════════════════════════════════════════════════
        // RIGHT PANEL — 320px — Forecast + Alerts
        // ══════════════════════════════════════════════════════
        GameObject rightPanel = MakePanel("RightPanel", root, bg1);
        SetLayoutElement(rightPanel, 320, -1, false, true);

        var rightVL = rightPanel.AddComponent<VerticalLayoutGroup>();
        rightVL.padding = new RectOffset(12, 12, 12, 12);
        rightVL.spacing = 8;
        rightVL.childControlWidth = true;
        rightVL.childControlHeight = false;
        rightVL.childForceExpandWidth = true;
        rightVL.childForceExpandHeight = false;

        // ── Environment strip ────────────────────────────────
        GameObject envStrip = MakePanel("EnvStrip", rightPanel, bg3);
        SetLayoutElement(envStrip, -1, 56, true, false);
        var envVL = envStrip.AddComponent<VerticalLayoutGroup>();
        envVL.padding = new RectOffset(12, 12, 6, 6);
        envVL.spacing = 2;
        envVL.childControlWidth = true; envVL.childForceExpandWidth = true;
        MakeTMP("EnvPresetLabel",      envStrip, "Lab Bench / PSU",  11, accent,   FontStyles.Bold);
        GameObject envMetrics = MakePanel("EnvMetrics", envStrip, Color.clear);
        SetLayoutElement(envMetrics, -1, 20, true, false);
        var emHL = envMetrics.AddComponent<HorizontalLayoutGroup>();
        emHL.childControlWidth = true; emHL.childControlHeight = true;
        emHL.childForceExpandWidth = true; emHL.childForceExpandHeight = true;
        MakeTMP("WindSpeedLabel",      envMetrics, "💨 —",       10, textSec, FontStyles.Normal);
        MakeTMP("SolarLabel",          envMetrics, "☀ —",        10, textSec, FontStyles.Normal);
        MakeTMP("CapacityFactorLabel", envMetrics, "CF: —",      10, textSec, FontStyles.Normal);

        AddSpacer(rightPanel, 4);

        // ── Forecast panel ───────────────────────────────────
        MakeSectionLabel("ForecastSection", rightPanel, "YIELD FORECAST", textSec);

        GameObject forecastCard = MakePanel("ForecastCard", rightPanel, bg2);
        SetLayoutElement(forecastCard, -1, 240, true, false);
        var fcVL = forecastCard.AddComponent<VerticalLayoutGroup>();
        fcVL.padding = new RectOffset(0, 0, 4, 4);
        fcVL.spacing = 0;
        fcVL.childControlWidth = true; fcVL.childForceExpandWidth = true;
        fcVL.childControlHeight = false;

        // Table header
        GameObject fcHeader = MakePanel("ForecastHeader", forecastCard, bg3);
        SetLayoutElement(fcHeader, -1, 28, true, false);
        var fchHL = fcHeader.AddComponent<HorizontalLayoutGroup>();
        fchHL.padding = new RectOffset(12, 12, 0, 0);
        fchHL.childControlWidth = true; fchHL.childControlHeight = true;
        fchHL.childForceExpandWidth = true; fchHL.childForceExpandHeight = true;
        MakeTMP("FCH_Period",  fcHeader, "PERIOD",   9, textSec, FontStyles.Bold);
        MakeTMP("FCH_H2",      fcHeader, "H₂ YIELD", 9, textSec, FontStyles.Bold);
        MakeTMP("FCH_Revenue", fcHeader, "REVENUE",  9, textSec, FontStyles.Bold);
        MakeTMP("FCH_ROI",     fcHeader, "CAPEX %",  9, textSec, FontStyles.Bold);

        // Table body (scroll view for rows)
        GameObject forecastTableBody = MakePanel("ForecastTableBody", forecastCard, Color.clear);
        SetLayoutElement(forecastTableBody, -1, -1, true, false);
        forecastTableBody.AddComponent<LayoutElement>().flexibleHeight = 1;
        var ftbVL = forecastTableBody.AddComponent<VerticalLayoutGroup>();
        ftbVL.childControlWidth = true; ftbVL.childForceExpandWidth = true;
        ftbVL.childControlHeight = false;

        // Refresh button
        MakeButton("RefreshForecastBtn", forecastCard, "↻  CALCULATE FORECAST", accentDim, bg0);
        SetLayoutElement(forecastCard.transform.Find("RefreshForecastBtn").gameObject, -1, 36, true, false);

        // Summary totals
        GameObject fcSummary = MakePanel("ForecastSummary", rightPanel, bg2);
        SetLayoutElement(fcSummary, -1, 60, true, false);
        AddRoundedBorder(fcSummary, accentDim);
        var fcsVL = fcSummary.AddComponent<VerticalLayoutGroup>();
        fcsVL.padding = new RectOffset(12, 12, 6, 6);
        fcsVL.spacing = 3;
        fcsVL.childControlWidth = true; fcsVL.childForceExpandWidth = true;
        MakeTMP("ForecastH2TotalText",  fcSummary, "Total H₂: —",         11, accent,  FontStyles.Bold);
        MakeTMP("ForecastRevenueText",  fcSummary, "Revenue: £—",          10, textPri, FontStyles.Normal);
        MakeTMP("ForecastPaybackText",  fcSummary, "CAPEX payback: —",     10, warn,    FontStyles.Normal);

        AddSpacer(rightPanel, 4);

        // ── Alerts panel ─────────────────────────────────────
        GameObject alertsHeader = MakePanel("AlertsHeader", rightPanel, Color.clear);
        SetLayoutElement(alertsHeader, -1, 24, true, false);
        var ahHL = alertsHeader.AddComponent<HorizontalLayoutGroup>();
        ahHL.childControlWidth = true; ahHL.childControlHeight = true;
        ahHL.childForceExpandWidth = true; ahHL.childForceExpandHeight = true;
        MakeSectionLabelInline("AlertsTitle", alertsHeader, "SYSTEM ALERTS", textSec);
        MakeButton("ClearAlertsBtn", alertsHeader, "CLEAR", bg3, textSec);
        SetLayoutElement(alertsHeader.transform.Find("ClearAlertsBtn").gameObject, 60, 22, false, false);

        GameObject alertsCard = MakePanel("AlertsCard", rightPanel, bg2);
        SetLayoutElement(alertsCard, -1, -1, true, false);
        alertsCard.AddComponent<LayoutElement>().flexibleHeight = 1;
        var acVL = alertsCard.AddComponent<VerticalLayoutGroup>();
        acVL.padding = new RectOffset(8, 8, 6, 6);
        acVL.spacing = 4;
        acVL.childControlWidth = true; acVL.childForceExpandWidth = true;
        acVL.childControlHeight = false;

        // "No alerts" placeholder
        GameObject noAlerts = MakePanel("NoAlertsLabel", alertsCard, Color.clear);
        SetLayoutElement(noAlerts, -1, 40, true, false);
        var nat = MakeTMP("NoAlertsText", noAlerts, "✓  No active alerts", 11, accentDim, FontStyles.Normal);
        nat.alignment = TextAlignmentOptions.Center;
        StretchFull(nat.gameObject);

        // AlertsContainer (where alert rows get instantiated)
        GameObject alertsContainer = MakePanel("AlertsContainer", alertsCard, Color.clear);
        alertsContainer.AddComponent<LayoutElement>().flexibleHeight = 1;
        var alertsContVL = alertsContainer.AddComponent<VerticalLayoutGroup>();
        alertsContVL.childControlWidth = true; alertsContVL.childForceExpandWidth = true;
        alertsContVL.childControlHeight = false; alertsContVL.spacing = 3;

        // ── Create Forecast Row Prefab ───────────────────────
        CreateForecastRowPrefab(bg2, bg3, textPri, textSec, accent, warn);

        // ── Create Alert Row Prefab ──────────────────────────
        CreateAlertRowPrefab(bg3, textPri);

        // ── Separate vertical bar between panels ─────────────
        // (1px dividers via panel bg colour — already achieved by bg1 vs bg0)

        Debug.Log("[Fluxero] Dashboard UI built successfully. Wire Inspector references in FluxeroDashboardV2 and InputConfigPanel.");
        Selection.activeGameObject = canvasGO;
        EditorUtility.DisplayDialog("Fluxero Dashboard Builder",
            "✓ Dashboard built!\n\nNext steps:\n" +
            "1. Add FluxeroDashboardV2.cs to [FLUXERO_MANAGER]\n" +
            "2. Wire all Inspector references (drag GameObjects into slots)\n" +
            "3. Assign LogoGlowMaterial to logo material slot\n" +
            "4. Hit Play and click START", "Got it");
    }

    // ═════════════════════════════════════════════════════════════════
    // PREFAB CREATORS
    // ═════════════════════════════════════════════════════════════════

    static void CreateForecastRowPrefab(Color bg2, Color bg3, Color textPri, Color textSec, Color accent, Color warn)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Fluxero_System/UI_Visuals/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Fluxero_System/UI_Visuals", "Prefabs");

        GameObject row = new GameObject("ForecastRow_Prefab");
        SetBg(row, Color.clear);
        SetLayoutElement(row, -1, 28, true, false);
        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset(12, 12, 0, 0);
        hl.childControlWidth = true; hl.childControlHeight = true;
        hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;

        MakeTMP("Col_Period",  row, "—",    10, textSec, FontStyles.Normal);
        MakeTMP("Col_H2",      row, "—",    10, accent,  FontStyles.Bold);
        MakeTMP("Col_Revenue", row, "£—",   10, textPri, FontStyles.Normal);
        MakeTMP("Col_ROI",     row, "—%",   10, warn,    FontStyles.Normal);

        string path = "Assets/Fluxero_System/UI_Visuals/Prefabs/ForecastRow.prefab";
        PrefabUtility.SaveAsPrefabAsset(row, path);
        DestroyImmediate(row);
        Debug.Log($"[Fluxero] Forecast row prefab saved to {path}");
    }

    static void CreateAlertRowPrefab(Color bg, Color textPri)
    {
        GameObject row = new GameObject("AlertRow_Prefab");
        SetBg(row, bg);
        SetLayoutElement(row, -1, 32, true, false);
        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset(8, 8, 4, 4);
        hl.childControlWidth = true; hl.childControlHeight = true;
        hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;

        var lbl = MakeTMP("AlertText", row, "🟢 Alert message", 10, textPri, FontStyles.Normal);
        lbl.overflowMode = TextOverflowModes.Ellipsis;

        string path = "Assets/Fluxero_System/UI_Visuals/Prefabs/AlertRow.prefab";
        PrefabUtility.SaveAsPrefabAsset(row, path);
        DestroyImmediate(row);
        Debug.Log($"[Fluxero] Alert row prefab saved to {path}");
    }

    // ═════════════════════════════════════════════════════════════════
    // COMPONENT HELPERS
    // ═════════════════════════════════════════════════════════════════

    static void MakeGaugeCard(GameObject parent, string name, string label,
        string valueObjName, string unitObjName,
        string defaultVal, string defaultUnit,
        Color valueColor, Color bg, Color textPri, Color textSec)
    {
        GameObject card = MakePanel(name, parent, bg);
        var vl = card.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(12, 12, 10, 10);
        vl.spacing = 4;
        vl.childControlWidth = true; vl.childForceExpandWidth = true;
        vl.childAlignment = TextAnchor.MiddleCenter;

        MakeTMP(label + "_Label", card, label, 9, textSec, FontStyles.Normal).alignment = TextAlignmentOptions.Center;

        // Value + unit row
        GameObject valRow = MakePanel(name + "_ValRow", card, Color.clear);
        SetLayoutElement(valRow, -1, 42, true, false);
        var vrHL = valRow.AddComponent<HorizontalLayoutGroup>();
        vrHL.childAlignment = TextAnchor.MiddleCenter;
        vrHL.childControlWidth = false; vrHL.childControlHeight = true;
        vrHL.childForceExpandHeight = true; vrHL.spacing = 4;

        var valText = MakeTMP(valueObjName, valRow, defaultVal, 28, valueColor, FontStyles.Bold);
        valText.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 42);

        if (unitObjName != null)
        {
            var unitText = MakeTMP(unitObjName, valRow, defaultUnit, 12, textSec, FontStyles.Normal);
            unitText.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 42);
        }
    }

    static void MakeTotalCard(GameObject parent, string name, string label,
        string valueObjName, string defaultVal,
        Color valueColor, Color bg, Color textPri, Color textSec)
    {
        GameObject card = MakePanel(name, parent, bg);
        var vl = card.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(12, 12, 8, 8);
        vl.spacing = 4;
        vl.childControlWidth = true; vl.childForceExpandWidth = true;
        vl.childAlignment = TextAnchor.MiddleCenter;

        MakeTMP(name + "_Label", card, label, 9, textSec, FontStyles.Normal).alignment = TextAlignmentOptions.Center;
        var val = MakeTMP(valueObjName, card, defaultVal, 16, valueColor, FontStyles.Bold);
        val.alignment = TextAlignmentOptions.Center;
    }

    static void MakeSliderRow(GameObject parent, string label, string sliderName, string inputName,
        string unit, float min, float max, float defaultVal,
        Color bg, Color fillColor, Color textPri, Color textSec)
    {
        GameObject row = MakePanel(label + "Row", parent, bg);
        SetLayoutElement(row, -1, 52, true, false);
        var vl = row.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(12, 12, 6, 6);
        vl.spacing = 4;
        vl.childControlWidth = true; vl.childForceExpandWidth = true;

        // Label row
        GameObject labelRow = MakePanel(label + "LabelRow", row, Color.clear);
        SetLayoutElement(labelRow, -1, 16, true, false);
        var lrHL = labelRow.AddComponent<HorizontalLayoutGroup>();
        lrHL.childControlWidth = true; lrHL.childControlHeight = true;
        lrHL.childForceExpandWidth = true; lrHL.childForceExpandHeight = true;
        MakeTMP(label + "Label", labelRow, label.ToUpper(), 9, textSec, FontStyles.Normal);
        MakeTMP(inputName, labelRow, $"{defaultVal:F1} {unit}", 10, textPri, FontStyles.Bold);

        // Slider
        GameObject sliderGO = MakeSliderElement(sliderName, row, fillColor, Hex("#252D3A"));
        SetLayoutElement(sliderGO, -1, 12, true, false);
        Slider s = sliderGO.GetComponentInChildren<Slider>();
        if (s != null) { s.minValue = min; s.maxValue = max; s.value = defaultVal; }
    }

    static GameObject MakeSliderElement(string name, GameObject parent, Color fillColor, Color trackColor)
    {
        GameObject go = MakePanel(name, parent, trackColor);
        var slider = go.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;

        GameObject fillArea = MakePanel("Fill Area", go, Color.clear);
        StretchFull(fillArea);

        GameObject fill = MakePanel("Fill", fillArea, fillColor);
        StretchFull(fill);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.value = 0.5f;
        return go;
    }

    static void MakeInputRow(GameObject parent, string label, string inputName,
        string defaultVal, Color textPri, Color textSec, Color inputBg)
    {
        GameObject row = MakePanel(label + "_Row", parent, Color.clear);
        SetLayoutElement(row, -1, 28, true, false);
        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 8;
        hl.childControlWidth = true; hl.childControlHeight = true;
        hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;

        MakeTMP(label + "_Label", row, label, 10, textSec, FontStyles.Normal);

        GameObject inputGO = MakePanel(inputName, row, inputBg);
        SetLayoutElement(inputGO, 80, -1, false, true);
        var input = inputGO.AddComponent<TMP_InputField>();
        var inputText = MakeTMP(inputName + "_Text", inputGO, defaultVal, 11, textPri, FontStyles.Normal);
        input.textComponent = inputText;
        input.text = defaultVal;
    }

    static Button MakeButton(string name, GameObject parent, string label, Color bg, Color textColor)
    {
        GameObject go = MakePanel(name, parent, bg);
        var btn = go.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor      = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
        cb.pressedColor     = Color.Lerp(bg, Color.black, 0.2f);
        btn.colors = cb;

        var txt = MakeTMP(name + "_Label", go, label, 11, textColor, FontStyles.Bold);
        txt.alignment = TextAlignmentOptions.Center;
        StretchFull(txt.gameObject);
        return btn;
    }

    static void MakeSectionLabel(string name, GameObject parent, string text, Color col)
    {
        GameObject go = MakePanel(name + "_Header", parent, Color.clear);
        SetLayoutElement(go, -1, 20, true, false);
        var lbl = MakeTMP(name + "_Label", go, text, 9, col, FontStyles.Bold);
        lbl.characterSpacing = 3f;
        StretchFull(lbl.gameObject);
    }

    static void MakeSectionLabelInline(string name, GameObject parent, string text, Color col)
    {
        var lbl = MakeTMP(name, parent, text, 9, col, FontStyles.Bold);
        lbl.characterSpacing = 3f;
    }

    // ═════════════════════════════════════════════════════════════════
    // PRIMITIVE HELPERS
    // ═════════════════════════════════════════════════════════════════

    static GameObject MakePanel(string name, GameObject parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        go.AddComponent<RectTransform>();
        return go;
    }

    static TextMeshProUGUI MakeTMP(string name, GameObject parent, string text, float size, Color color, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    static void SetBg(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static void SetLayoutElement(GameObject go, float preferredWidth, float preferredHeight,
        bool flexibleWidth, bool flexibleHeight)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (preferredWidth  > 0) le.preferredWidth  = preferredWidth;
        if (preferredHeight > 0) le.preferredHeight = preferredHeight;
        if (flexibleWidth)  le.flexibleWidth  = 1;
        if (flexibleHeight) le.flexibleHeight = 1;
    }

    static void AddRoundedBorder(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = go.GetComponent<Image>()?.color ?? Color.clear;
        // Outline component for border effect
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1, -1);
    }

    static void AddSpacer(GameObject parent, float height)
    {
        var go = new GameObject("Spacer");
        go.transform.SetParent(parent.transform, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
#endif