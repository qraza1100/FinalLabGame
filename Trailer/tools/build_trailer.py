"""
Writes the HyperFrames trailer (index.html + compositions/*.html) from one edit table.
Run from the Trailer folder:  python tools/build_trailer.py
Every timed element is written as static HTML (editable in the HyperFrames desktop app);
this script only saves typing. Change a number in EDIT below and re-run to retime the film.
"""
import json, os, html

W, H, FPS = 1920, 1080, 30
TOTAL = 40.6
P = 0.46875  # one beat at 128 BPM

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
VO = json.load(open(os.path.join(ROOT, "assets/voice/lines.json"), encoding="utf-8"))
VO = {v["id"]: v for v in VO}
try:
    PEAKS = json.load(open(os.path.join(ROOT, "assets/sfx/peaks.json")))
except Exception:
    PEAKS = {}

# ------------------------------------------------------------------ the edit
# clip: (id, footage, t0, t1, media_start, rate, options)
#   options: cam=(origin_x, origin_y), punch=[(t, scale, dur)], shake=[t], whip_in, whip_out, zoom_out=(t, scale)
SCENES = [
    {"id": "hook", "t0": 0.0, "t1": 5.625, "clips": [
        ("hook-v", "g13_take_heart", 0.0, 4.219, 0.38, 1.0, {"cam": (1110, 520), "start_scale": 1.45,
                                                             "punch": [(0.55, 1.0, 0.32)], "shake": [0.55]}),
        ("hook-freeze", "still:freeze_hook.jpg", 4.219, 4.46, 0, 1.0, {"cam": (960, 540), "punch": [(4.219, 1.06, 0.24)]}),
        ("hook-rewind", "rewind_vhs", 4.46, 5.625, 0.0, 1.83, {}),
     ], "flashes": [0.55], "stamps": [
        ("st-rewind", "REWIND", 4.5, 5.6, 170, 150, "#F2EFE6", -4, 76, "rewind"),
     ]},
    {"id": "dive", "t0": 5.625, "t1": 9.375, "clips": [
        ("dive-menu", "m01_menu_type", 5.625, 7.31, 0.0, 1.0, {"cam": (368, 608), "zoom_through": (7.03, 6.0)}),
        ("dive-crane", "g02_crane", 7.31, 9.375, 0.9, 1.1, {"cam": (960, 560), "start_scale": 1.3, "punch": [(7.31, 1.0, 0.4)]}),
     ], "flashes": [], "stamps": []},
    {"id": "rules", "t0": 9.375, "t1": 26.25, "clips": [
        ("r-air", "g04_moon_gate", 9.375, 11.25, 3.2, 1.0, {"cam": (300, 1000), "punch": [(9.55, 2.05, 0.26)]}),
        ("r-sonar", "g03_sonar_pearl_sigil", 11.25, 12.89, 0.25, 1.0, {"cam": (960, 540), "whip_in": True}),
        ("r-pearl", "g03_sonar_pearl_sigil", 12.89, 14.06, 3.23, 1.0, {"cam": (980, 500), "punch": [(13.2, 1.38, 0.24)]}),
        ("r-sigil", "g03_sonar_pearl_sigil", 14.06, 14.95, 5.22, 1.0, {"cam": (930, 520), "start_scale": 1.25, "whip_out": True}),
        ("r-gate", "g04_moon_gate", 14.95, 16.41, 1.6, 1.0, {"cam": (960, 540), "whip_in": True}),
        ("r-current", "g05_current", 16.41, 17.30, 0.5, 1.0, {"cam": (960, 540), "start_scale": 1.2}),
        ("r-urchin", "g06_urchin", 17.30, 18.28, 1.82, 1.0, {"cam": (1000, 560), "start_scale": 1.3, "shake": [17.85]}),
        ("r-vent", "g07_vent", 18.28, 19.69, 0.9, 1.0, {"cam": (940, 560), "punch": [(18.45, 1.35, 0.26)], "whip_in": True}),
        ("r-lose", "g16_lose", 19.69, 20.67, 2.62, 1.0, {"cam": (960, 540)}),
        ("r-lose-hold", "still:freeze_lose.jpg", 20.67, 21.09, 0, 1.0, {"cam": (960, 540), "start_scale": 1.0, "punch": [(20.67, 1.04, 0.4)]}),
        ("r-shark", "g10_shark", 21.09, 21.91, 1.65, 1.0, {"cam": (1000, 520), "start_scale": 1.15}),
        ("r-shark-freeze", "still:freeze_shark.jpg", 21.91, 22.97, 0, 1.0, {"cam": (1010, 510), "punch": [(21.91, 1.6, 0.22)], "shake": [21.91]}),
        ("r-sun", "g08_sun_sigil", 22.97, 23.44, 1.58, 1.0, {"cam": (960, 540), "start_scale": 1.2}),
        ("r-star", "g11_star_sigil", 23.44, 23.91, 0.8, 1.0, {"cam": (960, 540), "start_scale": 1.2}),
        ("r-heart", "g12_heart_reveal", 23.91, 26.25, 0.5, 1.0, {"cam": (960, 540), "start_scale": 1.12, "punch": [(23.91, 1.0, 2.3)]}),
     ], "flashes": [14.51, 21.91, 23.91], "stamps": [
        ("st-pearl", "+10 PTS", 13.39, 14.0, 1180, 330, "#3DF5E0", -6, 64, "good"),
        ("st-moon", "MOON SIGIL", 14.51, 14.95, 1130, 300, "#3DF5E0", 5, 60, "good"),
        ("st-gate", "GATE OPEN", 15.35, 16.35, 1180, 330, "#3DF5E0", -4, 60, "good"),
        ("st-current", "PUSHED!", 16.6, 17.28, 1200, 300, "#F2EFE6", 6, 62, "plain"),
        ("st-urchin", "-12 O2", 17.86, 18.26, 1180, 320, "#FF4D4D", -6, 70, "bad"),
        ("st-vent", "+AIR", 18.62, 19.66, 1200, 320, "#3DF5E0", 5, 70, "good"),
        ("st-lose", "(OR DON'T.)", 20.06, 21.07, 960, 380, "#FFB43A", -5, 92, "gag"),
        ("st-shark", "NOT FRIENDLY", 21.95, 22.95, 1120, 290, "#FF4D4D", 6, 74, "bad"),
        ("st-sun", "2 / 3", 23.22, 23.62, 1180, 330, "#3DF5E0", -5, 66, "good"),
        ("st-star", "3 / 3", 23.64, 24.4, 1180, 330, "#3DF5E0", 5, 66, "good"),
     ]},
    {"id": "payoff", "t0": 26.25, "t1": 36.09, "clips": [
        ("p-grab", "g13_take_heart", 26.25, 28.125, 0.38, 1.0, {"cam": (1110, 520), "start_scale": 1.35, "punch": [(26.80, 1.0, 0.32)], "shake": [26.80]}),
        ("p-escape", "g14_escape", 28.125, 31.875, 0.0, 1.76, {"cam": (960, 560), "punch": [(29.14, 1.18, 0.12), (29.30, 1.0, 0.5)], "shake": [29.14]}),
        ("p-win", "g15_exit_win", 31.875, 34.22, 0.6, 1.0, {"cam": (960, 540), "punch": [(32.9, 1.3, 0.3)]}),
        ("p-board", "m02_board", 34.22, 36.09, 0.3, 1.0, {"cam": (960, 430), "punch": [(34.6, 1.32, 0.28)], "whip_in": True}),
     ], "flashes": [26.80, 29.14, 32.405], "stamps": [
        ("st-score", "SCORE 670", 33.25, 34.2, 1400, 140, "#FFB43A", -5, 62, "gag"),
     ]},
]
ENDCARD = (36.09, TOTAL)

