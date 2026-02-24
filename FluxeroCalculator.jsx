import { useState } from "react";
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from "recharts";

// ═══════════════════════════════════════════════════════════════════
// PHYSICS CONSTANTS
// ═══════════════════════════════════════════════════════════════════
const H2_HHV       = 39.4;   // kWh/kg higher heating value
const WIND_SHEAR   = 0.143;  // power law exponent, open terrain IEC 61400
const AIR_DENSITY  = 1.225;  // kg/m³
const BETZ_CP      = 0.40;   // realistic Cp (Betz limit = 0.593)
const MIN_LOAD_ALK = 0.20;   // alkaline electrolyser min load fraction

// Published efficiency curves (load fraction → DC efficiency)
// Sources: Nel A-Series, ITM S-Series, DOE Hydrogen Roadmap 2023
const ALK_CURVE = [[0,0],[0.2,0.55],[0.3,0.60],[0.4,0.63],[0.5,0.65],[0.6,0.670],[0.7,0.685],[0.8,0.680],[0.9,0.665],[1.0,0.650]];
const PEM_CURVE = [[0,0],[0.05,0.60],[0.2,0.65],[0.4,0.68],[0.6,0.70],[0.8,0.695],[1.0,0.68]];

const MONTHS      = ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"];
const MONTH_DAYS  = [31,28,31,30,31,30,31,31,30,31,30,31];
const MONTH_HOURS = [744,672,744,720,744,720,744,744,720,744,720,744];

const PANEL_META = {
  mono:     { label:"Monocrystalline (20–23%)", tempCoeff:-0.0035, desc:"Most efficient. Best for limited space." },
  poly:     { label:"Polycrystalline (15–17%)", tempCoeff:-0.0040, desc:"Good value for large ground installs." },
  thin:     { label:"Thin-film (10–13%)",        tempCoeff:-0.0020, desc:"Works well in diffuse/cloudy light." },
  bifacial: { label:"Bifacial (+8–12% rear)",    tempCoeff:-0.0030, desc:"Captures reflected light. Ideal for ground mounts." },
};

// ═══════════════════════════════════════════════════════════════════
// UK POSTCODE-AREA DATA  (no external API needed)
// Solar: kWh/m²/day monthly averages derived from PVGIS 10-yr dataset
// Wind:  m/s monthly averages derived from NOABL database
// Temps: °C monthly averages for temperature derating
// ═══════════════════════════════════════════════════════════════════

// Solar irradiance kWh/m²/day by postcode area
const SOLAR = {
  // Scotland
  AB:[0.4,0.9,1.9,3.3,4.3,4.6,4.4,3.8,2.6,1.4,0.6,0.3], IV:[0.4,0.9,1.9,3.3,4.4,4.7,4.5,3.8,2.6,1.4,0.6,0.3],
  KW:[0.3,0.8,1.8,3.2,4.3,4.6,4.4,3.7,2.5,1.3,0.5,0.3], ZE:[0.3,0.7,1.7,3.1,4.2,4.5,4.3,3.6,2.4,1.2,0.5,0.2],
  EH:[0.5,1.1,2.1,3.5,4.5,4.9,4.7,4.0,2.8,1.6,0.7,0.4], G:[0.5,1.0,2.1,3.5,4.5,4.9,4.7,4.0,2.8,1.5,0.7,0.4],
  DD:[0.5,1.0,2.0,3.4,4.4,4.8,4.5,3.9,2.7,1.5,0.7,0.4], PH:[0.5,1.0,2.0,3.4,4.4,4.8,4.6,3.9,2.7,1.5,0.7,0.4],
  KA:[0.6,1.1,2.2,3.6,4.6,5.0,4.8,4.1,2.9,1.6,0.8,0.5], TD:[0.6,1.1,2.2,3.6,4.6,5.0,4.8,4.1,2.9,1.6,0.8,0.5],
  FK:[0.5,1.0,2.0,3.4,4.4,4.8,4.6,3.9,2.7,1.5,0.7,0.4], KY:[0.5,1.1,2.1,3.5,4.5,4.9,4.7,4.0,2.8,1.6,0.7,0.4],
  PA:[0.5,1.0,2.1,3.5,4.5,4.9,4.7,4.0,2.8,1.5,0.7,0.4], ML:[0.5,1.0,2.1,3.5,4.5,4.9,4.6,4.0,2.8,1.5,0.7,0.4],
  // Northern England
  NE:[0.6,1.2,2.3,3.7,4.8,5.2,5.0,4.3,3.0,1.7,0.8,0.5], SR:[0.6,1.2,2.3,3.7,4.8,5.2,5.0,4.3,3.0,1.7,0.8,0.5],
  DH:[0.6,1.2,2.3,3.7,4.8,5.2,5.0,4.3,3.0,1.7,0.8,0.5], TS:[0.7,1.3,2.3,3.8,4.8,5.2,5.0,4.3,3.1,1.8,0.9,0.6],
  DL:[0.7,1.3,2.3,3.8,4.8,5.2,5.0,4.3,3.1,1.8,0.9,0.6], CA:[0.6,1.2,2.2,3.7,4.7,5.1,4.9,4.2,3.0,1.7,0.8,0.5],
  LA:[0.6,1.2,2.2,3.7,4.7,5.1,4.9,4.2,3.0,1.7,0.8,0.5], HG:[0.7,1.3,2.4,3.9,4.9,5.3,5.1,4.4,3.1,1.8,0.9,0.6],
  YO:[0.7,1.3,2.4,3.9,5.0,5.4,5.2,4.5,3.2,1.8,0.9,0.6], HU:[0.7,1.3,2.4,3.9,5.0,5.4,5.2,4.5,3.2,1.9,0.9,0.6],
  LS:[0.7,1.3,2.3,3.8,4.8,5.2,5.0,4.3,3.1,1.8,0.9,0.6], WF:[0.7,1.3,2.4,3.9,4.9,5.3,5.1,4.4,3.1,1.8,0.9,0.6],
  BD:[0.7,1.3,2.3,3.8,4.8,5.2,5.0,4.3,3.0,1.7,0.8,0.5], HX:[0.7,1.2,2.3,3.8,4.8,5.2,5.0,4.3,3.0,1.7,0.8,0.5],
  S:[0.8,1.4,2.4,3.9,5.0,5.4,5.2,4.5,3.2,1.9,0.9,0.6],  DN:[0.8,1.4,2.4,3.9,5.0,5.4,5.2,4.5,3.2,1.9,0.9,0.6],
  // Midlands
  NG:[0.8,1.4,2.5,4.0,5.1,5.5,5.2,4.6,3.3,1.9,1.0,0.7], DE:[0.8,1.4,2.5,4.0,5.1,5.5,5.2,4.6,3.3,1.9,1.0,0.7],
  ST:[0.8,1.4,2.5,4.0,5.0,5.5,5.2,4.6,3.3,1.9,1.0,0.7], LE:[0.8,1.5,2.6,4.1,5.1,5.6,5.3,4.7,3.3,2.0,1.0,0.7],
  CV:[0.8,1.5,2.6,4.1,5.1,5.6,5.3,4.7,3.4,2.0,1.0,0.7], NN:[0.8,1.5,2.6,4.1,5.2,5.7,5.4,4.7,3.4,2.0,1.0,0.7],
  WV:[0.8,1.4,2.5,4.0,5.0,5.5,5.2,4.6,3.3,1.9,1.0,0.7], WS:[0.8,1.5,2.6,4.1,5.1,5.6,5.3,4.7,3.4,2.0,1.0,0.7],
  DY:[0.8,1.4,2.5,4.0,5.1,5.5,5.2,4.6,3.3,1.9,1.0,0.7], B:[0.8,1.5,2.6,4.1,5.1,5.6,5.3,4.7,3.4,2.0,1.0,0.7],
  TF:[0.8,1.4,2.5,4.0,5.0,5.5,5.2,4.6,3.3,1.9,1.0,0.7], SY:[0.9,1.5,2.6,4.1,5.2,5.6,5.4,4.8,3.4,1.9,1.0,0.7],
  HR:[0.9,1.6,2.7,4.2,5.3,5.8,5.5,4.9,3.5,2.0,1.0,0.7],
  // East of England
  LN:[0.8,1.5,2.6,4.1,5.2,5.6,5.4,4.8,3.4,2.0,1.0,0.7], PE:[0.9,1.6,2.7,4.2,5.3,5.8,5.5,4.9,3.5,2.1,1.1,0.8],
  CB:[0.9,1.6,2.7,4.2,5.3,5.8,5.5,4.9,3.5,2.1,1.1,0.8], IP:[0.9,1.7,2.8,4.3,5.4,5.9,5.6,5.0,3.6,2.1,1.1,0.8],
  NR:[0.9,1.6,2.8,4.3,5.4,5.9,5.6,5.0,3.6,2.1,1.1,0.8], CO:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  CM:[0.9,1.6,2.8,4.3,5.4,5.9,5.6,5.0,3.6,2.1,1.1,0.8], SS:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  SG:[0.9,1.6,2.7,4.2,5.3,5.8,5.5,4.9,3.5,2.1,1.1,0.8], AL:[0.9,1.6,2.8,4.3,5.4,5.9,5.6,5.0,3.6,2.1,1.1,0.8],
  HP:[0.9,1.6,2.8,4.3,5.4,5.9,5.6,5.0,3.6,2.1,1.1,0.8], MK:[0.9,1.6,2.7,4.2,5.3,5.8,5.5,4.9,3.5,2.1,1.1,0.8],
  LU:[0.9,1.6,2.7,4.2,5.3,5.8,5.5,4.9,3.5,2.1,1.1,0.8], OX:[0.9,1.6,2.8,4.3,5.4,5.9,5.6,5.0,3.6,2.1,1.1,0.8],
  // Greater London & South East
  E:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], EC:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  N:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], NW:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  SE:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], SW:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  W:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], WC:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  WD:[0.9,1.6,2.8,4.3,5.4,5.9,5.6,5.0,3.6,2.1,1.1,0.8], EN:[0.9,1.6,2.8,4.3,5.4,5.9,5.6,5.0,3.6,2.1,1.1,0.8],
  HA:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], UB:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  SL:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], TW:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  KT:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], SM:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  CR:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], BR:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  DA:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], RM:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  RH:[1.0,1.7,3.0,4.5,5.6,6.1,5.8,5.2,3.7,2.2,1.2,0.8], RG:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8],
  GU:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], ME:[1.0,1.8,3.0,4.5,5.6,6.1,5.8,5.2,3.8,2.2,1.2,0.8],
  CT:[1.1,1.8,3.0,4.5,5.6,6.2,5.9,5.3,3.8,2.3,1.2,0.9], TN:[1.1,1.8,3.0,4.5,5.6,6.1,5.8,5.2,3.8,2.3,1.2,0.9],
  BN:[1.1,1.8,3.0,4.5,5.6,6.2,5.9,5.3,3.8,2.3,1.2,0.9],
  // South
  PO:[1.1,1.9,3.1,4.6,5.7,6.3,6.0,5.3,3.9,2.3,1.2,0.9], SO:[1.1,1.9,3.1,4.6,5.7,6.3,6.0,5.3,3.9,2.3,1.2,0.9],
  SP:[1.0,1.8,3.0,4.5,5.6,6.1,5.8,5.2,3.8,2.2,1.2,0.8], BH:[1.1,1.9,3.1,4.6,5.7,6.3,6.0,5.3,3.9,2.3,1.2,0.9],
  DT:[1.1,1.9,3.1,4.6,5.7,6.3,6.0,5.3,3.9,2.3,1.3,0.9], GY:[1.3,2.1,3.4,4.9,6.0,6.6,6.3,5.6,4.2,2.6,1.4,1.1],
  JE:[1.3,2.1,3.4,4.9,6.0,6.6,6.3,5.6,4.2,2.6,1.4,1.1],
  // South West
  BA:[1.0,1.8,3.0,4.5,5.6,6.1,5.8,5.2,3.8,2.2,1.2,0.8], BS:[1.0,1.8,3.0,4.5,5.6,6.1,5.8,5.2,3.8,2.2,1.2,0.8],
  GL:[1.0,1.7,2.9,4.4,5.5,6.0,5.7,5.1,3.7,2.2,1.2,0.8], SN:[1.0,1.8,3.0,4.5,5.6,6.1,5.8,5.2,3.8,2.2,1.2,0.8],
  TA:[1.1,1.9,3.1,4.6,5.7,6.2,5.9,5.3,3.9,2.3,1.2,0.9], EX:[1.1,1.9,3.2,4.7,5.8,6.3,6.0,5.4,3.9,2.4,1.3,0.9],
  TQ:[1.2,2.0,3.3,4.8,5.9,6.4,6.1,5.5,4.0,2.4,1.3,1.0], PL:[1.1,1.9,3.2,4.7,5.8,6.3,6.0,5.4,3.9,2.4,1.3,0.9],
  TR:[1.3,2.1,3.4,4.9,6.0,6.5,6.2,5.6,4.1,2.5,1.4,1.0],
  // Wales
  CF:[0.9,1.6,2.7,4.2,5.3,5.8,5.5,4.9,3.5,2.0,1.0,0.7], NP:[0.9,1.6,2.7,4.2,5.3,5.8,5.5,4.9,3.5,2.0,1.0,0.7],
  SA:[1.0,1.7,2.8,4.3,5.4,5.9,5.6,5.0,3.6,2.1,1.1,0.7], LD:[0.9,1.5,2.6,4.1,5.2,5.7,5.4,4.8,3.4,1.9,1.0,0.7],
  LL:[0.7,1.3,2.3,3.8,4.9,5.3,5.1,4.4,3.1,1.8,0.9,0.6], SY:[0.9,1.5,2.6,4.1,5.2,5.6,5.4,4.8,3.4,1.9,1.0,0.7],
  // N Ireland, Manchester, etc
  BT:[0.6,1.1,2.1,3.6,4.6,5.0,4.8,4.1,2.9,1.6,0.8,0.5],
  M:[0.7,1.2,2.2,3.7,4.7,5.1,4.9,4.2,3.0,1.7,0.8,0.5], OL:[0.7,1.2,2.2,3.7,4.7,5.1,4.9,4.2,3.0,1.7,0.8,0.5],
  SK:[0.7,1.3,2.3,3.8,4.8,5.2,5.0,4.3,3.1,1.8,0.9,0.6], CH:[0.7,1.3,2.3,3.8,4.8,5.2,5.0,4.3,3.1,1.7,0.9,0.6],
  L:[0.7,1.3,2.3,3.8,4.8,5.2,5.0,4.3,3.1,1.7,0.9,0.6],  WA:[0.7,1.3,2.3,3.8,4.8,5.2,5.0,4.3,3.1,1.7,0.9,0.6],
  WN:[0.7,1.2,2.2,3.7,4.7,5.1,4.9,4.2,3.0,1.7,0.8,0.5], BL:[0.7,1.2,2.2,3.7,4.7,5.1,4.9,4.2,3.0,1.7,0.8,0.5],
  BB:[0.7,1.2,2.2,3.7,4.7,5.1,4.9,4.2,3.0,1.7,0.8,0.5], PR:[0.7,1.3,2.3,3.8,4.8,5.2,5.0,4.3,3.1,1.7,0.9,0.6],
  FY:[0.7,1.3,2.3,3.8,4.8,5.2,5.0,4.3,3.1,1.7,0.9,0.6],
  DEFAULT:[0.8,1.4,2.5,4.0,5.0,5.5,5.2,4.6,3.3,1.9,1.0,0.7],
};

