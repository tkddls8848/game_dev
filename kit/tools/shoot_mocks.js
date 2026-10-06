#!/usr/bin/env node
/**
 * 모든 PoC의 연출 목업을 PNG로 찍는다.
 *
 *   node kit/tools/shoot_mocks.js
 *   node kit/tools/shoot_mocks.js deck-rewind farm-signal      # 일부만
 *   node kit/tools/shoot_mocks.js --missing                    # PNG가 없는 것만
 *
 * 왜 서버를 띄우는가
 * -----------------
 * 목업은 `fetch('../data/*.json')` 으로 실제 데이터를 읽는다. 그게 규약이다 —
 * 손으로 그린 그림이면 여러 연출을 견줄 수 없다.
 *
 * 그런데 **`file://` 에서 fetch 는 Chromium 이 막는다.** 목업을 브라우저로 직접 열면
 * 빈 화면이 나온다. 그래서 저장소 뿌리를 정적 서버로 띄우고 http:// 로 방문한다.
 * 사람이 직접 볼 때도 같은 이유로 서버가 필요하다(아래 안내 참고).
 *
 * 출력: docs/poc-gallery/png/<슬러그>.png  +  shots.html (PNG만 보는 목록)
 */

const http = require("http");
const fs = require("fs");
const path = require("path");

const REPO = path.resolve(__dirname, "..", "..");
const OUT_DIR = path.join(REPO, "docs", "poc-gallery", "png");
const VIEWPORT = { width: 1600, height: 1000 };

const MIME = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".svg": "image/svg+xml",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".webp": "image/webp",
  ".ttf": "font/ttf",
  ".otf": "font/otf",
  ".woff": "font/woff",
  ".woff2": "font/woff2",
  ".mp3": "audio/mpeg",
  ".ogg": "audio/ogg",
  ".wav": "audio/wav",
  ".txt": "text/plain; charset=utf-8",
};

function serve(root) {
  return new Promise((resolve) => {
    const server = http.createServer((req, res) => {
      const rel = decodeURIComponent(req.url.split("?")[0]).replace(/^\/+/, "");
      const file = path.join(root, rel);
      // 뿌리 밖으로 나가는 요청은 거부한다
      if (!file.startsWith(root)) {
        res.writeHead(403).end();
        return;
      }
      fs.readFile(file, (err, buf) => {
        if (err) {
          res.writeHead(404).end();
          return;
        }
        res.writeHead(200, { "Content-Type": MIME[path.extname(file).toLowerCase()] || "application/octet-stream" });
        res.end(buf);
      });
    });
    server.listen(0, "127.0.0.1", () => resolve(server));
  });
}

/**
 * playwright 를 찾는다. 셋 중 어디에 있어도 쓴다.
 *
 * 이 저장소에는 playwright 를 devDependency 로 두지 않는다 — PoC 스무 개를 찍는 데
 * node_modules 를 들이는 값이 크지 않다. 대신 `npx playwright ...` 를 한 번이라도 돌렸으면
 * npm 이 캐시에 넣어 두므로 거기서 꺼내 쓴다. 캐시가 비면 아래 안내대로 설치하면 된다.
 */
function loadChromium() {
  const tries = [];
  try { tries.push(require("playwright")); } catch {}
  try {
    const g = require("child_process").execSync("npm root -g", { encoding: "utf8" }).trim();
    tries.push(require(path.join(g, "playwright")));
  } catch {}
  // npx 캐시: %LOCALAPPDATA%/npm-cache/_npx/<해시>/node_modules/playwright
  try {
    const cache = path.join(process.env.LOCALAPPDATA || process.env.HOME || "", "npm-cache", "_npx");
    for (const dir of fs.readdirSync(cache)) {
      const p = path.join(cache, dir, "node_modules", "playwright");
      if (fs.existsSync(p)) {
        try { tries.push(require(p)); } catch {}
      }
    }
  } catch {}
  for (const m of tries) if (m && m.chromium) return m.chromium;
  return null;
}