# voice line -> absolute start
VO_AT = {"v01": 0.12, "v02": 2.80, "v03": 4.26, "v04": 5.70, "v05": 9.45, "v06": 11.30, "v07": 12.93, "v08": 14.10,
         "v09": 16.45, "v10": 18.32, "v11": 21.12, "v12": 23.0, "v13": 26.30, "v14": 28.15, "v15": 32.45, "v16": 34.26,
         "v17": 36.35}

# caption groups: (voice id, [words...], highlight word index set)
CAPTIONS = [
    ("v01", [["P.O.V."], ["YOU", "JUST", "STOLE"], ["THE", "HEART"], ["OF", "ETERNITY"]], {"HEART"}),
    ("v02", [["AND", "THE", "TEMPLE"], ["WANTS", "IT", "BACK"]], {"BACK"}),
    ("v03", [["OKAY."]], set()),
    ("v04", [["TYPE", "YOUR", "NAME."], ["DIVE", "IN."]], {"DIVE"}),
    ("v05", [["YOUR", "AIR", "DRAINS"], ["EVERY", "SECOND."]], {"AIR"}),
    ("v06", [["LOST?"], ["PING", "THE", "SONAR."]], {"SONAR."}),
    ("v07", [["PEARLS", "ARE", "POINTS."]], {"POINTS."}),
    ("v08", [["SIGILS"], ["OPEN", "GATES."]], {"SIGILS"}),
    ("v09", [["CURRENTS", "PUSH."], ["URCHINS", "STING."]], {"STING."}),
    ("v10", [["VENTS", "KEEP", "YOU"], ["BREATHING."]], {"BREATHING."}),
    ("v11", [["AND", "THE", "SHARK?"], ["NOT", "FRIENDLY."]], {"SHARK?"}),
    ("v12", [["THREE", "SIGILS."], ["ONE", "HEART."]], {"HEART."}),
    ("v13", [["GRAB", "IT,"], ["AND", "THE", "CLOCK", "STARTS."]], {"CLOCK"}),
    ("v14", [["NINETY", "SECONDS."], ["GO!"]], {"GO!"}),
    ("v15", [["YOU", "MADE", "IT", "OUT."]], {"OUT."}),
    ("v16", [["NOW", "BEAT"], ["THE", "LEADERBOARD."]], {"LEADERBOARD."}),
]

