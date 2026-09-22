"use strict";

const SPEED_LIMIT_KPH = 80;
const MAX_FEED_ITEMS = 60;
const THROUGHPUT_SAMPLES = 40;
const QUEUE_CAPACITY = 10000;

const COLORS = {
  moving: "#2dd4a7",
  idle: "#5b6779",
  speeding: "#ff5c63",
  info: "#4d9fff",
  warning: "#f2b13c",
  critical: "#ff5c63",
  grid: "#1f2836",
  axis: "#5b6779",
};

const ALERT_COLORS = {
  Speeding: "#ff5c63",
  HarshBraking: "#f2b13c",
  HarshAcceleration: "#f5866b",
  GeofenceEntry: "#4d9fff",
  GeofenceExit: "#8b7bff",
  ExcessiveIdle: "#5b6779",
};

const el = (id) => document.getElementById(id);

const dom = {
  devices: el("kpi-devices"),
  rate: el("kpi-rate"),
  readings: el("kpi-readings"),
  alerts: el("kpi-alerts"),
  queue: el("kpi-queue"),
  queueFill: el("queue-fill"),
  status: el("connection-status"),
  statusText: el("connection-text"),
  feed: el("alert-feed"),
  feedCount: el("feed-count"),
  alertTotal: el("alert-total"),
};

/* ------------------------------------------------------------------ map --- */

const map = L.map("map", { zoomControl: true, attributionControl: true }).setView(
  [43.68, -79.48],
  11
);

L.tileLayer("https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png", {
  attribution: '&copy; OpenStreetMap &copy; CARTO',
  subdomains: "abcd",
  maxZoom: 19,
}).addTo(map);

const markers = new Map();
const latestByDevice = new Map();

function vehicleColor(reading) {
  if (reading.speedKph < 1) return COLORS.idle;
  if (reading.speedKph > SPEED_LIMIT_KPH) return COLORS.speeding;
  return COLORS.moving;
}

function vehicleIcon(reading) {
  const color = vehicleColor(reading);
  const heading = reading.speedKph < 1 ? 0 : reading.headingDegrees || 0;
  const shape =
    reading.speedKph < 1
      ? `<circle cx="8" cy="8" r="4.5" fill="${color}" stroke="#080b12" stroke-width="1.5"/>`
      : `<path d="M8 1.5 L13 14 L8 11 L3 14 Z" fill="${color}" stroke="#080b12" stroke-width="1.2" stroke-linejoin="round"/>`;

  return L.divIcon({
    className: "vehicle-marker",
    iconSize: [16, 16],
    iconAnchor: [8, 8],
    html:
      `<div class="vehicle-glyph" style="transform:rotate(${heading}deg)">` +
      `<svg width="16" height="16" viewBox="0 0 16 16">${shape}</svg></div>`,
  });
}

function popupHtml(reading) {
  const name = `Vehicle ${String(reading.deviceId).padStart(3, "0")}`;
  const state = reading.speedKph < 1 ? "Idle" : "Moving";
  return `
    <div class="vehicle-popup">
      <h4>${name}</h4>
      <dl>
        <dt>Status</dt><dd>${state}</dd>
        <dt>Speed</dt><dd>${reading.speedKph.toFixed(0)} km/h</dd>
        <dt>Heading</dt><dd>${Math.round(reading.headingDegrees)}&deg;</dd>
        <dt>Accel</dt><dd>${reading.accelerationG.toFixed(2)} g</dd>
        <dt>RPM</dt><dd>${Math.round(reading.engineRpm)}</dd>
      </dl>
    </div>`;
}

function applyReading(reading) {
  latestByDevice.set(reading.deviceId, reading);

  const position = [reading.latitude, reading.longitude];
  let marker = markers.get(reading.deviceId);

  if (!marker) {
    marker = L.marker(position, { icon: vehicleIcon(reading) }).addTo(map);
    marker.bindTooltip(`Vehicle ${String(reading.deviceId).padStart(3, "0")}`, {
      direction: "top",
      offset: [0, -10],
    });
    marker.bindPopup(popupHtml(reading));
    markers.set(reading.deviceId, marker);
    return;
  }

  marker.setLatLng(position);
  marker.setIcon(vehicleIcon(reading));

  if (marker.isPopupOpen()) {
    marker.setPopupContent(popupHtml(reading));
  }
}

async function drawGeofences() {
  try {
    const response = await fetch("/api/geofences");
    if (!response.ok) return;

    for (const zone of await response.json()) {
      L.circle([zone.centerLatitude, zone.centerLongitude], {
        radius: zone.radiusMeters,
        color: COLORS.info,
        weight: 1.5,
        opacity: 0.65,
        fillColor: COLORS.info,
        fillOpacity: 0.06,
        interactive: false,
      })
        .addTo(map)
        .bindTooltip(zone.name, { permanent: false, direction: "center" });
    }
  } catch {
    /* map still works without zones drawn */
  }
}

/* --------------------------------------------------------------- charts --- */