// Wind speed at 10m height (m/s) by postcode area — NOABL-derived
const WIND_SPD = {
  ZE:[9.0,8.8,8.1,7.5,7.2,6.9,6.8,7.0,7.5,8.1,8.6,9.1], KW:[8.0,7.8,7.2,6.7,6.5,6.2,6.1,6.3,6.7,7.2,7.7,8.1],
  IV:[7.8,7.5,7.0,6.5,6.3,6.0,5.9,6.1,6.5,7.0,7.5,7.9], AB:[7.2,7.0,6.5,6.0,5.8,5.5,5.4,5.6,6.0,6.5,7.0,7.3],
  PA:[6.8,6.6,6.1,5.6,5.4,5.1,5.0,5.2,5.6,6.1,6.5,6.9], LL:[6.8,6.6,6.1,5.6,5.4,5.1,5.0,5.2,5.6,6.1,6.5,6.9],
  SA:[6.5,6.3,5.8,5.4,5.2,4.9,4.8,5.0,5.4,5.8,6.2,6.6], TR:[7.0,6.8,6.3,5.8,5.6,5.3,5.2,5.4,5.8,6.3,6.7,7.1],
  PL:[6.5,6.3,5.8,5.4,5.2,4.9,4.8,5.0,5.4,5.8,6.2,6.6], EX:[5.8,5.6,5.2,4.8,4.6,4.3,4.2,4.4,4.8,5.2,5.5,5.9],
  CA:[6.2,6.0,5.6,5.2,5.0,4.8,4.7,4.8,5.2,5.6,6.0,6.3], NE:[5.2,5.1,4.7,4.3,4.2,4.0,3.9,4.0,4.3,4.7,5.0,5.3],
  DH:[5.0,4.9,4.5,4.2,4.1,3.9,3.8,3.9,4.2,4.5,4.8,5.1], SR:[5.0,4.9,4.5,4.2,4.1,3.9,3.8,3.9,4.2,4.5,4.8,5.1],
  TS:[5.0,4.9,4.5,4.2,4.0,3.8,3.7,3.8,4.2,4.5,4.8,5.1], YO:[5.2,5.1,4.7,4.3,4.2,4.0,3.9,4.0,4.3,4.7,5.0,5.3],
  HU:[5.5,5.4,5.0,4.6,4.4,4.2,4.1,4.2,4.6,5.0,5.3,5.6], LN:[5.3,5.2,4.8,4.4,4.3,4.1,4.0,4.1,4.4,4.8,5.1,5.4],
  NR:[5.8,5.7,5.3,4.9,4.7,4.5,4.4,4.5,4.9,5.3,5.6,5.9], IP:[5.5,5.4,5.0,4.6,4.4,4.2,4.1,4.2,4.6,5.0,5.3,5.6],
  CO:[5.3,5.2,4.8,4.4,4.3,4.1,4.0,4.1,4.4,4.8,5.1,5.4], SS:[5.2,5.1,4.7,4.3,4.2,4.0,3.9,4.0,4.3,4.7,5.0,5.3],
  CT:[5.8,5.7,5.3,4.9,4.7,4.5,4.4,4.5,4.9,5.3,5.6,5.9], TN:[4.8,4.7,4.3,4.0,3.9,3.7,3.6,3.7,4.0,4.3,4.6,4.9],
  BN:[5.0,4.9,4.5,4.2,4.0,3.8,3.7,3.8,4.2,4.5,4.8,5.1], PO:[5.2,5.1,4.7,4.3,4.2,4.0,3.9,4.0,4.3,4.7,5.0,5.3],
  EC:[3.5,3.4,3.2,2.9,2.8,2.7,2.6,2.7,2.9,3.2,3.4,3.5], WC:[3.5,3.4,3.2,2.9,2.8,2.7,2.6,2.7,2.9,3.2,3.4,3.5],
  SW:[3.8,3.7,3.4,3.1,3.0,2.9,2.8,2.9,3.1,3.4,3.6,3.8],
  CV:[4.2,4.1,3.8,3.5,3.4,3.2,3.2,3.3,3.5,3.8,4.0,4.2], LE:[4.4,4.3,4.0,3.7,3.5,3.4,3.3,3.4,3.7,4.0,4.2,4.4],
  NN:[4.5,4.4,4.0,3.7,3.6,3.4,3.3,3.4,3.7,4.0,4.3,4.5],
  BT:[6.0,5.8,5.4,5.0,4.8,4.6,4.5,4.6,5.0,5.4,5.8,6.1],
  DEFAULT:[5.0,4.9,4.5,4.2,4.0,3.8,3.7,3.8,4.2,4.5,4.8,5.1],
};

