#!/usr/bin/env node
"use strict";

const {
  assert,
  contractFixture,
  getJSON,
  postJSON,
  startContractServer,
  stopContractServer,
} = require("./ha_contract_fixture");

function authHeaders() {
  return { authorization: `Bearer ${contractFixture.deviceCredential}` };
}

function identity(extra = {}) {
  return {
    client_id: contractFixture.deviceID,
    device_id: contractFixture.deviceID,
    device_name: contractFixture.deviceName,
    client_type: contractFixture.clientType,
    ...extra,
  };
}

async function post(baseURL, path, body) {
  const response = await postJSON(`${baseURL}${path}`, body, authHeaders());
  assert(response.statusCode === 200, `${path} failed with HTTP ${response.statusCode}`);
  return response.body;
}

async function get(baseURL, path) {
  const response = await getJSON(`${baseURL}${path}`, authHeaders());
  assert(response.statusCode === 200, `${path} failed with HTTP ${response.statusCode}`);
  return response.body;
}

async function run() {
  const { server, sockets, observed, baseURL } = await startContractServer();
  try {
    const pair = await post(baseURL, "/api/djconnect/v1/pair", identity({ pair_code: "123456", pairing_token: "123456", pairing_code: "123456", app_version: "ci" }));
    assert(pair.client_type === "windows", "pair response must keep Windows client type");
    assert(pair.device_token === contractFixture.deviceCredential, "pair response missing device credential");
    assert(pair.dj_announcement?.default_output === "both", "pair response missing DJ announcement capabilities");

    const status = await post(baseURL, "/api/djconnect/v1/status", identity({ firmware: "windows-app", version: "ci" }));
    assert(status.playback?.title === "Contract Track", "status response missing playback");

    const command = await post(baseURL, "/api/djconnect/v1/command", identity({ command: "ask_dj_followup_response", args: { response: "yes" }, dj_announcement_output: "both" }));
    assert(command.success === true, "command response failed");

    const event = await post(baseURL, "/api/djconnect/v1/event", identity({ type: "foreground" }));
    assert(event.success === true, "event response failed");

    const ask = await post(baseURL, "/api/djconnect/v1/ask_dj/message", identity({ client_message_id: "contract-client-message", text: "Tell me about this track", audio_response: "auto", dj_announcement_output: "both" }));
    assert(ask.assistant_message?.announcement?.delivery === "both", "Ask DJ response missing announcement delivery");

    const history = await get(baseURL, "/api/djconnect/v1/ask_dj/history?since_revision=0");
    assert(history.history_revision === 2, "history response failed");

    const clear = await post(baseURL, "/api/djconnect/v1/ask_dj/history/clear", identity());
    assert(clear.cleared === true, "history clear response failed");

    const profile = await post(baseURL, "/api/djconnect/v1/music_dna/profile", identity({ music_dna_key: "contract-key" }));
    assert(profile.enabled === true, "Music DNA profile failed");

    const settings = await post(baseURL, "/api/djconnect/v1/music_dna/settings", identity({ enabled: true, music_dna_key: "contract-key" }));
    assert(settings.generation === 8, "Music DNA settings failed");

    const dnaClear = await post(baseURL, "/api/djconnect/v1/music_dna/clear", identity({ music_dna_key: "contract-key" }));
    assert(dnaClear.generation === 9, "Music DNA clear failed");

    const feed = await get(baseURL, "/api/djconnect/v1/music_discovery");
    assert(feed.sections?.[0]?.items?.[0]?.id === "disco-1", "Music Discovery feed failed");

    const refresh = await post(baseURL, "/api/djconnect/v1/music_discovery/refresh", identity({ music_dna_key: "contract-key" }));
    assert(refresh.revision === 4, "Music Discovery refresh failed");

    const play = await post(baseURL, "/api/djconnect/v1/music_discovery/play", identity({ discovery_item_id: "disco-1", section_id: "new_for_you" }));
    assert(play.accepted === true, "Music Discovery play failed");

    const feedback = await post(baseURL, "/api/djconnect/v1/music_discovery/feedback", identity({ discovery_item_id: "disco-1", section_id: "new_for_you", feedback: "less_like_this" }));
    assert(feedback.feedback_recorded === true, "Music Discovery feedback failed");

    const insight = await post(baseURL, "/api/djconnect/v1/track_insight", identity({ track: { title: "Contract Track", artist: "Contract Artist" } }));
    assert(insight.track_insight?.analysis?.summary === "Contract insight", "Track Insight failed");

    const vibecast = await get(baseURL, "/api/djconnect/v1/vibecast?locale=nl-NL");
    assert(vibecast.enabled === true, "VibeCast failed");

    const expected = [
      "/api/djconnect/v1/pair",
      "/api/djconnect/v1/status",
      "/api/djconnect/v1/command",
      "/api/djconnect/v1/event",
      "/api/djconnect/v1/ask_dj/message",
      "/api/djconnect/v1/ask_dj/history",
      "/api/djconnect/v1/ask_dj/history/clear",
      "/api/djconnect/v1/music_dna/profile",
      "/api/djconnect/v1/music_dna/settings",
      "/api/djconnect/v1/music_dna/clear",
      "/api/djconnect/v1/music_discovery",
      "/api/djconnect/v1/music_discovery/refresh",
      "/api/djconnect/v1/music_discovery/play",
      "/api/djconnect/v1/music_discovery/feedback",
      "/api/djconnect/v1/track_insight",
      "/api/djconnect/v1/vibecast",
    ];
    assert(JSON.stringify(observed.httpRequests.map((request) => request.path)) === JSON.stringify(expected), "unexpected HTTP route order");
    assert(observed.sessionCalls === 0, "HTTP e2e must not require websocket session");
    console.log(`HTTP contract e2e passed: ${expected.length} routes.`);
  } finally {
    await stopContractServer(server, sockets);
  }
}

if (require.main === module) {
  run().catch((error) => {
    console.error(`HTTP contract e2e failed: ${error.message}`);
    process.exit(1);
  });
}