# sound effects: (file, the moment it must hit, volume, id)
SFX = [
    ("impact-bass-1.mp3", 0.55, 0.9), ("game_pickup_heart.wav", 0.55, 0.8), ("game_collapse.wav", 0.95, 0.55),
    ("scratch.wav", 4.22, 0.9), ("rewind.wav", 4.50, 0.7),
    ("typing.mp3", 6.12, 0.5), ("click.mp3", 7.00, 0.8), ("whoosh-hi.wav", 7.22, 0.7),
    ("whoosh-short.mp3", 9.55, 0.6), ("game_sonar.wav", 11.47, 0.7), ("whoosh.mp3", 11.25, 0.55),
    ("game_pickup_pearl.wav", 13.39, 0.8), ("pop.mp3", 13.40, 0.6),
    ("game_pickup_sigil.wav", 14.51, 0.8), ("whoosh-low.wav", 14.95, 0.6), ("game_gate_open.wav", 15.30, 0.7),
    ("whoosh-short.mp3", 16.41, 0.45), ("game_hit.wav", 17.85, 0.9), ("glitch-1.mp3", 17.85, 0.35),
    ("game_vent.wav", 18.40, 0.6), ("whoosh-hi.wav", 18.28, 0.45),
    ("game_lose.wav", 20.04, 0.8), ("pop.mp3", 20.06, 0.6),
    ("game_hit.wav", 21.91, 0.9), ("impact-bass-2.mp3", 21.91, 0.8),
    ("game_pickup_sigil.wav", 23.22, 0.7), ("ping.mp3", 23.64, 0.6), ("sparkle.mp3", 23.95, 0.6),
    ("game_pickup_heart.wav", 26.80, 0.8), ("game_collapse.wav", 27.10, 0.5), ("whoosh-cinematic.mp3", 28.12, 0.6),
    ("game_boost.wav", 29.14, 0.8), ("impact-bass-1.mp3", 29.14, 0.7),
    ("game_win.wav", 32.40, 0.8), ("pop.mp3", 33.25, 0.6), ("whoosh.mp3", 34.22, 0.5), ("pop.mp3", 34.60, 0.5),
    ("whoosh-low.wav", 36.09, 0.6), ("impact-bass-1.mp3", 37.04, 0.9), ("pop.mp3", 37.64, 0.5),
]

