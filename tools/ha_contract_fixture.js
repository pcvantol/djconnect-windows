#!/usr/bin/env node
"use strict";

const crypto = require("crypto");
const http = require("http");

const DEVICE_CREDENTIAL = "local-device-credential";
const WS_SESSION = "local-websocket-session";
const DEVICE_ID = "djconnect-windows-contract";
const DEVICE_NAME = "CI Windows PC";
const CLIENT_TYPE = "windows";

const routes = [
  "djconnect/capabilities",
  "djconnect/command",
  "djconnect/ask_dj/message",
  "djconnect/ask_dj/history",
  "djconnect/ask_dj/history/clear",
  "djconnect/ask_dj/history/state",
  "djconnect/music_dna/profile",
  "djconnect/music_dna/settings",
  "djconnect/music_dna/clear",
  "djconnect/music_discovery/feed",
  "djconnect/music_discovery/refresh",
  "djconnect/music_discovery/play",
  "djconnect/music_discovery/feedback",
  "djconnect/track_insight",
];

const contractFixture = {
  clientType: CLIENT_TYPE,
  deviceCredential: DEVICE_CREDENTIAL,
  deviceID: DEVICE_ID,
  deviceName: DEVICE_NAME,
  routes,
  webSocketSession: WS_SESSION,
};

function fail(message) {
  throw new Error(message);
}

function assert(condition, message) {
  if (!condition) fail(message);
}

function safeLog(message) {
  console.log(String(message).replaceAll(DEVICE_CREDENTIAL, "<redacted>").replaceAll(WS_SESSION, "<redacted>"));
}

function readRequestBody(request) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    request.on("data", (chunk) => chunks.push(chunk));
    request.on("end", () => resolve(Buffer.concat(chunks)));
    request.on("error", reject);
  });
}

function parseJSON(buffer) {
  if (buffer.length === 0) return {};
  return JSON.parse(buffer.toString("utf8"));
}

function jsonResponse(response, statusCode, body) {
  const data = Buffer.from(JSON.stringify(body));
  response.writeHead(statusCode, {
    "content-type": "application/json",
    "content-length": data.length,
  });
  response.end(data);
}

function assertCredential(request) {
  assert(request.headers.authorization === `Bearer ${DEVICE_CREDENTIAL}`, `${request.method} ${request.url} missing bearer credential`);
}

function bodyIdentity(body) {
  return body.identity || body.payload?.identity || body;
}

function assertWindowsIdentity(body, route) {
  const identity = bodyIdentity(body);
  assert(identity.device_id === DEVICE_ID || body.device_id === DEVICE_ID, `${route} missing device_id`);
  assert(identity.client_id === DEVICE_ID || body.client_id === DEVICE_ID, `${route} missing client_id`);
  assert(identity.device_name === DEVICE_NAME || body.device_name === DEVICE_NAME, `${route} missing device_name`);
  assert(identity.client_type === CLIENT_TYPE || body.client_type === CLIENT_TYPE, `${route} missing client_type=windows`);
}

function announcementCapabilities() {
  return {
    speaker_configured: true,
    speaker_entity_id: "media_player.contract_voice",
    speaker_name: "Contract Voice",
    supported_outputs: ["client_device", "both", "ha_speaker", "text_only"],
    locked_outputs: [],
    default_output: "both",
    output: "both",
  };
}

function playback() {
  return {
    has_playback: true,
    is_playing: true,
    title: "Contract Track",
    artist: "Contract Artist",
    album: "Contract Album",
    track_name: "Contract Track",
    artist_name: "Contract Artist",
    volume_percent: 42,
  };
}