// Monthly temps °C by latitude band (for panel temp derating)
const TEMPS = {
  far_north:[3,3,5,8,11,14,16,16,13,9,5,3],  // Scotland/N.Ireland
  north:    [4,4,6,9,12,15,17,17,14,10,6,4],  // N.England
  mid:      [5,5,7,10,13,16,18,18,15,11,7,5], // Midlands
  south:    [6,6,8,11,14,17,19,19,16,12,8,6], // South England/Wales
};

function getPostcodeArea(postcode) {
  return (postcode||"").toUpperCase().replace(/\s/g,"").match(/^([A-Z]{1,2})/)?.[1] || "";
}

function getSolarData(postcode) {
  const area = getPostcodeArea(postcode);
  return SOLAR[area] || SOLAR.DEFAULT;
}

function getWindData(postcode) {
  const area = getPostcodeArea(postcode);
  return WIND_SPD[area] || WIND_SPD.DEFAULT;
}

function getTempData(lat) {
  if (lat > 56) return TEMPS.far_north;
  if (lat > 53) return TEMPS.north;
  if (lat > 51) return TEMPS.mid;
  return TEMPS.south;
}

// Rough lat from postcode area for temp derating
function latFromPostcode(postcode) {
  const area = getPostcodeArea(postcode);
  const north = ["AB","DD","EH","FK","G","IV","KA","KW","KY","ML","PA","PH","TD","ZE","NE","SR","DH","CA","DL","TS","BT"];
  const midNorth = ["LA","BB","FY","PR","L","M","WN","BL","OL","SK","WA","CH","HG","YO","HU","LS","WF","BD","HX","S","DN","NG","LN"];
  if (north.includes(area)) return 57;
  if (midNorth.includes(area)) return 53.5;
  const south = ["TR","PL","TQ","EX","CT","BN","PO","TN","GY","JE"];
  if (south.includes(area)) return 50.5;
  return 52;
}

// ═══════════════════════════════════════════════════════════════════
// PHYSICS ENGINE
// ═══════════════════════════════════════════════════════════════════

function lerpCurve(curve, x) {
  if (x <= curve[0][0]) return curve[0][1];
  for (let i = 0; i < curve.length - 1; i++) {
    const [x0,y0] = curve[i], [x1,y1] = curve[i+1];
    if (x <= x1) return y0 + (x-x0)/(x1-x0)*(y1-y0);
  }
  return curve[curve.length-1][1];
}

function electEff(loadFrac, type) {
  if (type === "alkaline" && loadFrac < MIN_LOAD_ALK) return 0;
  if (type === "pem" && loadFrac < 0.05) return 0;
  return lerpCurve(type === "alkaline" ? ALK_CURVE : PEM_CURVE, loadFrac);
}

function turbinePower(v10m, p) {
  const vH = v10m * Math.pow(Math.max(p.hubH,1)/10, WIND_SHEAR);
  if (vH < p.cutIn || vH > p.cutOut) return 0;
  if (vH >= p.vRated) return p.pRatedKW;
  return p.pRatedKW * Math.pow((vH-p.cutIn)/(p.vRated-p.cutIn), 3);
}

function calcSolar(postcode, solar) {
  const irr = getSolarData(postcode);
  const kWp = (Number(solar.numPanels) * Number(solar.panelWatt)) / 1000;
  const sysLoss = 1 - (Number(solar.lossP)||14)/100;
  const invEff  = (Number(solar.invEff)||97)/100;

  // Geometry: tilt & orientation correction vs optimal 35° south
  const tiltRad = ((Number(solar.tilt)||35) * Math.PI) / 180;
  const aspectRad = ((Number(solar.orient)||0) * Math.PI) / 180;
  const tiltFactor   = Math.max(0.6, Math.cos(tiltRad - 0.6109));
  const aspectFactor = Math.cos(aspectRad) * 0.12 + 0.88;
  const geom = tiltFactor * aspectFactor;

  const bifacialBonus = solar.panelType === "bifacial" ? 1.09 : 1.0;
  const tempCoeff = PANEL_META[solar.panelType||"mono"]?.tempCoeff || -0.0035;
  const lat = latFromPostcode(postcode);
  const temps = getTempData(lat);

  return irr.map((d, m) => {
    const tempDerating = 1 + tempCoeff * (temps[m] - 25);
    return Math.max(0, d * MONTH_DAYS[m] * kWp * sysLoss * invEff * geom * bifacialBonus * tempDerating);
  });
}

function calcWind(postcode, wind) {
  const avgSpeeds = getWindData(postcode);
  const turbP = {
    hubH: Number(wind.hubH)||30, cutIn: Number(wind.cutIn)||3,
    cutOut: Number(wind.cutOut)||25, vRated: Number(wind.vRated)||12,
    pRatedKW: Number(wind.pRatedKW)||50,
  };
  const nTurb = Number(wind.nTurbines)||1;
  const wake  = nTurb > 1 ? 1 - (Number(wind.wakeLoss)||8)/100 : 1.0;

  return avgSpeeds.map((v, m) => {
    // Weibull k=2 correction: accounts for wind speed variability within month
    // Effective energy ≈ P(v_avg * 0.88) for k=2 Rayleigh distribution
    const vEff = v * 0.88;
    return turbinePower(vEff, turbP) * nTurb * wake * MONTH_HOURS[m];
  });
}

function calcH2(monthlyKWh, electKW, electType, storageDayKg) {
  return monthlyKWh.map((kWh, m) => {
    if (kWh <= 0 || electKW <= 0) return 0;
    const avgP = kWh / MONTH_HOURS[m];
    const load = Math.min(avgP / electKW, 1.0);
    const eff  = electEff(load, electType);
    if (eff === 0) return 0;
    let h2 = (kWh * eff) / H2_HHV;
    if (storageDayKg > 0) h2 = Math.min(h2, storageDayKg * MONTH_DAYS[m]);
    return h2;
  });
}

function runCalc(inputs) {
  const { sourceType, postcode, lat: latIn, solar, wind, hydro, lab, elect, revenue } = inputs;

  let monthlyKWh = Array(12).fill(0);
  const dataSources = [];

  const area = getPostcodeArea(postcode);
  const locationLabel = postcode ? `${postcode.toUpperCase()} (${area} area)` : (latIn ? `${latIn}°N` : "UK estimate");

  if (sourceType === "solar" || sourceType === "hybrid") {
    const kWh = calcSolar(postcode, solar);
    monthlyKWh = monthlyKWh.map((v,i) => v + kWh[i]);
    dataSources.push(`Solar: ${area ? area+" area" : "UK"} postcode-specific irradiance + temp derating`);
  }

  if (sourceType === "wind" || sourceType === "hybrid") {
    const kWh = calcWind(postcode, wind);
    monthlyKWh = monthlyKWh.map((v,i) => v + kWh[i]);
    dataSources.push(`Wind: ${area ? area+" area" : "UK"} NOABL-derived speeds + Weibull k=2 + hub height correction`);
  }

  if (sourceType === "hydro") {
    const kW = (Number(hydro?.capacityKW)||0) * ((Number(hydro?.capFactor)||45)/100);
    monthlyKWh = MONTH_HOURS.map(h => kW * h);
    dataSources.push("Hydro: rated capacity × capacity factor");
  }

  if (sourceType === "lab") {
    const kW = Number(lab?.powerKW)||0;
    const hrs = Number(lab?.hoursPerDay)||8;
    monthlyKWh = MONTH_HOURS.map(h => kW * (hrs/24) * h);
    dataSources.push("Lab PSU: flat power profile");
  }

  const electKW     = Number(elect.ratedKW)||1;
  const electType   = elect.type || "alkaline";
  const storageDayKg = Number(elect.storageKgDay)||0;
  const h2Monthly   = calcH2(monthlyKWh, electKW, electType, storageDayKg);

  const annualH2Kg = h2Monthly.reduce((a,b) => a+b, 0);
  const annualKWh  = monthlyKWh.reduce((a,b) => a+b, 0);

  let h2Price = 8.50;
  if (revenue.mode === "contract") h2Price = parseFloat(revenue.customPrice)||8.50;
  if (revenue.mode === "onsite")   h2Price = (parseFloat(revenue.dieselPrice)||1.55) * 1.25 * 9;
  const annualRevenue = annualH2Kg * h2Price;

  // CAPEX
  let capex = Number(elect.capexGBP)||0;
  if (!capex) {
    capex += electKW * (electType === "pem" ? 1200 : 700);
    if (sourceType === "solar" || sourceType === "hybrid") {
      const kWp = (Number(solar.numPanels)*Number(solar.panelWatt))/1000;
      capex += kWp * (solar.secondLife !== false ? 360 : 800);
    }
    if (sourceType === "wind" || sourceType === "hybrid") {
      capex += Number(wind.pRatedKW) * (Number(wind.nTurbines)||1) * 1500;
    }
  }

  const payback = annualRevenue > 0 ? capex / annualRevenue : null;
  const co2Saved = annualH2Kg * 9 / 1000;

  // Curtailment
  const curtailmentPct = (() => {
    let wasted = 0, total = 0;
    h2Monthly.forEach((h2,m) => {
      const maxH2 = (monthlyKWh[m] * 0.685) / H2_HHV;
      wasted += Math.max(0, maxH2 - h2);
      total  += maxH2;
    });
    return total > 0 ? (wasted/total)*100 : 0;
  })();

  // Confidence
  const hasPostcode = !!postcode && area.length > 0;
  const opts = [elect.storageKgDay, elect.capexGBP].filter(Boolean).length;
  const confidence = hasPostcode && opts >= 1 ? "Detailed Projection"
    : hasPostcode ? "Good Estimate" : "Rough Estimate";

  // Unity config
  const unityConfig = {
    version:"1.0", source:"FluxeroCalculator", timestamp:new Date().toISOString(),
    location:{ postcode: postcode||"", label:locationLabel },
    sourceType,
    solar:(sourceType==="solar"||sourceType==="hybrid")?{
      kWp:(Number(solar.numPanels)*Number(solar.panelWatt))/1000,
      numPanels:Number(solar.numPanels), panelWatt:Number(solar.panelWatt),
      tiltDeg:Number(solar.tilt)||35, azimuthDeg:Number(solar.orient)||0,
      panelType:solar.panelType,
    }:null,
    wind:(sourceType==="wind"||sourceType==="hybrid")?{
      nTurbines:Number(wind.nTurbines)||1, pRatedKW:Number(wind.pRatedKW),
      hubHeightM:Number(wind.hubH)||30, rotorDiamM:Number(wind.rotorDiam)||18,
    }:null,
    electrolyser:{ ratedKW:electKW, type:electType },
    monthlyH2Kg:h2Monthly.map(v=>+v.toFixed(2)),
    annualH2Kg:+annualH2Kg.toFixed(2),
    annualRevenueGBP:+annualRevenue.toFixed(2),
    capexGBP:+capex.toFixed(0),
  };

  return { locationLabel, monthlyKWh, h2Monthly, annualH2Kg, annualKWh, annualRevenue,
    h2Price, capex, payback, co2Saved, curtailmentPct, confidence, dataSources, unityConfig };
}