# ------------------------------------------------------------------ helpers
def r3(x):
    return f"{x:.3f}".rstrip("0").rstrip(".") if "." in f"{x:.3f}" else str(x)


def speech_segments(v):
    """Speech intervals from the silencedetect output stored in lines.json."""
    dur = v["dur"]
    starts, ends = v["sil_starts"], v["sil_ends"]
    sil = []
    si = 0
    if ends and (not starts or ends[0] < starts[0]):
        sil.append((0.0, ends[0])); ends = ends[1:]
    for s, e in zip(starts, ends):
        sil.append((s, e))
    segs, t = [], 0.0
    for s, e in sorted(sil):
        if s - t > 0.05:
            segs.append((t, s))
        t = max(t, e)
    if dur - t > 0.05:
        segs.append((t, dur))
    return segs or [(0.0, dur)]


def word_times(vid, groups):
    """Spread words over the speech segments by character count (one segment per group when they match)."""
    v = VO[vid]
    segs = speech_segments(v)
    at = VO_AT[vid]
    out = []
    # phrases end at a group whose last word ends with punctuation (or the last group)
    phrases, cur = [], []
    for gi, g in enumerate(groups):
        cur.append(g)
        if g[-1][-1] in ".?!,:" or gi == len(groups) - 1:
            phrases.append(cur); cur = []
    if len(segs) == len(groups):
        pairs = list(zip(groups, segs))
    elif len(segs) == len(phrases):
        pairs = []
        for ph, (a, b) in zip(phrases, segs):
            tot = sum(len(w) + 1 for g in ph for w in g)
            t = a
            for g in ph:
                span = (b - a) * sum(len(w) + 1 for w in g) / tot
                pairs.append((g, (t, t + span))); t += span
    else:
        s0, s1 = segs[0][0], segs[-1][1]
        allw = [w for g in groups for w in g]
        total = sum(len(w) + 1 for w in allw)
        pairs, t = [], s0
        for g in groups:
            span = (s1 - s0) * sum(len(w) + 1 for w in g) / total
            pairs.append((g, (t, t + span))); t += span
    for g, (a, b) in pairs:
        total = sum(len(w) + 1 for w in g)
        t = a
        ws = []
        for w in g:
            span = (b - a) * (len(w) + 1) / total
            ws.append((w, at + t, at + t + span)); t += span
        out.append(ws)
    return out


def sfx_peak(name):
    p = PEAKS.get(name, {})
    pk = p.get("peak")
    if "sonar" in name or not isinstance(pk, (int, float)) or pk > 2.6:
        return 0.03
    return pk


# ------------------------------------------------------------------ scene files
SCENE_CSS = """
    #root { position: absolute; inset: 0; overflow: hidden; background: #05121b; }
    .cam { position: absolute; inset: 0; }
    .cam video, .cam img { position: absolute; left: 0; top: 0; width: 1920px; height: 1080px; object-fit: cover; }
    .flash { position: absolute; inset: 0; background: #ffffff; opacity: 0; pointer-events: none; }
    .stamp { position: absolute; left: 0; top: 0; width: 1920px; height: 1080px; pointer-events: none; }
    .stamp-card { position: absolute; font-family: "Archivo Black", sans-serif; text-transform: uppercase; line-height: 1;
      padding: 16px 30px 20px; border: 6px solid #05121B; border-radius: 16px; box-shadow: 9px 9px 0 #05121B;
      white-space: nowrap; transform-origin: 50% 50%; }
    .stamp-card.good { background: #3DF5E0; color: #05121B; }
    .stamp-card.bad { background: #FF4D4D; color: #ffffff; }
    .stamp-card.plain { background: #F2EFE6; color: #05121B; }
    .stamp-card.gag { background: #FFB43A; color: #05121B; }
    .stamp-card.rewind { background: rgba(5,18,27,0.78); color: #F2EFE6; border-color: #F2EFE6; box-shadow: none; letter-spacing: 0.06em; }
    .rw-tri { display: inline-block; width: 0; height: 0; border-top: 26px solid transparent; border-bottom: 26px solid transparent;
      border-right: 40px solid #F2EFE6; margin-right: 4px; vertical-align: -6px; }
"""