function askDJMessageResponse(extra = {}) {
  return {
    success: true,
    text: "Contract answer",
    dj_text: "Contract answer",
    history_revision: 2,
    clear_revision: 0,
    assistant_message: {
      id: "assistant-contract",
      role: "assistant",
      text: "Contract answer",
      origin: "message",
      announcement: {
        output: "both",
        delivery: "both",
        audio_response_effective: "always",
        audio_url: "/api/djconnect/v1/tts/contract.mp3",
        audio_type: "mp3",
        target: { kind: "ha_media_player", entity_id: "media_player.contract_voice", name: "Contract Voice" },
        warnings: [],
      },
    },
    messages: [
      { id: "user-contract", role: "user", text: "Contract request", client_message_id: "contract-client-message", exchange_id: "contract-exchange", exchange_order: 0, history_revision: 1 },
      { id: "assistant-contract", role: "assistant", text: "Contract answer", client_message_id: "contract-client-message", exchange_id: "contract-exchange", exchange_order: 1, history_revision: 2 },
    ],
    sources: [],
    links: [],
    dj_announcement: announcementCapabilities(),
    ...extra,
  };
}

function historyResponse(extra = {}) {
  return {
    success: true,
    history_revision: 2,
    clear_revision: 0,
    cleared: false,
    ask_dj_clear_required: false,
    history_limit: 1000,
    history_trimmed_before: null,
    history_trimmed_count: 0,
    messages: [],
    ...extra,
  };
}

function musicDNAResponse(extra = {}) {
  return {
    success: true,
    enabled: true,
    generation: 7,
    profile: {
      summary: "Contract Music DNA",
      favorite_genres: [{ name: "house", count: 12 }],
      recent_tracks: [{ title: "Contract Track", artist: "Contract Artist" }],
    },
    ...extra,
  };
}

function discoveryFeed(extra = {}) {
  return {
    success: true,
    enabled: true,
    revision: 3,
    sections: [
      {
        id: "new_for_you",
        title: "New for you",
        items: [
          {
            id: "disco-1",
            kind: "track",
            title: "Contract Track",
            subtitle: "Contract Artist",
            uri: "spotify:track:contract",
            reason: "Contract reason",
            reason_sources: ["music_dna"],
            confidence: "high",
          },
        ],
      },
    ],
    ...extra,
  };
}

function trackInsightResponse() {
  return {
    success: true,
    track_insight: {
      contract_version: 2,
      track: { title: "Contract Track", artist: "Contract Artist", album: "Contract Album" },
      analysis: {
        summary: "Contract insight",
        sections: [{ title: "Energy", text: "Steady lift" }],
        dj_tips: [{ title: "Mix", text: "Bring it in over 16 bars." }],
      },
    },
  };
}

function capabilitiesResponse(id) {
  return {
    id,
    type: "result",
    success: true,
    result: {
      success: true,
      websocket_supported: true,
      transports: { websocket: true },
      commands: routes,
      features: {
        ask_dj: true,
        music_dna: true,
        music_discovery: true,
        music_discovery_feedback: true,
        track_insight: true,
      },
    },
  };
}

function routeResult(message) {
  const type = message.type;
  if (type !== "djconnect/capabilities") {
    assertWindowsIdentity(message, type);
    assert(message.device_token === DEVICE_CREDENTIAL, `${type} missing DJConnect device credential`);
  }

  if (type === "djconnect/capabilities") return capabilitiesResponse(message.id);
  if (type === "djconnect/command") return result(message.id, { success: true, playback: playback(), dj_announcement: announcementCapabilities() });
  if (type === "djconnect/ask_dj/message") return result(message.id, askDJMessageResponse());
  if (type === "djconnect/ask_dj/history") return result(message.id, historyResponse());
  if (type === "djconnect/ask_dj/history/clear") return result(message.id, historyResponse({ clear_revision: 1, cleared: true }));
  if (type === "djconnect/ask_dj/history/state") return result(message.id, historyResponse({ ask_dj_clear_required: true, clear_revision: 1 }));
  if (type === "djconnect/music_dna/profile") return result(message.id, musicDNAResponse());
  if (type === "djconnect/music_dna/settings") return result(message.id, musicDNAResponse({ generation: 8, enabled: message.enabled === true }));
  if (type === "djconnect/music_dna/clear") return result(message.id, musicDNAResponse({ generation: 9, profile: {} }));
  if (type === "djconnect/music_discovery/feed") return result(message.id, discoveryFeed());
  if (type === "djconnect/music_discovery/refresh") return result(message.id, discoveryFeed({ revision: 4 }));
  if (type === "djconnect/music_discovery/play") return result(message.id, { success: true, accepted: true });
  if (type === "djconnect/music_discovery/feedback") return result(message.id, { success: true, feedback_recorded: true });
  if (type === "djconnect/track_insight") return result(message.id, trackInsightResponse());
  return { id: message.id, type: "result", success: false, error: { code: "unsupported", message: "Unsupported contract route" } };
}