// ═══════════════════════════════════════════════════════════════════
// AI ENRICHMENT via Anthropic API (allowed in artifact sandbox)
// ═══════════════════════════════════════════════════════════════════
async function getAIInsights(inputs, results) {
  try {
    const res = await fetch("https://api.anthropic.com/v1/messages", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        model: "claude-sonnet-4-20250514",
        max_tokens: 700,
        messages:[{ role:"user", content:`You are a green hydrogen energy expert. A Fluxero Small Modular Hydrogen Plant calculation has been run. Provide expert analysis.

Location: ${results.locationLabel}
Source type: ${inputs.sourceType}
Annual H2: ${results.annualH2Kg.toFixed(0)} kg
Annual revenue: £${results.annualRevenue.toFixed(0)}
Payback: ${results.payback?.toFixed(1) || "N/A"} years
Curtailment: ${results.curtailmentPct.toFixed(0)}%
Electrolyser: ${inputs.elect.ratedKW} kW ${inputs.elect.type}

Give a JSON response with exactly these fields:
{
  "farmerSummary": "2-3 sentences in plain language a farmer would understand, mentioning what this means practically",
  "insights": ["specific insight 1 about this location/config", "specific insight 2"],
  "mainRisk": "single sentence about the biggest risk",
  "recommendation": "single most impactful improvement recommendation"
}
Only respond with valid JSON, no markdown.` }]
      }),
    });
    const d = await res.json();
    const text = d.content?.[0]?.text || "{}";
    return JSON.parse(text.replace(/```json|```/g,"").trim());
  } catch { return null; }
}

// ═══════════════════════════════════════════════════════════════════
// DESIGN TOKENS
// ═══════════════════════════════════════════════════════════════════
const C = {
  bg0:"#060910", bg1:"#0B0F18", bg2:"#101620", bg3:"#161E2C", bg4:"#1C2736",
  green:"#00E5A0", greenD:"#00A86B", greenGlow:"rgba(0,229,160,0.08)",
  amber:"#FFB800", red:"#FF4050",
  white:"#EEF3FF", grey:"#7C8799", greyL:"#A8B5C5",
  border:"rgba(255,255,255,0.065)",
};

// ── Atoms ─────────────────────────────────────────────────────────
const Lbl = ({children, hint, req}) => (
  <div style={{marginBottom:hint?3:8}}>
    <span style={{fontSize:13,fontWeight:600,color:C.white,display:"block"}}>
      {children}{req&&<span style={{color:C.green,marginLeft:4}}>*</span>}
    </span>
    {hint&&<div style={{fontSize:11,color:C.grey,marginTop:2,lineHeight:1.5}}>{hint}</div>}
  </div>
);

const Inp = ({value,onChange,placeholder,unit,type="number",error}) => (
  <div>
    <div style={{display:"flex",alignItems:"center",background:C.bg3,
      border:`1.5px solid ${error?C.red:C.border}`,borderRadius:8,overflow:"hidden"}}>
      <input type={type} value={value} onChange={e=>onChange(e.target.value)} placeholder={placeholder}
        style={{flex:1,background:"none",border:"none",outline:"none",padding:"11px 14px",
          fontSize:13,color:C.white,fontFamily:"'DM Mono',monospace",boxSizing:"border-box"}}/>
      {unit&&<span style={{padding:"0 12px",color:C.grey,fontSize:11,
        borderLeft:`1px solid ${C.border}`,whiteSpace:"nowrap"}}>{unit}</span>}
    </div>
    {error&&<div style={{fontSize:11,color:C.red,marginTop:3}}>{error}</div>}
  </div>
);

const Sel = ({value,onChange,options}) => (
  <select value={value} onChange={e=>onChange(e.target.value)}
    style={{width:"100%",background:C.bg3,border:`1.5px solid ${C.border}`,borderRadius:8,
      padding:"11px 14px",fontSize:13,color:C.white,outline:"none",cursor:"pointer",appearance:"none"}}>
    {options.map(o=><option key={o.value} value={o.value}>{o.label}</option>)}
  </select>
);

const G2 = ({children}) => <div style={{display:"grid",gridTemplateColumns:"1fr 1fr",gap:12}}>{children}</div>;
const G3 = ({children}) => <div style={{display:"grid",gridTemplateColumns:"1fr 1fr 1fr",gap:12}}>{children}</div>;

const Field = ({label,hint,req,children}) => (
  <div style={{marginBottom:16}}>
    <Lbl req={req} hint={hint}>{label}</Lbl>
    {children}
  </div>
);

const Sec = ({children}) => (
  <div style={{fontSize:9,fontWeight:700,letterSpacing:"2px",color:C.greenD,marginBottom:12,marginTop:4}}>{children}</div>
);

const Hr = () => <div style={{borderTop:`1px solid ${C.border}`,margin:"20px 0"}}/>;

const InfoBox = ({children, color=C.greenD}) => (
  <div style={{background:C.bg2,border:`1px solid ${C.border}`,borderRadius:10,padding:"14px 16px"}}>
    {children}
  </div>
);

const Toggle = ({checked,onChange,label,sub}) => (
  <div style={{display:"flex",alignItems:"center",justifyContent:"space-between",
    background:C.bg3,border:`1px solid ${C.border}`,borderRadius:10,padding:"14px 16px"}}>
    <div>
      <div style={{fontSize:13,fontWeight:600,color:C.white}}>{label}</div>
      {sub&&<div style={{fontSize:11,color:C.grey,marginTop:2}}>{sub}</div>}
    </div>
    <button onClick={()=>onChange(!checked)} style={{width:44,height:24,borderRadius:12,border:"none",
      background:checked?C.green:C.bg4,cursor:"pointer",position:"relative",transition:"background 0.2s",flexShrink:0,marginLeft:16}}>
      <div style={{position:"absolute",top:3,width:18,height:18,borderRadius:"50%",
        background:"#fff",transition:"left 0.2s",left:checked?23:3}}/>
    </button>
  </div>
);

// ═══════════════════════════════════════════════════════════════════
// STEP COMPONENTS
// ═══════════════════════════════════════════════════════════════════
function S0_Source({value,onChange}) {
  const opts = [
    {id:"solar",icon:"☀️",label:"Solar",sub:"PV panels"},
    {id:"wind",icon:"💨",label:"Wind",sub:"Turbines"},
    {id:"hybrid",icon:"⚡",label:"Solar + Wind",sub:"Combined"},
    {id:"hydro",icon:"💧",label:"Hydro / Other",sub:"Run-of-river"},
    {id:"lab",icon:"🔬",label:"Lab / PSU",sub:"Bench test"},
  ];
  return (
    <div>
      <h2 style={{fontSize:22,fontWeight:800,color:C.white,marginBottom:8}}>What is your energy source?</h2>
      <p style={{fontSize:13,color:C.grey,marginBottom:28}}>Determines which physics model and data we use for your site.</p>
      <div style={{display:"grid",gridTemplateColumns:"repeat(5,1fr)",gap:10}}>
        {opts.map(o=>(
          <button key={o.id} onClick={()=>onChange(o.id)} style={{
            padding:"18px 8px",border:`1.5px solid ${value===o.id?C.green:C.border}`,
            borderRadius:12,background:value===o.id?C.greenGlow:C.bg3,
            cursor:"pointer",outline:"none",textAlign:"center",transition:"all 0.2s"}}>
            <div style={{fontSize:26,marginBottom:6}}>{o.icon}</div>
            <div style={{fontSize:12,fontWeight:700,color:value===o.id?C.green:C.white}}>{o.label}</div>
            <div style={{fontSize:10,color:C.grey,marginTop:2}}>{o.sub}</div>
          </button>
        ))}
      </div>
    </div>
  );
}

