#!/usr/bin/env node
"use strict";

const crypto = require("crypto");
const net = require("net");
const {
  assert,
  contractFixture,
  decodeFrame,
  encodeFrame,
  postJSON,
  startContractServer,
  stopContractServer,
} = require("./ha_contract_fixture");

function identity(extra = {}) {
  return {
    client_id: contractFixture.deviceID,
    device_id: contractFixture.deviceID,
    device_name: contractFixture.deviceName,
    client_type: contractFixture.clientType,
    device_token: contractFixture.deviceCredential,
    ...extra,
  };
}

function authHeaders() {
  return { authorization: `Bearer ${contractFixture.deviceCredential}` };
}

class RawWebSocketClient {
  constructor(baseURL) {
    const url = new URL(baseURL);
    this.host = url.hostname;
    this.port = Number.parseInt(url.port, 10);
    this.socket = null;
    this.buffer = Buffer.alloc(0);
    this.messages = [];
    this.waiters = [];
  }

  connect() {
    return new Promise((resolve, reject) => {
      const key = crypto.randomBytes(16).toString("base64");
      this.socket = net.createConnection({ host: this.host, port: this.port }, () => {
        this.socket.write([
          "GET /api/websocket HTTP/1.1",
          `Host: ${this.host}:${this.port}`,
          "Upgrade: websocket",
          "Connection: Upgrade",
          `Sec-WebSocket-Key: ${key}`,
          "Sec-WebSocket-Version: 13",
          "",
          "",
        ].join("\r\n"));
      });
      this.socket.once("error", reject);
      const waitForUpgrade = (chunk) => {
        this.buffer = Buffer.concat([this.buffer, chunk]);
        const separator = this.buffer.indexOf("\r\n\r\n");
        if (separator < 0) return;
        const header = this.buffer.subarray(0, separator).toString("utf8");
        if (!header.startsWith("HTTP/1.1 101")) {
          reject(new Error("websocket upgrade failed"));
          return;
        }
        this.socket.off("data", waitForUpgrade);
        this.socket.on("data", (data) => this.onData(data));
        this.buffer = this.buffer.subarray(separator + 4);
        this.drainFrames();
        resolve();
      };
      this.socket.on("data", waitForUpgrade);
    });
  }

  onData(chunk) {
    this.buffer = Buffer.concat([this.buffer, chunk]);
    this.drainFrames();
  }

  drainFrames() {
    let frame;
    while ((frame = decodeFrame(this.buffer))) {
      this.buffer = this.buffer.subarray(frame.consumed);
      if (frame.opcode === 0x1) {
        const message = JSON.parse(frame.text);
        const waiter = this.waiters.shift();
        if (waiter) waiter(message);
        else this.messages.push(message);
      }
    }
  }

  receive() {
    if (this.messages.length > 0) return Promise.resolve(this.messages.shift());
    return new Promise((resolve) => this.waiters.push(resolve));
  }

  send(value) {
    this.socket.write(encodeFrame(JSON.stringify(value), { masked: true }));
  }

  close() {
    this.socket?.destroy();
  }
}

async function call(client, id, type, payload = {}) {
  client.send({ id, type, ...identity(payload) });
  const message = await client.receive();
  assert(message.id === id, `${type} returned wrong id`);
  assert(message.success === true, `${type} did not return success`);
  return message.result;
}

async function run() {
  const { server, sockets, observed, baseURL } = await startContractServer();
  const client = new RawWebSocketClient(baseURL);
  try {
    const session = await postJSON(`${baseURL}/api/djconnect/v1/websocket/session`, {
      ...identity({ requested_commands: contractFixture.routes }),
    }, authHeaders());
    assert(session.statusCode === 200, "websocket session endpoint failed");
    assert(Array.isArray(session.body.commands), "session response missing commands");

    await client.connect();
    const authRequired = await client.receive();
    assert(authRequired.type === "auth_required", "server did not send auth_required");
    client.send({ type: "auth", access_token: session.body.access_token });
    const authOK = await client.receive();
    assert(authOK.type === "auth_ok", "server did not send auth_ok");

    const capabilities = await call(client, 1, "djconnect/capabilities");
    assert(capabilities.websocket_supported === true, "capabilities did not enable websocket");

    const command = await call(client, 2, "djconnect/command", { command: "play" });
    assert(command.playback?.title === "Contract Track", "websocket command failed");

    const ask = await call(client, 3, "djconnect/ask_dj/message", { client_message_id: "contract-client-message", text: "Tell me about this track" });
    assert(ask.assistant_message?.text === "Contract answer", "websocket Ask DJ failed");

    const history = await call(client, 4, "djconnect/ask_dj/history");
    assert(history.history_revision === 2, "websocket Ask DJ history failed");

    const clear = await call(client, 5, "djconnect/ask_dj/history/clear");
    assert(clear.cleared === true, "websocket Ask DJ clear failed");

    const state = await call(client, 6, "djconnect/ask_dj/history/state");
    assert(state.ask_dj_clear_required === true, "websocket Ask DJ history state failed");

    const profile = await call(client, 7, "djconnect/music_dna/profile", { music_dna_key: "contract-key" });
    assert(profile.enabled === true, "websocket Music DNA profile failed");

    const settings = await call(client, 8, "djconnect/music_dna/settings", { enabled: true, music_dna_key: "contract-key" });
    assert(settings.generation === 8, "websocket Music DNA settings failed");

    const dnaClear = await call(client, 9, "djconnect/music_dna/clear", { music_dna_key: "contract-key" });
    assert(dnaClear.generation === 9, "websocket Music DNA clear failed");

    const feed = await call(client, 10, "djconnect/music_discovery/feed", { music_dna_key: "contract-key" });
    assert(feed.sections?.[0]?.items?.[0]?.id === "disco-1", "websocket Music Discovery feed failed");

    const refresh = await call(client, 11, "djconnect/music_discovery/refresh", { music_dna_key: "contract-key" });
    assert(refresh.revision === 4, "websocket Music Discovery refresh failed");

    const play = await call(client, 12, "djconnect/music_discovery/play", { discovery_item_id: "disco-1", section_id: "new_for_you" });
    assert(play.accepted === true, "websocket Music Discovery play failed");

    const feedback = await call(client, 13, "djconnect/music_discovery/feedback", { discovery_item_id: "disco-1", section_id: "new_for_you", feedback: "less_like_this" });
    assert(feedback.feedback_recorded === true, "websocket Music Discovery feedback failed");

    const insight = await call(client, 14, "djconnect/track_insight", { track: { title: "Contract Track", artist: "Contract Artist" } });
    assert(insight.track_insight?.analysis?.summary === "Contract insight", "websocket Track Insight failed");

    const expected = contractFixture.routes;
    assert(JSON.stringify(observed.websocketMessages.map((message) => message.type)) === JSON.stringify(expected), "unexpected websocket route order");
    assert(observed.sessionCalls === 1, "websocket e2e must use one websocket session request");
    console.log(`WebSocket contract e2e passed: ${expected.length} routes.`);
  } finally {
    client.close();
    await stopContractServer(server, sockets);
  }
}

if (require.main === module) {
  run().catch((error) => {
    console.error(`WebSocket contract e2e failed: ${error.message}`);
    process.exit(1);
  });
}