function result(id, body) {
  return { id, type: "result", success: true, result: body };
}

async function handleHTTP(request, response, observed) {
  const url = new URL(request.url, "http://127.0.0.1");
  const path = url.pathname;

  if (request.method === "POST" && path === "/api/djconnect/v1/pair") {
    const body = parseJSON(await readRequestBody(request));
    assert(body.device_id === DEVICE_ID, "pair missing device_id");
    assert(body.device_name === DEVICE_NAME, "pair missing device_name");
    assert(body.client_type === CLIENT_TYPE, "pair missing client_type=windows");
    assert(body.pair_code === "123456", "pair missing expected pair_code");
    assert(body.pairing_token === "123456", "pair missing compatibility pairing_token");
    assert(body.pairing_code === "123456", "pair missing compatibility pairing_code");
    observed.httpRequests.push({ method: request.method, path });
    jsonResponse(response, 200, {
      success: true,
      client_type: CLIENT_TYPE,
      device_id: DEVICE_ID,
      device_token: DEVICE_CREDENTIAL,
      ha_pairing_status: "paired",
      api_base: "/api/djconnect/v1",
      voice_path: "/api/djconnect/v1/voice",
      status_path: "/api/djconnect/v1/status",
      event_path: "/api/djconnect/v1/event",
      ha_local_url: "http://127.0.0.1",
      remote_supported: false,
      ask_dj_supported: true,
      ask_dj_voice_supported: true,
      ask_dj_audio_response_supported: true,
      music_backend: "spotify_direct",
      music_backend_name: "Spotify Direct",
      music_backend_available: true,
      dj_announcement: announcementCapabilities(),
    });
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/websocket/session") {
    assertCredential(request);
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    assert(Array.isArray(body.requested_commands), "session missing requested_commands");
    assert(body.access_token === undefined && body.home_assistant_token === undefined, "session request leaked HA credential");
    observed.sessionCalls += 1;
    jsonResponse(response, 200, {
      success: true,
      access_token: WS_SESSION,
      expires_at: new Date(Date.now() + 10 * 60 * 1000).toISOString(),
      commands: routes,
    });
    return true;
  }

  if (!path.startsWith("/api/djconnect/v1/")) return false;
  assertCredential(request);
  observed.httpRequests.push({ method: request.method, path });

  if (request.method === "POST" && path === "/api/djconnect/v1/status") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    jsonResponse(response, 200, {
      success: true,
      backend_available: true,
      playback: playback(),
      outputs: [{ id: "contract-output", name: "Contract Output", is_active: true }],
      music_backend: "spotify_direct",
      music_backend_name: "Spotify Direct",
      music_backend_available: true,
      dj_announcement: announcementCapabilities(),
    });
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/command") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    assert(typeof body.command === "string" && body.command.length > 0, "command missing command name");
    assert(body.dj_announcement_speaker_entity_id === undefined, "Windows must not send HA speaker entity");
    jsonResponse(response, 200, { success: true, playback: playback(), dj_announcement: announcementCapabilities() });
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/event") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    jsonResponse(response, 200, { success: true });
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/ask_dj/message") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    assert(typeof body.text === "string" && body.text.length > 0, "ask_dj/message missing text");
    jsonResponse(response, 200, askDJMessageResponse());
    return true;
  }

  if (request.method === "GET" && path === "/api/djconnect/v1/ask_dj/history") {
    jsonResponse(response, 200, historyResponse());
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/ask_dj/history/clear") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    jsonResponse(response, 200, historyResponse({ clear_revision: 1, cleared: true }));
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/music_dna/profile") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    jsonResponse(response, 200, musicDNAResponse());
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/music_dna/settings") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    jsonResponse(response, 200, musicDNAResponse({ generation: 8, enabled: body.enabled === true }));
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/music_dna/clear") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    jsonResponse(response, 200, musicDNAResponse({ generation: 9, profile: {} }));
    return true;
  }

  if (request.method === "GET" && path === "/api/djconnect/v1/music_discovery") {
    jsonResponse(response, 200, discoveryFeed());
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/music_discovery/refresh") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    jsonResponse(response, 200, discoveryFeed({ revision: 4 }));
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/music_discovery/play") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    jsonResponse(response, 200, { success: true, accepted: true });
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/music_discovery/feedback") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    jsonResponse(response, 200, { success: true, feedback_recorded: true });
    return true;
  }

  if (request.method === "POST" && path === "/api/djconnect/v1/track_insight") {
    const body = parseJSON(await readRequestBody(request));
    assertWindowsIdentity(body, path);
    jsonResponse(response, 200, trackInsightResponse());
    return true;
  }

  if (request.method === "GET" && path === "/api/djconnect/v1/vibecast") {
    jsonResponse(response, 200, {
      success: true,
      enabled: true,
      items: [{ id: "contract-vibe", kind: "track_fact", text: [{ type: "text", value: "Contract VibeCast" }] }],
    });
    return true;
  }

  return false;
}