const throughputChart = new Chart(el("throughputChart"), {
  type: "line",
  data: {
    labels: Array(THROUGHPUT_SAMPLES).fill(""),
    datasets: [
      {
        data: Array(THROUGHPUT_SAMPLES).fill(0),
        borderColor: COLORS.moving,
        borderWidth: 2,
        pointRadius: 0,
        tension: 0.35,
        fill: true,
        backgroundColor: (ctx) => {
          const { chart } = ctx;
          if (!chart.chartArea) return "rgba(45,212,167,0.12)";
          const g = chart.ctx.createLinearGradient(0, chart.chartArea.top, 0, chart.chartArea.bottom);
          g.addColorStop(0, "rgba(45,212,167,0.35)");
          g.addColorStop(1, "rgba(45,212,167,0)");
          return g;
        },
      },
    ],
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    plugins: { legend: { display: false }, tooltip: { enabled: false } },
    scales: {
      x: { display: false },
      y: {
        beginAtZero: true,
        grid: { color: COLORS.grid, drawTicks: false },
        border: { display: false },
        ticks: { color: COLORS.axis, font: { size: 10 }, maxTicksLimit: 4, padding: 6 },
      },
    },
  },
});

const alertChart = new Chart(el("alertChart"), {
  type: "doughnut",
  data: {
    labels: [],
    datasets: [{ data: [], backgroundColor: [], borderColor: "#0e131d", borderWidth: 2 }],
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    cutout: "62%",
    plugins: {
      legend: {
        position: "right",
        labels: {
          color: "#8794a8",
          boxWidth: 9,
          boxHeight: 9,
          usePointStyle: true,
          pointStyle: "circle",
          font: { size: 10.5 },
          padding: 9,
        },
      },
    },
  },
});

const alertCounts = new Map();

function recordAlertType(type) {
  alertCounts.set(type, (alertCounts.get(type) ?? 0) + 1);

  const labels = [...alertCounts.keys()];
  alertChart.data.labels = labels.map((l) => l.replace(/([a-z])([A-Z])/g, "$1 $2"));
  alertChart.data.datasets[0].data = labels.map((l) => alertCounts.get(l));
  alertChart.data.datasets[0].backgroundColor = labels.map((l) => ALERT_COLORS[l] ?? COLORS.info);
  alertChart.update("none");
}

function pushThroughput(value) {
  const series = throughputChart.data.datasets[0].data;
  series.push(value);
  if (series.length > THROUGHPUT_SAMPLES) series.shift();
  throughputChart.update("none");
}

/* ----------------------------------------------------------- alert feed --- */

let feedTotal = 0;

function renderAlert(alert) {
  if (feedTotal === 0) dom.feed.innerHTML = "";
  feedTotal++;

  const item = document.createElement("li");
  item.className = `alert-item sev-${alert.severity.toLowerCase()}`;

  const body = document.createElement("div");
  body.className = "alert-body";

  const meta = document.createElement("div");
  meta.className = "alert-meta";

  const type = document.createElement("span");
  type.className = "alert-type";
  type.textContent = alert.type.replace(/([a-z])([A-Z])/g, "$1 $2");

  const time = document.createElement("span");
  time.className = "alert-time";
  time.textContent = new Date(alert.timestampUtc).toLocaleTimeString([], { hour12: false });

  const message = document.createElement("div");
  message.className = "alert-message";
  message.textContent = alert.message;

  meta.append(type, time);
  body.append(meta, message);
  item.append(body);

  dom.feed.prepend(item);

  while (dom.feed.children.length > MAX_FEED_ITEMS) {
    dom.feed.removeChild(dom.feed.lastChild);
  }

  dom.feedCount.textContent = feedTotal;
  recordAlertType(alert.type);
  dom.alertTotal.textContent = `${feedTotal} total`;
}

/* ---------------------------------------------------------------- stats --- */

const compact = new Intl.NumberFormat("en", { notation: "compact", maximumFractionDigits: 1 });

function applyStats(stats) {
  dom.devices.textContent = stats.activeDevices;
  dom.rate.textContent = stats.readingsPerSecond.toFixed(0);
  dom.readings.textContent = compact.format(stats.totalReadings);
  dom.alerts.textContent = compact.format(stats.totalAlerts);
  dom.queue.textContent = stats.queueDepth;

  const saturation = Math.min(100, (stats.queueDepth / QUEUE_CAPACITY) * 100);
  dom.queueFill.style.width = `${Math.max(saturation, stats.queueDepth > 0 ? 2 : 0)}%`;
  dom.queueFill.style.background =
    saturation > 60 ? COLORS.critical : saturation > 20 ? COLORS.warning : COLORS.moving;

  pushThroughput(stats.readingsPerSecond);
}

function setConnection(state) {
  const labels = { connected: "Live", connecting: "Connecting", down: "Disconnected" };
  dom.statusText.textContent = labels[state];
  dom.status.className = `status ${
    state === "connected" ? "is-connected" : state === "down" ? "is-down" : ""
  }`;
}

/* -------------------------------------------------------------- signalr --- */

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/telemetry")
  .withAutomaticReconnect()
  .build();

connection.on("telemetry", (readings) => {
  for (const reading of readings) applyReading(reading);
});

connection.on("alerts", (alerts) => {
  for (const alert of alerts) renderAlert(alert);
});

connection.on("stats", applyStats);

connection.onreconnecting(() => setConnection("connecting"));
connection.onreconnected(() => setConnection("connected"));
connection.onclose(() => setConnection("down"));

async function start() {
  setConnection("connecting");
  await drawGeofences();

  try {
    await connection.start();
    setConnection("connected");
  } catch (error) {
    console.error("SignalR connection failed", error);
    setConnection("down");
    setTimeout(start, 4000);
  }
}

start();
