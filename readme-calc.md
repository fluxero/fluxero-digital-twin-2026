# Fluxero H₂ Site Calculator

A physics-based green hydrogen production calculator built for Fluxero's Small Modular Hydrogen Plant (SMHP) platform. Given a UK site location and energy source configuration, it computes monthly and annual hydrogen output, revenue projections, payback period, and CO₂ savings — with no external API calls required for the core calculation.

---

## What it does

### Core calculation engine
- Accepts solar, wind, hydro, hybrid, or lab/PSU as the energy source
- Looks up **postcode-specific solar irradiance** (PVGIS 10-year dataset) and **wind speed data** (NOABL database) for every UK postcode area — all bundled locally, no internet needed
- Applies **monthly temperature derating** to solar panels based on latitude band
- Models **IEC 61400 wind turbine power curves** with hub height correction (power law exponent 0.143) and Weibull k=2 variability correction
- Uses **published alkaline and PEM electrolyser efficiency curves** (Nel A-Series, ITM S-Series, DOE Hydrogen Roadmap 2023) — not a flat efficiency number
- Enforces alkaline minimum load (20%) and calculates curtailment loss
- Supports second-life solar panels (Fluxero's Solarel partnership — £360/kWp vs £800/kWp)
- Outputs monthly H₂ (kg), annual H₂, annual revenue, estimated CAPEX, payback period, and CO₂ avoided

### AI site analysis
- After calculation, calls the Anthropic Claude API to generate a plain-language site summary, location-specific insights, main risk, and top recommendation
- Designed to work inside a Claude artifact sandbox (auth handled automatically)
- Fails silently if the API is unavailable — all core physics still runs

### Unity export
- Generates a structured JSON config (location, source type, electrolyser spec, monthly H₂ output) ready to load into the Fluxero Unity digital twin
- Copy-to-clipboard button built in

---

## Tech stack

| Layer | Detail |
|---|---|
| Framework | React (hooks only, no class components) |
| Charts | Recharts (`BarChart`) |
| Fonts | Sora + DM Mono (Google Fonts) |
| Styling | Inline styles, no CSS framework |
| External deps | `recharts` only |
| API calls | Anthropic `/v1/messages` (AI insights only, optional) |

---

## How to run locally

### Prerequisites
- Node.js v16+ ([nodejs.org](https://nodejs.org))
- npm (comes with Node)

### Steps

```bash
# 1. Create a new React app
npx create-react-app fluxero-h2-calculator
cd fluxero-h2-calculator

# 2. Install the only dependency
npm install recharts

# 3. Replace src/App.js with the calculator code
# Open src/App.js, delete all contents, paste in H2Calculator.jsx

# 4. Start the dev server
npm start
```

Opens at `http://localhost:3000`.

### Fastest alternative (no install)
Paste the code directly into [stackblitz.com](https://stackblitz.com) → New Project → React. Runs instantly in the browser.

---

## File structure

```
src/
└── App.js          ← entire app (single-file component)
public/
└── index.html      ← standard CRA boilerplate
package.json        ← only extra dep: recharts
```

---

## Step-by-step user flow

```
1. Source type     → Solar / Wind / Hybrid / Hydro / Lab PSU
2. Location        → UK postcode (e.g. DH1 3RG)
3. Source config   → Panel count, turbine spec, etc.
4. Electrolyser    → Alkaline or PEM, rated kW, optional storage/CAPEX
5. Revenue model   → Market rate / Fixed contract / On-site diesel displacement
6. Calculate       → Physics engine runs → AI insights fetch → Results screen
```

---

## Physics constants used

| Constant | Value | Source |
|---|---|---|
| H₂ higher heating value | 39.4 kWh/kg | Standard |
| Wind shear exponent | 0.143 | IEC 61400, open terrain |
| Air density | 1.225 kg/m³ | Standard |
| Realistic Cp | 0.40 | (Betz limit = 0.593) |
| Alkaline min load | 20% | Nel A-Series datasheet |
| Weibull k factor | 2 (Rayleigh) | Wind energy standard |

---

## Revenue modes

| Mode | Basis |
|---|---|
| Market rate | £8.50/kg (UK 2025) |
| Fixed contract | Your agreed £/kg with buyer |
| On-site use | Diesel displacement — diesel price × 1.25 × 9 |

---

## CAPEX estimation (if not overridden)

| Component | Cost |
|---|---|
| Alkaline electrolyser | £700/kW |
| PEM electrolyser | £1,200/kW |
| Solar (new panels) | £800/kWp |
| Solar (second-life) | £360/kWp |
| Wind turbines | £1,500/kW rated |

---

## Notes for developers

- All postcode lookup data is hardcoded in `SOLAR` and `WIND_SPD` objects — no external API call
- The AI insights block (`getAIInsights`) can be removed entirely without affecting calculations
- The Unity JSON export block at the bottom of results is designed to be consumed by the Fluxero Unity digital twin app
- The `runCalc()` function is pure (no side effects) and can be extracted and unit-tested independently

---

## Related

- [Fluxero Digital Twin](https://github.com/fluxero) — Unity visualisation layer that consumes the JSON export from this calculator
- Fluxero pitch deck — see `/docs` branch

---

*Built by Fluxero · Durham, UK · 2026*