function S1_Location({data,onChange}) {
  return (
    <div>
      <h2 style={{fontSize:22,fontWeight:800,color:C.white,marginBottom:8}}>Where is your site?</h2>
      <p style={{fontSize:13,color:C.grey,marginBottom:22}}>
        We have postcode-specific solar irradiance and wind speed data built in for the entire UK —
        no internet connection needed. A postcode gives accuracy within ~5%.
      </p>
      <Field label="UK Postcode" req hint="e.g. DH1 3RG · we have data for every UK postcode area">
        <Inp value={data.postcode||""} onChange={v=>onChange({...data,postcode:v})} placeholder="e.g. DH1 3RG" type="text"/>
      </Field>
      <InfoBox>
        <div style={{fontSize:11,fontWeight:700,color:C.greenD,marginBottom:8}}>📡 Why this matters</div>
        <div style={{fontSize:11,color:C.grey,lineHeight:1.9}}>
          <div>• <b style={{color:C.greyL}}>Solar:</b> Cornwall gets ~40% more irradiance than Edinburgh — flat UK averages hide this</div>
          <div>• <b style={{color:C.greyL}}>Wind:</b> Coastal/upland sites can have 3× the wind energy of sheltered inland valleys</div>
          <div>• <b style={{color:C.greyL}}>Temperature:</b> Panel output is derated monthly for your local temperature profile</div>
        </div>
      </InfoBox>
    </div>
  );
}

function S2_Solar({data,onChange}) {
  const kWp = ((Number(data.numPanels)||0)*(Number(data.panelWatt)||0)/1000).toFixed(2);
  return (
    <div>
      <h2 style={{fontSize:22,fontWeight:800,color:C.white,marginBottom:8}}>Solar Panel Configuration</h2>
      <p style={{fontSize:13,color:C.grey,marginBottom:22}}>Panel spec, count, tilt and orientation all feed into the physics model.</p>
      <Sec>PANEL SPEC</Sec>
      <G2>
        <Field label="Number of panels" req><Inp value={data.numPanels||""} onChange={v=>onChange({...data,numPanels:v})} placeholder="e.g. 20"/></Field>
        <Field label="Watts per panel" req><Inp value={data.panelWatt||""} onChange={v=>onChange({...data,panelWatt:v})} placeholder="e.g. 400" unit="W"/></Field>
      </G2>
      {Number(kWp)>0&&(
        <div style={{background:C.greenGlow,border:`1px solid rgba(0,229,160,0.2)`,borderRadius:8,
          padding:"10px 14px",marginBottom:16,fontSize:12,color:C.green,fontWeight:600}}>
          ⚡ Total: {kWp} kWp
        </div>
      )}
      <Field label="Panel type" hint={PANEL_META[data.panelType||"mono"]?.desc}>
        <Sel value={data.panelType||"mono"} onChange={v=>onChange({...data,panelType:v})}
          options={Object.entries(PANEL_META).map(([k,v])=>({value:k,label:v.label}))}/>
      </Field>
      <Hr/>
      <Sec>INSTALLATION GEOMETRY</Sec>
      <G2>
        <Field label="Tilt angle" hint="0°=flat · 35°=optimal UK · 90°=vertical">
          <Inp value={data.tilt||""} onChange={v=>onChange({...data,tilt:v})} placeholder="35" unit="°"/>
        </Field>
        <Field label="Orientation">
          <Sel value={String(data.orient||0)} onChange={v=>onChange({...data,orient:Number(v)})}
            options={[{value:"0",label:"South (optimal)"},{value:"-45",label:"South-East"},
              {value:"45",label:"South-West"},{value:"-90",label:"East"},{value:"90",label:"West"},{value:"180",label:"North"}]}/>
        </Field>
      </G2>
      <Hr/>
      <Sec>LOSSES</Sec>
      <G2>
        <Field label="System losses" hint="Wiring, soiling, shading, mismatch. Default 14%.">
          <Inp value={data.lossP||""} onChange={v=>onChange({...data,lossP:v})} placeholder="14" unit="%"/>
        </Field>
        <Field label="Inverter efficiency" hint="Modern string inverters ~97%">
          <Inp value={data.invEff||""} onChange={v=>onChange({...data,invEff:v})} placeholder="97" unit="%"/>
        </Field>
      </G2>
      <Toggle checked={data.secondLife!==false} onChange={v=>onChange({...data,secondLife:v})}
        label="Second-life solar panels"
        sub="Fluxero's Solarel partnership — saves ~55% on panel CAPEX (£360/kWp vs £800/kWp)"/>
    </div>
  );
}

function S3_Wind({data,onChange}) {
  const nT = Number(data.nTurbines)||1;
  const rotD = Number(data.rotorDiam)||0;
  const vR = Number(data.vRated)||12;
  const peak = rotD>0 ? (0.5*AIR_DENSITY*Math.PI*(rotD/2)**2*BETZ_CP*vR**3/1000).toFixed(1) : null;
  return (
    <div>
      <h2 style={{fontSize:22,fontWeight:800,color:C.white,marginBottom:8}}>Wind Turbine Configuration</h2>
      <p style={{fontSize:13,color:C.grey,marginBottom:22}}>
        IEC 61400 power curve applied to monthly wind speed data for your postcode.
        Weibull k=2 correction accounts for speed variability within each month.
      </p>
      <Sec>TURBINE SPEC</Sec>
      <G2>
        <Field label="Number of turbines" req><Inp value={data.nTurbines||""} onChange={v=>onChange({...data,nTurbines:v})} placeholder="1"/></Field>
        <Field label="Rated power" req hint="Nameplate kW at rated wind speed"><Inp value={data.pRatedKW||""} onChange={v=>onChange({...data,pRatedKW:v})} placeholder="e.g. 50" unit="kW"/></Field>
      </G2>
      <G2>
        <Field label="Rotor diameter"><Inp value={data.rotorDiam||""} onChange={v=>onChange({...data,rotorDiam:v})} placeholder="e.g. 18" unit="m"/></Field>
        <Field label="Hub height" hint="Scales wind speed via power law (h/10)^0.143"><Inp value={data.hubH||""} onChange={v=>onChange({...data,hubH:v})} placeholder="30" unit="m"/></Field>
      </G2>
      {peak&&<div style={{background:C.greenGlow,border:`1px solid rgba(0,229,160,0.2)`,borderRadius:8,
        padding:"10px 14px",marginBottom:16,fontSize:12,color:C.green,fontWeight:600}}>
        ⚡ Theoretical peak from rotor geometry: ~{peak} kW (Cp = 0.40)
      </div>}
      <Hr/>
      <Sec>POWER CURVE — IEC 61400</Sec>
      <G3>
        <Field label="Cut-in speed" hint="Typical 2.5–4 m/s"><Inp value={data.cutIn||""} onChange={v=>onChange({...data,cutIn:v})} placeholder="3" unit="m/s"/></Field>
        <Field label="Rated speed" hint="Typical 10–15 m/s"><Inp value={data.vRated||""} onChange={v=>onChange({...data,vRated:v})} placeholder="12" unit="m/s"/></Field>
        <Field label="Cut-out speed" hint="Typical 20–25 m/s"><Inp value={data.cutOut||""} onChange={v=>onChange({...data,cutOut:v})} placeholder="25" unit="m/s"/></Field>
      </G3>
      {nT>1&&<><Hr/><Sec>WAKE LOSSES</Sec>
        <Field label="Wake loss factor" hint="5–15% typical for farm layout">
          <Inp value={data.wakeLoss||""} onChange={v=>onChange({...data,wakeLoss:v})} placeholder="8" unit="%"/>
        </Field>
      </>}
    </div>
  );
}

function S4_HydroLab({sourceType,hydro,lab,onHydro,onLab}) {
  if (sourceType==="hydro") return (
    <div>
      <h2 style={{fontSize:22,fontWeight:800,color:C.white,marginBottom:8}}>Hydro Configuration</h2>
      <G2>
        <Field label="Installed capacity" req><Inp value={hydro.capacityKW||""} onChange={v=>onHydro({...hydro,capacityKW:v})} placeholder="e.g. 100" unit="kW"/></Field>
        <Field label="Capacity factor" hint="Run-of-river typical 40–60%"><Inp value={hydro.capFactor||""} onChange={v=>onHydro({...hydro,capFactor:v})} placeholder="45" unit="%"/></Field>
      </G2>
    </div>
  );
  return (
    <div>
      <h2 style={{fontSize:22,fontWeight:800,color:C.white,marginBottom:8}}>Lab / PSU Configuration</h2>
      <G2>
        <Field label="PSU output" req><Inp value={lab.powerKW||""} onChange={v=>onLab({...lab,powerKW:v})} placeholder="0.010" unit="kW"/></Field>
        <Field label="Daily run hours"><Inp value={lab.hoursPerDay||""} onChange={v=>onLab({...lab,hoursPerDay:v})} placeholder="8" unit="hrs"/></Field>
      </G2>
    </div>
  );
}

