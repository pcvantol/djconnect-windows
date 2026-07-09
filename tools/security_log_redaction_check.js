#!/usr/bin/env node
"use strict";

const fs = require("fs");
const path = require("path");
const { assert, contractFixture, safeLog } = require("./ha_contract_fixture");

const toolsDir = __dirname;
const checkedFiles = [
  "ha_contract_fixture.js",
  "http_e2e_contract.js",
  "websocket_e2e_contract.js",
].map((file) => path.join(toolsDir, file));

function fileText(file) {
  return fs.readFileSync(file, "utf8");
}

function captureConsole(callback) {
  const original = console.log;
  const lines = [];
  console.log = (line) => lines.push(String(line));
  try {
    callback();
  } finally {
    console.log = original;
  }
  return lines.join("\n");
}

function run() {
  for (const file of checkedFiles) {
    const text = fileText(file);
    assert(!text.includes("/api/djconnect/v1/push/bootstrap"), `${path.basename(file)} must not introduce Apple push bootstrap`);
    assert(!text.includes("apns"), `${path.basename(file)} must not introduce APNs flows`);
    assert(!text.includes("spotify_client_secret"), `${path.basename(file)} must not include Spotify secrets`);
    assert(!text.includes("openai"), `${path.basename(file)} must not include OpenAI credentials`);
  }

  const output = captureConsole(() => {
    safeLog(`credential ${contractFixture.deviceCredential}`);
    safeLog(`session ${contractFixture.webSocketSession}`);
  });
  assert(!output.includes(contractFixture.deviceCredential), "safeLog leaked device credential");
  assert(!output.includes(contractFixture.webSocketSession), "safeLog leaked websocket session");
  assert(output.includes("<redacted>"), "safeLog did not mark redacted values");

  console.log("Security/log-redaction validation passed.");
}

if (require.main === module) {
  try {
    run();
  } catch (error) {
    console.error(`Security/log-redaction validation failed: ${error.message}`);
    process.exit(1);
  }
}