def scene_file(sc):
    sid = sc["id"]
    t0 = sc["t0"]
    parts, js = [], []
    z = 1
    for (cid, src, a, b, ms, rate, opt) in sc["clips"]:
        la, lb = a - t0, b - t0
        ox, oy = opt.get("cam", (960, 540))
        cam_id = cid + "-cam"
        if src.startswith("still:"):
            media = f'<img id="{cid}" class="clip" src="assets/stills/{src[6:]}" alt="" data-start="{r3(la)}" data-duration="{r3(lb - la)}" data-track-index="{z}" />'
        else:
            rate_attr = f' data-playback-rate="{r3(rate)}"' if abs(rate - 1) > 1e-6 else ""
            media = (f'<video id="{cid}" class="clip" src="assets/footage/{src}.mp4" data-start="{r3(la)}" data-duration="{r3(lb - la)}"'
                     f' data-media-start="{r3(ms)}"{rate_attr} data-track-index="{z}" muted playsinline></video>')
        parts.append(f'    <div class="cam" id="{cam_id}" style="z-index:{z}; transform-origin:{ox}px {oy}px;">\n      {media}\n    </div>')
        z += 1
        s0 = opt.get("start_scale", 1.0)
        js.append(f'tl.set("#{cam_id}", {{ scale: {s0}, x: 0, y: 0 }}, 0);')
        if opt.get("whip_in"):
            js.append(f'tl.fromTo("#{cam_id}", {{ x: 320 }}, {{ x: 0, duration: 0.2, ease: "power3.out" }}, {r3(la)});')
        if opt.get("whip_out"):
            js.append(f'tl.to("#{cam_id}", {{ x: -320, duration: 0.16, ease: "power3.in" }}, {r3(lb - 0.16)});')
        cur = s0
        for (pt, ps, pd) in opt.get("punch", []):
            js.append(f'tl.fromTo("#{cam_id}", {{ scale: {cur} }}, {{ scale: {ps}, duration: {pd}, ease: "{"power2.inOut" if pd > 1 else "power3.out"}" }}, {r3(pt - t0)});')
            cur = ps
        if "zoom_through" in opt:
            zt, zs = opt["zoom_through"]
            js.append(f'tl.fromTo("#{cam_id}", {{ scale: {cur} }}, {{ scale: {zs}, duration: {r3(lb - (zt - t0) - 0.0 - la + la)}, ease: "power3.in" }}, {r3(zt - t0)});')
        for st in opt.get("shake", []):
            lt = st - t0
            js.append(f'tl.to("#{cam_id}", {{ keyframes: [{{x: 18, y: -10, duration: 0.033}}, {{x: -15, y: 9, duration: 0.033}}, '
                      f'{{x: 11, y: -6, duration: 0.033}}, {{x: -7, y: 4, duration: 0.033}}, {{x: 3, y: -2, duration: 0.033}}, {{x: 0, y: 0, duration: 0.04}}] }}, {r3(lt)});')
    for i, ft in enumerate(sc.get("flashes", [])):
        lt = ft - t0
        fid = f"{sid}-flash{i}"
        parts.append(f'    <div class="flash clip" id="{fid}" data-start="{r3(lt)}" data-duration="0.2" data-track-index="{z}" style="z-index:{z};"><div class="flash" id="{fid}-in"></div></div>')
        js.append(f'tl.fromTo("#{fid}-in", {{ opacity: 0.85 }}, {{ opacity: 0, duration: 0.18, ease: "power2.out" }}, {r3(lt)});')
        z += 1
    for (stid, text, a, b, x, y, color, rot, size, kind) in sc.get("stamps", []):
        la, lb = a - t0, b - t0
        inner = html.escape(text)
        if kind == "rewind":
            inner = '<span class="rw-tri"></span><span class="rw-tri"></span> ' + inner
        parts.append(f'    <div class="stamp clip" id="{stid}" data-start="{r3(la)}" data-duration="{r3(lb - la)}" data-track-index="{z}" style="z-index:{z};">'
                     f'<div class="stamp-card {kind}" id="{stid}-card" style="left:{x}px; top:{y}px; font-size:{size}px;">{inner}</div></div>')
        js.append(f'tl.fromTo("#{stid}-card", {{ scale: 1.7, rotate: {rot + (8 if rot < 0 else -8)}, opacity: 0 }}, '
                  f'{{ scale: 1, rotate: {rot}, opacity: 1, duration: 0.14, ease: "expo.out" }}, {r3(la)});')
        js.append(f'tl.to("#{stid}-card", {{ scale: 0.85, opacity: 0, duration: 0.07, ease: "power3.in" }}, {r3(lb - 0.08)});')
        z += 1
    body = "\n".join(parts)
    script = "\n      ".join(js)
    return f"""<template>
  <style>{SCENE_CSS}  </style>
  <div id="root" data-composition-id="{sid}" data-width="{W}" data-height="{H}">
{body}
  </div>
  <script>
    (function () {{
      const tl = gsap.timeline({{ paused: true }});
      {script}
      window.__timelines["{sid}"] = tl;
    }})();
  </script>
</template>
"""