function S5_Electrolyser({data,onChange}) {
  return (
    <div>
      <h2 style={{fontSize:22,fontWeight:800,color:C.white,marginBottom:8}}>Electrolyser Configuration</h2>
      <p style={{fontSize:13,color:C.grey,marginBottom:22}}>
        We apply published efficiency curves — not a flat number.
        Alkaline minimum load (20%) and part-load degradation are both modelled.
      </p>
      <Sec>TYPE & RATING</Sec>
      <Field label="Type" hint={data.type==="pem"?"Faster response. Better for variable wind. Higher cost (£1,200/kW).":"Proven technology. Min load 20%. Ideal for solar/hydro. Fluxero standard (£700/kW)."}>
        <Sel value={data.type||"alkaline"} onChange={v=>onChange({...data,type:v})}
          options={[{value:"alkaline",label:"Alkaline (AEL) — Fluxero standard · £700/kW"},{value:"pem",label:"PEM — faster response · £1,200/kW"}]}/>
      </Field>
      <Field label="Rated power" req hint="Right-size to 70–80% of average input power for best efficiency.">
        <Inp value={data.ratedKW||""} onChange={v=>onChange({...data,ratedKW:v})} placeholder="e.g. 60" unit="kW"/>
      </Field>
      {(data.type||"alkaline")==="alkaline"&&(
        <div style={{background:"rgba(255,184,0,0.07)",border:`1px solid rgba(255,184,0,0.2)`,
          borderRadius:10,padding:"12px 16px",marginBottom:16}}>
          <div style={{fontSize:11,fontWeight:700,color:C.amber,marginBottom:6}}>⚠ Alkaline minimum load: 20%</div>
          <div style={{fontSize:11,color:C.grey,lineHeight:1.7}}>
            Power below {data.ratedKW?(Number(data.ratedKW)*0.2).toFixed(0):"—"} kW = shutdown.
            Curtailment is calculated and shown in results.
          </div>
        </div>
      )}
      <Hr/>
      <Sec>OPTIONAL REFINEMENTS</Sec>
      <G2>
        <Field label="Storage capacity" hint="Max H₂ your tanks hold per day. Leave blank = unlimited.">
          <Inp value={data.storageKgDay||""} onChange={v=>onChange({...data,storageKgDay:v})} placeholder="Leave blank" unit="kg/day"/>
        </Field>
        <Field label="Total CAPEX override" hint="Leave blank — we'll estimate from system size.">
          <Inp value={data.capexGBP||""} onChange={v=>onChange({...data,capexGBP:v})} placeholder="Leave blank" unit="£"/>
        </Field>
      </G2>
    </div>
  );
}

function S6_Revenue({data,onChange}) {
  const modes = [
    {id:"market",label:"Market Rate",sub:"£8.50/kg UK 2025"},
    {id:"contract",label:"Fixed Contract",sub:"Your agreed price"},
    {id:"onsite",label:"On-site Use",sub:"Diesel displacement"},
  ];
  return (
    <div>
      <h2 style={{fontSize:22,fontWeight:800,color:C.white,marginBottom:8}}>Revenue Model</h2>
      <p style={{fontSize:13,color:C.grey,marginBottom:22}}>How will you generate returns from the hydrogen you produce?</p>
      <div style={{display:"grid",gridTemplateColumns:"repeat(3,1fr)",gap:10,marginBottom:20}}>
        {modes.map(m=>(
          <button key={m.id} onClick={()=>onChange({...data,mode:m.id})} style={{
            padding:"14px 10px",border:`1.5px solid ${data.mode===m.id?C.green:C.border}`,
            borderRadius:10,background:data.mode===m.id?C.greenGlow:C.bg3,
            cursor:"pointer",outline:"none",textAlign:"center",transition:"all 0.2s"}}>
            <div style={{fontSize:12,fontWeight:700,color:data.mode===m.id?C.green:C.white,marginBottom:4}}>{m.label}</div>
            <div style={{fontSize:10,color:C.grey}}>{m.sub}</div>
          </button>
        ))}
      </div>
      {data.mode==="contract"&&<Field label="Your agreed H₂ price" hint="Per kg with your buyer">
        <Inp value={data.customPrice||""} onChange={v=>onChange({...data,customPrice:v})} placeholder="8.50" unit="£/kg"/>
      </Field>}
      {data.mode==="onsite"&&<Field label="Current diesel price">
        <Inp value={data.dieselPrice||""} onChange={v=>onChange({...data,dieselPrice:v})} placeholder="1.55" unit="£/litre"/>
      </Field>}
    </div>
  );
}

// ═══════════════════════════════════════════════════════════════════
// RESULTS
// ═══════════════════════════════════════════════════════════════════
function fmtH2(kg) {
  if (!kg && kg!==0) return "—";
  if (kg<0.01) return `${(kg*1000).toFixed(1)} g`;
  if (kg<1) return `${(kg*1000).toFixed(0)} g`;
  if (kg<1000) return `${kg.toFixed(1)} kg`;
  if (kg<1000000) return `${(kg/1000).toFixed(2)} t`;
  return `${(kg/1000).toFixed(0)} t`;
}
function fmtM(n) {
  if (!n&&n!==0) return "—";
  if (n>=1e6) return `£${(n/1e6).toFixed(2)}M`;
  if (n>=1000) return `£${(n/1000).toFixed(1)}k`;
  return `£${n.toFixed(0)}`;
}

const ChartTip = ({active,payload,label}) => {
  if (!active||!payload?.length) return null;
  return (
    <div style={{background:C.bg4,border:`1px solid ${C.border}`,borderRadius:8,padding:"10px 14px"}}>
      <div style={{fontSize:11,fontWeight:700,color:C.white,marginBottom:4}}>{label}</div>
      {payload.map(p=><div key={p.name} style={{fontSize:11,color:C.green}}>{p.name}: {fmtH2(p.value)}</div>)}
    </div>
  );
};

