const http = require("http");
const fs = require("fs");
const path = require("path");
const os = require("os");

const PORT = 4500;
const MAX_BODY_BYTES = 50 * 1024 * 1024;
const OUTPUT_DIR = path.join(os.homedir(), "Documents", "generations");

fs.mkdirSync(OUTPUT_DIR, { recursive: true });

async function readBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let totalBytes = 0;

    req.on("data", chunk => {
      totalBytes += chunk.length;

      if (totalBytes > MAX_BODY_BYTES) {
        req.destroy();
        reject(new Error("Payload too large"));
        return;
      }

      chunks.push(chunk);
    });

    req.on("end", () => resolve(Buffer.concat(chunks)));
    req.on("error", reject);
  });
}

function decodeBase64(value, fieldName) {
  if (typeof value !== "string") {
    throw new Error(`The callback JSON does not contain a ${fieldName} string`);
  }

  const comma = value.indexOf(",");
  if (comma !== -1) {
    value = value.slice(comma + 1);
  }

  value = value.replace(/\s/g, "");
  return Buffer.from(value, "base64");
}

function sanitizeFilename(value, fallback) {
  const cleaned = String(value ?? "")
    .replace(/[<>:"/\\|?*\u0000-\u001F]/g, "")
    .replace(/\s+/g, " ")
    .trim();

  return cleaned || fallback;
}

function firstThreeWords(value) {
  const words = sanitizeFilename(value, "tts")
    .replace(/[^\p{L}\p{N}\s'-]/gu, "")
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 3);

  return sanitizeFilename(words.join(" "), "tts");
}

const server = http.createServer(async (req, res) => {
  if (
    req.method !== "POST" ||
    (req.url !== "/tts/" && req.url !== "/images/")
  ) {
    res.writeHead(404);
    return res.end("Not Found");
  }

  try {
    const body = await readBody(req);
    const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
    const json = JSON.parse(body.toString("utf8"));

    if (req.url === "/tts/") {
      const audio = decodeBase64(json.audio ?? json.Audio, "audio");

      if (audio.subarray(0, 4).toString("ascii") !== "RIFF") {
        throw new Error(
          `Decoded audio has no RIFF header; found ${JSON.stringify(
            audio.subarray(0, 4).toString("ascii")
          )}`
        );
      }

      const prefix = firstThreeWords(
        json.text ?? json.Text ?? json.prompt ?? json.Prompt ?? json.input
      );
      const jsonPath = path.join(
        OUTPUT_DIR,
        `${prefix} - ${timestamp}.txt`
      );
      const wavPath = path.join(
        OUTPUT_DIR,
        `${prefix} - ${timestamp}.wav`
      );

      await fs.promises.writeFile(jsonPath, body);
      await fs.promises.writeFile(wavPath, audio);

      console.log(`Saved TTS JSON as ${jsonPath}`);
      console.log(`Saved WAV as ${wavPath} (${audio.length} bytes)`);
    } else {
      const png = decodeBase64(
        json.imageData ??
          json.image ??
          json.Image ??
          json.png ??
          json.Png ??
          json.PNG,
        "PNG"
      );

      if (
        png.length < 8 ||
        !png.subarray(0, 8).equals(
          Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])
        )
      ) {
        throw new Error("Decoded image does not have a PNG signature");
      }

      const prefix = sanitizeFilename(
        json.titleDa ?? json.TitleDa ?? json.titleDA ?? json.TitleDA,
        "untitled"
      );
      const jsonPath = path.join(
        OUTPUT_DIR,
        `${prefix} - ${timestamp}.txt`
      );
      const pngPath = path.join(
        OUTPUT_DIR,
        `${prefix} - ${timestamp}.png`
      );

      await fs.promises.writeFile(jsonPath, body);
      await fs.promises.writeFile(pngPath, png);
      console.log(`Saved image JSON as ${jsonPath}`);
      console.log(`Saved PNG as ${pngPath} (${png.length} bytes)`);
    }

    res.writeHead(202, { "Content-Type": "text/plain" });
    res.end("Accepted");
  } catch (error) {
    console.error("Failed to save:", error);
    res.writeHead(400, { "Content-Type": "text/plain" });
    res.end(error.message);
  }
});

server.listen(PORT, () => {
  console.log(`Listening on http://localhost:${PORT}`);
  console.log(`Saving generations to ${OUTPUT_DIR}`);
});