def captions_file():
    groups_html, js = [], []
    gi = 0
    flat = []
    for vid, groups, hi in CAPTIONS:
        for ws in word_times(vid, groups):
            flat.append((ws, hi))
    for k, (ws, hi) in enumerate(flat):
        g_start = max(0.05, ws[0][1] - 0.04)
        nxt = flat[k + 1][0][0][1] - 0.04 if k + 1 < len(flat) else ws[-1][2] + 0.5
        g_end = min(ws[-1][2] + 0.35, nxt)
        gid = f"cg{gi:02d}"
        spans = []
        for j, (w, a, b) in enumerate(ws):
            wid = f"{gid}w{j}"
            cls = "cw hi" if w in hi else "cw"
            spans.append(f'<span class="{cls}" id="{wid}">{html.escape(w)}</span>')
            js.append(f'tl.fromTo("#{wid}", {{ scale: 0.5, rotate: {-5 if j % 2 == 0 else 4}, opacity: 0 }}, '
                      f'{{ scale: 1, rotate: 0, opacity: 1, duration: 0.16, ease: "back.out(2.2)" }}, {r3(a)});')
        groups_html.append(f'    <div class="cg clip" id="{gid}" data-start="{r3(g_start)}" data-duration="{r3(g_end - g_start)}" data-track-index="1">'
                           f'<div class="cg-line" id="{gid}-line">{" ".join(spans)}</div></div>')
        js.append(f'tl.to("#{gid}-line", {{ opacity: 0, scale: 0.92, duration: 0.07, ease: "power3.in" }}, {r3(g_end - 0.08)});')
        gi += 1
    return f"""<template>
  <style>
    #root {{ position: absolute; inset: 0; pointer-events: none; }}
    .cg {{ position: absolute; left: 0; top: 0; width: 1920px; height: 1080px; overflow: visible; }}
    .cg-line {{ position: absolute; left: 0; top: 690px; width: 1920px; text-align: center; overflow: visible; }}
    .cw {{ display: inline-block; font-family: "Archivo Black", sans-serif; font-size: 104px; line-height: 1.12;
      color: #ffffff; text-transform: uppercase; letter-spacing: -0.01em; margin: 0 10px;
      -webkit-text-stroke: 12px #05121B; paint-order: stroke fill; text-shadow: 0 9px 0 #05121B; }}
    .cw.hi {{ color: #05121B; background: #FFB43A; -webkit-text-stroke: 0; text-shadow: none; padding: 4px 22px 10px;
      border: 7px solid #05121B; border-radius: 18px; box-shadow: 9px 9px 0 #05121B; }}
  </style>
  <div id="root" data-composition-id="captions" data-width="{W}" data-height="{H}">
{chr(10).join(groups_html)}
  </div>
  <script>
    (function () {{
      const tl = gsap.timeline({{ paused: true }});
      {(chr(10) + "      ").join(js)}
      window.__timelines["captions"] = tl;
    }})();
  </script>
</template>
"""