function ResultsScreen({results,inputs,onBack}) {
  const [tab,setTab] = useState("h2");
  const [copied,setCopied] = useState(false);
  const [showJSON,setShowJSON] = useState(false);

  const cc = results.confidence==="Detailed Projection"?C.green:results.confidence==="Good Estimate"?C.amber:C.grey;

  const chart = MONTHS.map((m,i)=>({month:m,
    H2:+results.h2Monthly[i].toFixed(2),
    MWh:+(results.monthlyKWh[i]/1000).toFixed(2),
  }));

  return (
    <div style={{display:"flex",height:"100vh",overflow:"hidden",background:C.bg0}}>
      {/* Sidebar */}
      <div style={{width:256,background:C.bg1,borderRight:`1px solid ${C.border}`,
        padding:"26px 22px",display:"flex",flexDirection:"column",flexShrink:0,overflowY:"auto"}}>
        <div style={{fontSize:13,fontWeight:800,letterSpacing:"3px",color:C.green,
          fontFamily:"'DM Mono',monospace",marginBottom:28}}>FLUXERO</div>
        <div style={{fontSize:18,fontWeight:800,color:C.white,marginBottom:6}}>Results</div>
        <div style={{display:"inline-flex",alignItems:"center",gap:6,padding:"4px 10px",
          background:`${cc}15`,border:`1px solid ${cc}40`,borderRadius:20,marginBottom:16,width:"fit-content"}}>
          <div style={{width:6,height:6,borderRadius:"50%",background:cc}}/>
          <span style={{fontSize:10,fontWeight:700,color:cc}}>{results.confidence}</span>
        </div>
        <div style={{fontSize:11,color:C.grey,marginBottom:20}}>📍 {results.locationLabel}</div>
        {[
          {label:"CO₂ AVOIDED",val:`${results.co2Saved.toFixed(1)} t/yr`,accent:C.green},
          {label:"CURTAILMENT",val:`${results.curtailmentPct.toFixed(0)}%`,accent:results.curtailmentPct>20?C.amber:C.green},
          {label:"EST. CAPEX",val:fmtM(results.capex)},
        ].map(d=>(
          <div key={d.label} style={{marginBottom:16}}>
            <div style={{fontSize:9,fontWeight:700,letterSpacing:"1.5px",color:C.greenD,marginBottom:3}}>{d.label}</div>
            <div style={{fontSize:14,fontWeight:700,color:d.accent||C.white,fontFamily:"'DM Mono',monospace"}}>{d.val}</div>
          </div>
        ))}
        {results.dataSources.length>0&&(
          <div style={{background:C.bg2,border:`1px solid ${C.border}`,borderRadius:10,padding:"12px 14px",marginBottom:16}}>
            <div style={{fontSize:9,fontWeight:700,color:C.greenD,letterSpacing:"1px",marginBottom:8}}>DATA SOURCES</div>
            {results.dataSources.map((d,i)=>(
              <div key={i} style={{fontSize:10,color:C.grey,marginBottom:3,lineHeight:1.5}}>• {d}</div>
            ))}
          </div>
        )}
        <div style={{flex:1}}/>
        <button onClick={onBack} style={{width:"100%",padding:"10px",background:C.bg3,
          border:`1px solid ${C.border}`,borderRadius:8,color:C.greyL,fontSize:12,
          fontWeight:600,cursor:"pointer",fontFamily:"inherit"}}>← Adjust inputs</button>
      </div>

      {/* Main */}
      <div style={{flex:1,overflowY:"auto",padding:"32px 40px"}}>
        <div style={{fontSize:20,fontWeight:800,color:C.white,marginBottom:4}}>
          {fmtH2(results.annualH2Kg)} of green hydrogen per year
        </div>
        <div style={{fontSize:12,color:C.grey,marginBottom:22}}>{inputs.sourceType} · {results.locationLabel}</div>

        {/* 4 headline cards */}
        <div style={{display:"grid",gridTemplateColumns:"repeat(4,1fr)",gap:12,marginBottom:16}}>
          {[
            {lbl:"H₂ PER DAY",  val:fmtH2(results.annualH2Kg/365),  ac:C.green,glow:true},
            {lbl:"H₂ PER YEAR", val:fmtH2(results.annualH2Kg),      ac:C.green},
            {lbl:"ANNUAL REVENUE",val:fmtM(results.annualRevenue),   ac:C.green,glow:true},
            {lbl:"PAYBACK",     val:results.payback?(results.payback<1?`${(results.payback*12).toFixed(0)}mo`:`${results.payback.toFixed(1)}yr`):"—", ac:C.amber},
          ].map(c=>(
            <div key={c.lbl} style={{background:c.glow?`linear-gradient(135deg,${C.bg3},rgba(0,229,160,0.07))`:C.bg3,
              border:`1px solid ${c.glow?"rgba(0,229,160,0.15)":C.border}`,borderRadius:12,padding:"18px 16px"}}>
              <div style={{fontSize:9,fontWeight:700,letterSpacing:"1.5px",color:C.grey,marginBottom:10}}>{c.lbl}</div>
              <div style={{fontSize:28,fontWeight:800,color:c.ac,fontFamily:"'DM Mono',monospace",lineHeight:1}}>{c.val}</div>
            </div>
          ))}
        </div>

        {/* Chart */}
        <div style={{display:"flex",gap:8,marginBottom:10}}>
          {[{id:"h2",lbl:"H₂ Output (kg)"},{id:"kwh",lbl:"Energy Input (MWh)"}].map(t=>(
            <button key={t.id} onClick={()=>setTab(t.id)} style={{
              padding:"6px 14px",border:`1px solid ${tab===t.id?C.green:C.border}`,
              borderRadius:6,background:tab===t.id?C.greenGlow:C.bg3,
              cursor:"pointer",color:tab===t.id?C.green:C.grey,fontSize:11,fontWeight:600,outline:"none",
            }}>{t.lbl}</button>
          ))}
        </div>
        <div style={{background:C.bg3,border:`1px solid ${C.border}`,borderRadius:12,
          padding:"16px 16px 8px",marginBottom:14}}>
          <ResponsiveContainer width="100%" height={160}>
            <BarChart data={chart} margin={{top:4,right:4,bottom:0,left:-16}}>
              <CartesianGrid strokeDasharray="3 3" stroke={C.border} vertical={false}/>
              <XAxis dataKey="month" tick={{fill:C.grey,fontSize:10}} axisLine={false} tickLine={false}/>
              <YAxis tick={{fill:C.grey,fontSize:10}} axisLine={false} tickLine={false}/>
              <Tooltip content={<ChartTip/>}/>
              <Bar dataKey={tab==="h2"?"H2":"MWh"} name={tab==="h2"?"H₂ (kg)":"Energy (MWh)"}
                fill={C.green} radius={[3,3,0,0]} maxBarSize={28} fillOpacity={0.85}/>
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Technical breakdown */}
        <div style={{background:C.bg3,border:`1px solid ${C.border}`,borderRadius:12,padding:"16px 20px",marginBottom:14}}>
          <Sec>TECHNICAL BREAKDOWN</Sec>
          {[
            ["Total energy input",`${(results.annualKWh/1000).toFixed(1)} MWh/yr`],
            ["H₂ price used",`£${results.h2Price.toFixed(2)}/kg`],
            ["Estimated CAPEX",fmtM(results.capex)],
            ["Curtailment loss",`${results.curtailmentPct.toFixed(1)}%`],
            ["CO₂ avoided",`${results.co2Saved.toFixed(1)} t/yr vs grey H₂`],
          ].map(([k,v])=>(
            <div key={k} style={{display:"flex",justifyContent:"space-between",
              padding:"9px 0",borderBottom:`1px solid ${C.border}`}}>
              <span style={{fontSize:12,color:C.grey}}>{k}</span>
              <span style={{fontSize:12,fontWeight:600,color:C.white,fontFamily:"'DM Mono',monospace"}}>{v}</span>
            </div>
          ))}
        </div>

        {/* AI insights (if loaded) */}
        {results.aiInsights&&(
          <div style={{background:`linear-gradient(135deg,${C.bg3},rgba(0,229,160,0.05))`,
            border:`1px solid rgba(0,229,160,0.15)`,borderRadius:12,padding:"18px 20px",marginBottom:14}}>
            <Sec>AI SITE ANALYSIS</Sec>
            {results.aiInsights.farmerSummary&&(
              <div style={{fontSize:12,color:C.greyL,lineHeight:1.8,marginBottom:14,
                padding:"12px 14px",background:C.bg2,borderRadius:8,fontStyle:"italic"}}>
                "{results.aiInsights.farmerSummary}"
              </div>
            )}
            {results.aiInsights.insights?.map((ins,i)=>(
              <div key={i} style={{display:"flex",gap:8,marginBottom:8}}>
                <span style={{color:C.green,flexShrink:0}}>→</span>
                <span style={{fontSize:11,color:C.greyL,lineHeight:1.6}}>{ins}</span>
              </div>
            ))}
            {results.aiInsights.mainRisk&&(
              <div style={{marginTop:10,padding:"10px 12px",background:"rgba(255,180,0,0.07)",
                border:`1px solid rgba(255,180,0,0.2)`,borderRadius:8}}>
                <span style={{fontSize:11,fontWeight:700,color:C.amber}}>⚠ Main risk: </span>
                <span style={{fontSize:11,color:C.grey}}>{results.aiInsights.mainRisk}</span>
              </div>
            )}
            {results.aiInsights.recommendation&&(
              <div style={{marginTop:8,padding:"10px 12px",background:C.greenGlow,
                border:`1px solid rgba(0,229,160,0.15)`,borderRadius:8}}>
                <span style={{fontSize:11,fontWeight:700,color:C.green}}>✓ Recommendation: </span>
                <span style={{fontSize:11,color:C.grey}}>{results.aiInsights.recommendation}</span>
              </div>
            )}
          </div>
        )}

        {/* Unity export */}
        <div style={{padding:"18px 22px",background:"rgba(0,229,160,0.04)",
          border:`1px solid rgba(0,229,160,0.14)`,borderRadius:12}}>
          <div style={{fontSize:13,fontWeight:700,color:C.green,marginBottom:6}}>
            🗺 Open in Fluxero Site Planner (Unity)
          </div>
          <div style={{fontSize:11,color:C.grey,lineHeight:1.7,marginBottom:14}}>
            Export this config to the Fluxero Unity app. It will map your {inputs.sourceType} installation
            on a 3D model of your land, pre-loaded with all your spec and monthly output data.
          </div>
          <div style={{display:"flex",gap:10}}>
            <button onClick={()=>{navigator.clipboard.writeText(JSON.stringify(results.unityConfig,null,2));setCopied(true);setTimeout(()=>setCopied(false),2500);}}
              style={{padding:"10px 18px",background:copied?C.greenD:C.green,border:"none",
                borderRadius:8,color:C.bg0,fontSize:12,fontWeight:700,cursor:"pointer",
                transition:"background 0.2s",fontFamily:"inherit"}}>
              {copied?"✓ Copied!":"Copy site config →"}
            </button>
            <button onClick={()=>setShowJSON(v=>!v)}
              style={{padding:"10px 18px",background:C.bg3,border:`1px solid ${C.border}`,
                borderRadius:8,color:C.greyL,fontSize:12,fontWeight:600,cursor:"pointer",fontFamily:"inherit"}}>
              {showJSON?"Hide JSON":"Preview JSON"}
            </button>
          </div>
          {showJSON&&(
            <pre style={{marginTop:12,background:C.bg0,borderRadius:8,padding:14,
              fontSize:10,color:C.green,overflow:"auto",maxHeight:220,lineHeight:1.6}}>
              {JSON.stringify(results.unityConfig,null,2)}
            </pre>
          )}
        </div>
      </div>
    </div>
  );
}

// ═══════════════════════════════════════════════════════════════════
// LOADING SCREEN
// ═══════════════════════════════════════════════════════════════════
function LoadingScreen({pct,msg}) {
  return (
    <div style={{display:"flex",flexDirection:"column",alignItems:"center",justifyContent:"center",
      height:"100vh",background:C.bg0,padding:40}}>
      <div style={{fontSize:13,fontWeight:800,letterSpacing:"3px",color:C.green,
        fontFamily:"'DM Mono',monospace",marginBottom:40}}>FLUXERO</div>
      <div style={{fontSize:18,fontWeight:700,color:C.white,marginBottom:8}}>Computing your site...</div>
      <div style={{fontSize:12,color:C.grey,marginBottom:28,textAlign:"center",minHeight:20}}>{msg}</div>
      <div style={{width:320,height:4,background:C.bg3,borderRadius:2,overflow:"hidden"}}>
        <div style={{height:"100%",width:`${pct}%`,background:C.green,borderRadius:2,transition:"width 0.4s ease"}}/>
      </div>
      <div style={{fontSize:11,color:C.grey,marginTop:10}}>{pct}%</div>
    </div>
  );
}