/** games/ 아래에서 목업이 있는 PoC를 찾는다. mystery-blackwood 는 Unity 게임이라 뺀다. */
function findPocs(filter) {
  const games = path.join(REPO, "games");
  return fs
    .readdirSync(games, { withFileTypes: true })
    .filter((d) => d.isDirectory() && d.name !== "mystery-blackwood" && !d.name.startsWith("_"))  // _archive = 보관
    .map((d) => d.name)
    .filter((n) => !filter.length || filter.includes(n))
    .filter((n) => fs.existsSync(path.join(games, n, "presentation", "index.html")))
    .sort();
}

(async () => {
  const chromium = loadChromium();
  if (!chromium) {
    console.error("playwright 를 찾지 못했다. `npm i -D playwright && npx playwright install chromium`");
    process.exit(1);
  }

  const onlyMissing = process.argv.includes("--missing");
  const wanted = process.argv.slice(2).filter((arg) => arg !== "--missing");
  const slugs = findPocs(wanted).filter((slug) => !onlyMissing || !fs.existsSync(path.join(OUT_DIR, slug + ".png")));
  if (!slugs.length) {
    if (onlyMissing && !wanted.length) {
      require('child_process').execFileSync('python', [path.join(REPO, 'kit', 'tools', 'build_poc_gallery.py')], { stdio: 'inherit' });
      return;
    }
    console.error("목업이 있는 PoC가 없다" + (wanted.length ? " (요청: " + wanted.join(", ") + ")" : ""));
    process.exit(1);
  }

  fs.mkdirSync(OUT_DIR, { recursive: true });
  const server = await serve(REPO);
  const port = server.address().port;
  const browser = await chromium.launch();
  const results = [];
  const shotOf = {};   // 슬러그 -> 어느 절을 찍었나

  for (const slug of slugs) {
    const page = await browser.newPage({ viewport: VIEWPORT, deviceScaleFactor: 1 });
    const errors = [];
    page.on("pageerror", (e) => errors.push(String(e.message).slice(0, 200)));
    page.on("requestfailed", (r) => errors.push("요청 실패: " + r.url().replace("http://127.0.0.1:" + port, "")));

    const url = `http://127.0.0.1:${port}/games/${slug}/presentation/index.html`;
    try {
      await page.goto(url, { waitUntil: "networkidle", timeout: 30000 });
      // 폰트와 fetch 후 렌더가 끝날 여유
      await page.evaluate(() => document.fonts && document.fonts.ready);
      await page.waitForTimeout(600);

      // 데이터가 실제로 들어왔는지 본다 — 빈 화면을 '성공'으로 찍지 않는다
      const text = (await page.evaluate(() => document.body.innerText || "")).trim();

      // 내용이 없으면 **찍지 않는다.** 아직 만들고 있는 PoC(템플릿 뼈대만 있는 상태)를
      // 빈 이미지로 남기면 목록에서 '연출이 이렇다'로 읽힌다 — 없는 것이 낫다.
      if (text.length <= 200) {
        results.push({ slug, bytes: 0, chars: text.length, errors, skipped: true });
        console.log(`건너뜀 ${slug.padEnd(24)} (내용 없음 — 아직 만들고 있는 중이거나 fetch 실패)`);
        for (const e of errors.slice(0, 3)) console.log("      " + e);
        await page.close();
        continue;
      }

      // 목업은 문서 꼴이고 **게임 화면은 그 안의 한 절**이다
      // (예: deck-rewind 의 "§3 전투 한 장면"). 문서 머리를 찍으면 데이터 시트가 나오지
      // 게임 화면이 나오지 않는다. 그래서 화면에 해당하는 절을 찾아 그것만 찍는다.
      // 그 절이 화면 가운데 오도록 **스크롤한 뒤 뷰포트를 찍는다.**
      //
      // 절만 잘라 찍으면(element screenshot) 두 가지가 깨진다: 폭이 제각각이라 나란히
      // 비교가 안 되고, 옆 칸이 잘려 나간다(hybrid 는 '밭과 덱을 나란히'가 핵인데
      // '밭'만 521px 로 잘렸다). 뷰포트를 찍으면 전부 같은 1600x1000 화면이 된다.
      const target = await page.evaluate((slug) => {
        if (document.querySelector('#app[data-ready="true"]')) return null;
        const preferred = {
          'grafted-memory': '의뢰인의 기억망', 'scent-layers': '크로마토그램',
          'locked-phone': '여러 시각의 잠금 화면', 'tactics-whisper-map': '천 위의 마을',
          'curse-ledger': '족보', 'gods-secretary': '오늘의 접수함',
          'last-tenants': '세대 배치도', 'red-pen': '교정지', 'the-interpreter': '회담 기록'
        };
        const preferredHead = preferred[slug] && Array.from(document.querySelectorAll('h2,h3')).find(h => h.textContent.includes(preferred[slug]));
        if (preferredHead) {
          (preferredHead.closest('section') || preferredHead).scrollIntoView({ block: 'start' });
          return preferredHead.textContent.trim().slice(0, 48);
        }
        // h1(문서 제목)은 뺀다 — 제목에도 '밭'·'되감기' 같은 말이 들어 있어 문서 전체가 잡힌다.
        const STRONG = ["전투", "장면", "임무", "시세판", "압판", "지도", "초소", "밭", "덱", "달력", "한 해",
                        "board", "scene", "mission", "field"];
        const heads = Array.from(document.querySelectorAll("h2,h3"));
        for (const h of heads) {
          const t = (h.textContent || "").toLowerCase();
          if (STRONG.some((w) => t.includes(w.toLowerCase()))) {
            h.scrollIntoView({ block: "center" });
            return (h.textContent || "").trim().replace(/\s+/g, " ").slice(0, 32);
          }
        }
        return null;   // 못 찾으면 문서 머리 그대로
      }, slug);
      await page.waitForTimeout(250);

      const out = path.join(OUT_DIR, slug + ".png");
      await page.screenshot({ path: out });
      shotOf[slug] = target;

      const scrollH = await page.evaluate(() => document.documentElement.scrollHeight);
      const fullOut = path.join(OUT_DIR, slug + "-full.png");
      if (scrollH > VIEWPORT.height * 1.2) {
        await page.screenshot({ path: fullOut, fullPage: true });
      } else if (fs.existsSync(fullOut)) {
        fs.unlinkSync(fullOut);   // 짧아졌으면 남은 파일을 지운다
      }

      const bytes = fs.statSync(out).size;
      results.push({ slug, bytes, chars: text.length, errors });
      console.log(
        `${text.length > 200 ? "ok  " : "빈?  "}${slug.padEnd(24)} ${(bytes / 1024).toFixed(0).padStart(5)}KB  글자 ${String(text.length).padStart(5)}` +
          (shotOf[slug] ? `  [${shotOf[slug]}]` : "  [뷰포트]") + (errors.length ? `  ⚠ ${errors.length}건` : "")
      );
      for (const e of errors.slice(0, 3)) console.log("      " + e);
    } catch (e) {
      results.push({ slug, bytes: 0, chars: 0, errors: [String(e.message).slice(0, 200)] });
      console.log(`실패 ${slug}: ${String(e.message).slice(0, 160)}`);
    }
    await page.close();
  }

  await browser.close();
  server.close();

  const reportPath = path.join(REPO, 'docs', 'poc-gallery', 'capture-report.json');
  let previous = [];
  try { previous = JSON.parse(fs.readFileSync(reportPath, 'utf8')).results || []; } catch {}
  const merged = new Map(previous.map(r => [r.slug, r]));
  for (const result of results) merged.set(result.slug, { ...result, capturedAt: new Date().toISOString() });
  fs.writeFileSync(reportPath, JSON.stringify({ capturedAt: new Date().toISOString(), results: [...merged.values()] }, null, 2));
  require('child_process').execFileSync('python', [path.join(REPO, 'kit', 'tools', 'build_poc_gallery.py')], { stdio: 'inherit' });
  if (results.some(r => !r.skipped && (!r.bytes || r.errors.length))) process.exitCode = 1;
})();