function websocketAcceptValue(key) {
  return crypto.createHash("sha1").update(`${key}258EAFA5-E914-47DA-95CA-C5AB0DC85B11`).digest("base64");
}

function encodeFrame(payload, { masked = false } = {}) {
  const data = Buffer.from(payload);
  const header = [0x81];
  if (data.length < 126) {
    header.push((masked ? 0x80 : 0) | data.length);
  } else {
    header.push((masked ? 0x80 : 0) | 126, (data.length >> 8) & 0xff, data.length & 0xff);
  }
  let frame = Buffer.concat([Buffer.from(header), data]);
  if (masked) {
    const mask = crypto.randomBytes(4);
    const start = header.length + 4;
    frame = Buffer.concat([Buffer.from(header), mask, data]);
    for (let index = 0; index < data.length; index += 1) frame[start + index] = data[index] ^ mask[index % 4];
  }
  return frame;
}

function decodeFrame(buffer) {
  if (buffer.length < 2) return null;
  const opcode = buffer[0] & 0x0f;
  const masked = (buffer[1] & 0x80) !== 0;
  let length = buffer[1] & 0x7f;
  let offset = 2;
  if (length === 126) {
    if (buffer.length < offset + 2) return null;
    length = buffer.readUInt16BE(offset);
    offset += 2;
  } else if (length === 127) {
    fail("64-bit websocket frames are not supported");
  }
  let mask;
  if (masked) {
    if (buffer.length < offset + 4) return null;
    mask = buffer.subarray(offset, offset + 4);
    offset += 4;
  }
  if (buffer.length < offset + length) return null;
  const payload = Buffer.from(buffer.subarray(offset, offset + length));
  if (masked) {
    for (let index = 0; index < payload.length; index += 1) payload[index] ^= mask[index % 4];
  }
  return { opcode, text: payload.toString("utf8"), consumed: offset + length };
}

function sendJSON(socket, value) {
  socket.write(encodeFrame(JSON.stringify(value)));
}