def index_file():
    hosts, audio = [], []
    ti = 1
    for sc in SCENES:
        hosts.append(f'      <div id="{sc["id"]}" data-composition-id="{sc["id"]}" data-composition-src="compositions/{sc["id"]}.html" '
                     f'data-start="{r3(sc["t0"])}" data-duration="{r3(sc["t1"] - sc["t0"])}" data-track-index="{ti}" data-track-kind="graphics" '
                     f'data-width="{W}" data-height="{H}" style="z-index:{ti};"></div>')
        ti += 1
    a, b = ENDCARD
    hosts.append(f'      <div id="endcard" data-composition-id="endcard" data-composition-src="compositions/endcard.html" data-start="{r3(a)}" '
                 f'data-duration="{r3(b - a)}" data-track-index="{ti}" data-track-kind="graphics" data-width="{W}" data-height="{H}" style="z-index:{ti};"></div>')
    ti += 1
    hosts.append(f'      <div id="captions" data-composition-id="captions" data-composition-src="compositions/captions.html" data-start="0" '
                 f'data-duration="{r3(TOTAL)}" data-track-index="{ti}" data-track-kind="captions" data-width="{W}" data-height="{H}" style="z-index:50;"></div>')
    audio.append(f'      <audio id="music" class="clip" src="assets/music/bed.wav" data-start="0" data-duration="{r3(TOTAL)}" data-volume="0.42" '
                 f'data-fade-out="1.6" data-track-index="20"></audio>')
    for vid, at in VO_AT.items():
        d = VO[vid]["dur"]
        audio.append(f'      <audio id="vo-{vid}" class="clip" src="assets/voice/norm/{vid}.wav" data-start="{r3(at)}" data-duration="{r3(d)}" '
                     f'data-volume="1" data-track-index="{21 if int(vid[1:]) % 2 else 26}"></audio>')
    for i, (f, hit, vol) in enumerate(SFX):
        st = max(0.0, hit - sfx_peak(f) + 1.0 / FPS)
        dur = PEAKS.get(f, {}).get("dur") or 1.0
        if "cinematic" not in f:
            dur = min(dur, 2.4)
        dur = min(dur, max(0.3, TOTAL - st))
        audio.append(f'      <audio id="sfx{i:02d}" class="clip" src="assets/sfx/{f}" data-start="{r3(st)}" data-duration="{r3(dur)}" '
                     f'data-volume="{vol}" data-track-index="{30 + (i % 8)}"></audio>')
    return f"""<!doctype html>
<html lang="en" data-resolution="landscape"
  data-composition-variables='[{{"id":"tagline","type":"string","label":"End-card tagline","default":"FIND THE HEART · SURVIVE THE DEEP · ESCAPE"}},{{"id":"repo","type":"string","label":"Repository link","default":"github.com/qraza1100/FinalLabGame"}}]'>
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=1920, height=1080" />
    <script src="https://cdn.jsdelivr.net/npm/gsap@3.15.0/dist/gsap.min.js"></script>
    <style>
      * {{ margin: 0; padding: 0; box-sizing: border-box; }}
      html, body {{ margin: 0; width: 1920px; height: 1080px; overflow: hidden; background: #05121b; }}
      #root {{ width: 100%; height: 100%; position: relative; }}
    </style>
  </head>
  <body>
    <div id="root" data-composition-id="main" data-start="0" data-duration="{r3(TOTAL)}" data-width="{W}" data-height="{H}" data-motion-blur="on">
{chr(10).join(hosts)}
{chr(10).join(audio)}
    </div>
    <script>
      window.__timelines = window.__timelines || {{}};
      window.__timelines["main"] = gsap.timeline({{ paused: true }});
    </script>
  </body>
</html>
"""


if __name__ == "__main__":
    os.makedirs(os.path.join(ROOT, "compositions"), exist_ok=True)
    for sc in SCENES:
        open(os.path.join(ROOT, "compositions", sc["id"] + ".html"), "w", encoding="utf-8").write(scene_file(sc))
    open(os.path.join(ROOT, "compositions", "captions.html"), "w", encoding="utf-8").write(captions_file())
    open(os.path.join(ROOT, "index.html"), "w", encoding="utf-8").write(index_file())
    print("wrote index.html, captions and", len(SCENES), "scenes; total", TOTAL, "s")