// ═══════════════════════════════════════════════════════════════════
// STEP PROGRESS BAR
// ═══════════════════════════════════════════════════════════════════
function StepBar({steps,cur}) {
  return (
    <div style={{display:"flex",alignItems:"center",marginBottom:32}}>
      {steps.map((s,i)=>(
        <div key={i} style={{display:"flex",alignItems:"center"}}>
          <div style={{display:"flex",alignItems:"center",gap:8}}>
            <div style={{width:26,height:26,borderRadius:"50%",display:"flex",alignItems:"center",
              justifyContent:"center",flexShrink:0,transition:"all 0.2s",
              background:i<cur?C.green:i===cur?C.bg4:C.bg3,
              border:`1.5px solid ${i<=cur?C.green:C.border}`,
              fontSize:11,fontWeight:700,color:i<cur?C.bg0:i===cur?C.green:C.grey,
            }}>{i<cur?"✓":i+1}</div>
            {i===cur&&<span style={{fontSize:11,color:C.white,fontWeight:600}}>{s}</span>}
          </div>
          {i<steps.length-1&&<div style={{width:20,height:1,background:C.border,margin:"0 6px",flexShrink:0}}/>}
        </div>
      ))}
    </div>
  );
}

// ═══════════════════════════════════════════════════════════════════
// ROOT APP
// ═══════════════════════════════════════════════════════════════════
export default function App() {
  const [stage,setStage]   = useState("steps");
  const [step,setStep]     = useState(0);
  const [pct,setPct]       = useState(0);
  const [msg,setMsg]       = useState("");
  const [results,setResults] = useState(null);
  const [errors,setErrors] = useState({});

  const [src,setSrc]     = useState("solar");
  const [loc,setLoc]     = useState({postcode:""});
  const [solar,setSolar] = useState({numPanels:"",panelWatt:"",panelType:"mono",tilt:"35",orient:0,lossP:"14",invEff:"97",secondLife:true});
  const [wind,setWind]   = useState({nTurbines:"1",pRatedKW:"",rotorDiam:"",hubH:"30",cutIn:"3",vRated:"12",cutOut:"25",wakeLoss:"8"});
  const [hydro,setHydro] = useState({capacityKW:"",capFactor:"45"});
  const [lab,setLab]     = useState({powerKW:"",hoursPerDay:"8"});
  const [elect,setElect] = useState({type:"alkaline",ratedKW:"",storageKgDay:"",capexGBP:""});
  const [rev,setRev]     = useState({mode:"market",customPrice:"8.50",dieselPrice:"1.55"});

  const getSteps = () => {
    const b = ["Source","Location"];
    if (src==="solar")  return [...b,"Solar","Electrolyser","Revenue"];
    if (src==="wind")   return [...b,"Wind","Electrolyser","Revenue"];
    if (src==="hybrid") return [...b,"Solar","Wind","Electrolyser","Revenue"];
    return [...b,"Source Config","Electrolyser","Revenue"];
  };

  const getContent = () => {
    const base = [
      <S0_Source value={src} onChange={v=>{setSrc(v);setStep(0);}}/>,
      <S1_Location data={loc} onChange={setLoc}/>,
    ];
    if (src==="solar")  return [...base,<S2_Solar data={solar} onChange={setSolar}/>,<S5_Electrolyser data={elect} onChange={setElect}/>,<S6_Revenue data={rev} onChange={setRev}/>];
    if (src==="wind")   return [...base,<S3_Wind data={wind} onChange={setWind}/>,<S5_Electrolyser data={elect} onChange={setElect}/>,<S6_Revenue data={rev} onChange={setRev}/>];
    if (src==="hybrid") return [...base,<S2_Solar data={solar} onChange={setSolar}/>,<S3_Wind data={wind} onChange={setWind}/>,<S5_Electrolyser data={elect} onChange={setElect}/>,<S6_Revenue data={rev} onChange={setRev}/>];
    return [...base,<S4_HydroLab sourceType={src} hydro={hydro} lab={lab} onHydro={setHydro} onLab={setLab}/>,<S5_Electrolyser data={elect} onChange={setElect}/>,<S6_Revenue data={rev} onChange={setRev}/>];
  };

  const steps   = getSteps();
  const content = getContent();
  const isLast  = step === content.length - 1;

  function validate() {
    const e = {};
    if (step===1 && !loc.postcode) e.loc="Enter a UK postcode";
    if (src==="solar"&&step===2&&(!solar.numPanels||!solar.panelWatt)) e.solar="Enter panel count and wattage";
    if (src==="wind"&&step===2&&!wind.pRatedKW) e.wind="Enter rated power";
    if ((src==="hybrid")&&step===2&&(!solar.numPanels||!solar.panelWatt)) e.solar="Enter solar panel details";
    setErrors(e);
    return !Object.keys(e).length;
  }

  async function doCalc() {
    if (!validate()) return;
    setStage("loading"); setPct(5); setMsg("Setting up calculation...");

    const inputs = { sourceType:src, postcode:loc.postcode, solar, wind, hydro, lab, elect, revenue:rev };

    setPct(20); setMsg("Looking up postcode-specific solar data...");
    await new Promise(r=>setTimeout(r,300));
    setPct(45); setMsg("Applying wind speed data and power curves...");
    await new Promise(r=>setTimeout(r,300));
    setPct(65); setMsg("Running electrolyser efficiency model...");
    await new Promise(r=>setTimeout(r,200));

    const r = runCalc(inputs);
    setPct(80); setMsg("Getting AI site analysis...");

    const ai = await getAIInsights(inputs, r);
    r.aiInsights = ai;

    setPct(100); setMsg("Done");
    await new Promise(x=>setTimeout(x,400));
    setResults(r);
    setStage("results");
  }

  if (stage==="loading") return <LoadingScreen pct={pct} msg={msg}/>;
  if (stage==="results") return <ResultsScreen results={results} inputs={{sourceType:src,postcode:loc.postcode}} onBack={()=>{setStage("steps");setStep(0);}}/>;

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Sora:wght@400;600;700;800&family=DM+Mono:wght@400;500;600&display=swap');
        *{box-sizing:border-box;margin:0;padding:0} body{font-family:'Sora',sans-serif}
        input::placeholder{color:#252D3D}
        input[type=number]::-webkit-inner-spin-button{-webkit-appearance:none}
        select option{background:#161E2C}
        ::-webkit-scrollbar{width:4px}
        ::-webkit-scrollbar-thumb{background:#1C2636;border-radius:2px}
      `}</style>

      <div style={{display:"flex",minHeight:"100vh",background:C.bg0}}>
        {/* Sidebar */}
        <div style={{width:236,background:C.bg1,borderRight:`1px solid ${C.border}`,
          padding:"32px 22px",display:"flex",flexDirection:"column",
          position:"sticky",top:0,height:"100vh",flexShrink:0}}>
          <div style={{fontSize:13,fontWeight:800,letterSpacing:"3px",color:C.green,
            fontFamily:"'DM Mono',monospace",marginBottom:6}}>FLUXERO</div>
          <div style={{fontSize:10,color:C.grey,marginBottom:32}}>Small Modular Hydrogen Plants</div>
          <div style={{fontSize:14,fontWeight:700,color:C.white,marginBottom:8}}>H₂ Site Calculator</div>
          <div style={{fontSize:11,color:C.grey,lineHeight:1.8,marginBottom:28}}>
            Postcode-specific UK data. Real electrolyser efficiency curves. No internet needed.
          </div>
          {["Postcode solar irradiance","NOABL wind speed data","IEC 61400 power curves","Alkaline/PEM eff. curves","Temperature derating","Monthly H₂ output","AI site analysis","Unity site planner export"].map(f=>(
            <div key={f} style={{display:"flex",gap:8,alignItems:"flex-start",marginBottom:7}}>
              <div style={{width:5,height:5,borderRadius:"50%",background:C.green,marginTop:4,flexShrink:0}}/>
              <span style={{fontSize:11,color:C.greyL,lineHeight:1.4}}>{f}</span>
            </div>
          ))}
          <div style={{flex:1}}/>
          <div style={{fontSize:10,color:C.grey,marginBottom:6}}>Step {step+1} of {content.length}</div>
          <div style={{height:3,background:C.bg3,borderRadius:2}}>
            <div style={{height:"100%",borderRadius:2,background:C.green,
              width:`${((step+1)/content.length)*100}%`,transition:"width 0.3s"}}/>
          </div>
        </div>

        {/* Form */}
        <div style={{flex:1,overflowY:"auto",padding:"40px 52px",maxWidth:780}}>
          <StepBar steps={steps} cur={step}/>
          <div style={{minHeight:400}}>{content[step]}</div>
          {Object.keys(errors).length>0&&(
            <div style={{background:"rgba(255,64,80,0.08)",border:`1px solid rgba(255,64,80,0.2)`,
              borderRadius:8,padding:"10px 14px",marginTop:16,fontSize:12,color:C.red}}>
              {Object.values(errors).join(" · ")}
            </div>
          )}
          <div style={{display:"flex",justifyContent:"space-between",marginTop:28,paddingTop:20,
            borderTop:`1px solid ${C.border}`}}>
            <button onClick={()=>step>0&&setStep(s=>s-1)} style={{
              padding:"12px 24px",background:step===0?"transparent":C.bg3,
              border:`1px solid ${step===0?"transparent":C.border}`,
              borderRadius:8,color:step===0?"transparent":C.greyL,
              fontSize:13,fontWeight:600,cursor:step===0?"default":"pointer",fontFamily:"inherit",
            }}>← Back</button>
            <button onClick={()=>{if(!validate())return;isLast?doCalc():setStep(s=>s+1);}} style={{
              padding:"12px 32px",background:isLast?C.green:C.bg4,
              border:`1.5px solid ${isLast?C.green:C.border}`,
              borderRadius:8,color:isLast?C.bg0:C.white,fontSize:13,fontWeight:700,
              cursor:"pointer",fontFamily:"inherit",
              boxShadow:isLast?`0 0 24px rgba(0,229,160,0.25)`:"none",
            }}>{isLast?"Calculate →":"Continue →"}</button>
          </div>
        </div>
      </div>
    </>
  );
}