function startContractServer(options = {}) {
  const observed = { httpRequests: [], sessionCalls: 0, websocketMessages: [] };
  const sockets = new Set();
  const host = options.host || "127.0.0.1";
  const port = Number.isInteger(options.port) ? options.port : 0;
  const server = http.createServer(async (request, response) => {
    try {
      if (await handleHTTP(request, response, observed)) return;
      jsonResponse(response, 404, { success: false, error: "not_found" });
    } catch (error) {
      jsonResponse(response, 500, { success: false, error: "contract_fixture_failed", message: error.message });
    }
  });

  server.on("upgrade", (request, socket) => {
    if (new URL(request.url, "http://127.0.0.1").pathname !== "/api/websocket") {
      socket.destroy();
      return;
    }
    const key = request.headers["sec-websocket-key"];
    socket.write([
      "HTTP/1.1 101 Switching Protocols",
      "Upgrade: websocket",
      "Connection: Upgrade",
      `Sec-WebSocket-Accept: ${websocketAcceptValue(key)}`,
      "",
      "",
    ].join("\r\n"));
    sockets.add(socket);
    sendJSON(socket, { type: "auth_required", ha_version: "2026.7.0" });
    let authenticated = false;
    let pending = Buffer.alloc(0);
    socket.on("data", (chunk) => {
      pending = Buffer.concat([pending, chunk]);
      let frame;
      while ((frame = decodeFrame(pending))) {
        pending = pending.subarray(frame.consumed);
        if (frame.opcode === 0x8) {
          socket.end();
          return;
        }
        const message = JSON.parse(frame.text);
        if (!authenticated) {
          assert(message.type === "auth", "websocket expected auth message");
          if (message.access_token === WS_SESSION) {
            authenticated = true;
            sendJSON(socket, { type: "auth_ok" });
          } else {
            sendJSON(socket, { type: "auth_invalid", message: "invalid session" });
            socket.end();
          }
          continue;
        }
        observed.websocketMessages.push({ type: message.type });
        sendJSON(socket, routeResult(message));
      }
    });
    socket.on("close", () => sockets.delete(socket));
    socket.on("error", () => sockets.delete(socket));
  });

  return new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(port, host, () => {
      const address = server.address();
      resolve({ server, sockets, observed, baseURL: `http://${host}:${address.port}` });
    });
  });
}

function postJSON(url, body, headers = {}) {
  return new Promise((resolve, reject) => {
    const data = Buffer.from(JSON.stringify(body));
    const request = http.request(url, {
      method: "POST",
      headers: { "content-type": "application/json", "content-length": data.length, ...headers },
    }, (response) => collectJSON(response, resolve, reject));
    request.on("error", reject);
    request.end(data);
  });
}

function getJSON(url, headers = {}) {
  return new Promise((resolve, reject) => {
    const request = http.request(url, { method: "GET", headers }, (response) => collectJSON(response, resolve, reject));
    request.on("error", reject);
    request.end();
  });
}

function collectJSON(response, resolve, reject) {
  const chunks = [];
  response.on("data", (chunk) => chunks.push(chunk));
  response.on("end", () => {
    try {
      resolve({ statusCode: response.statusCode, body: JSON.parse(Buffer.concat(chunks).toString("utf8")) });
    } catch (error) {
      reject(error);
    }
  });
}

async function stopContractServer(server, sockets) {
  for (const socket of sockets) socket.destroy();
  await new Promise((resolve) => server.close(resolve));
}

module.exports = {
  assert,
  contractFixture,
  decodeFrame,
  encodeFrame,
  getJSON,
  postJSON,
  routeResult,
  safeLog,
  startContractServer,
  stopContractServer,
};

if (require.main === module) {
  const port = process.env.DJCONNECT_CONTRACT_PORT ? Number.parseInt(process.env.DJCONNECT_CONTRACT_PORT, 10) : 0;
  startContractServer({ port })
    .then(({ baseURL }) => {
      safeLog(`DJConnect Windows HA contract fixture listening at ${baseURL}`);
      safeLog("Press Ctrl-C to stop.");
    })
    .catch((error) => {
      console.error(`DJConnect Windows HA contract fixture failed: ${error.message}`);
      process.exit(1);
    });
}
