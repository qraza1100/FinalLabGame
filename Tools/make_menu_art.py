"""Builds menu background + app icon pages from the mood board's SVG art (render them with a headless browser)."""
import re, sys, os
src = sys.argv[1]; out = sys.argv[2]
html = open(src, encoding="utf-8").read()
defs = re.search(r'(<svg width="0" height="0".*?</svg>)', html, re.S).group(1)
scene = re.search(r'(<svg class="scene".*?</svg>)', html, re.S).group(1)
script = re.search(r'(<script>.*?</script>)', html, re.S).group(1)
fonts = "@font-face{font-family:Cinzel;src:url(src/Cinzel.ttf)}@font-face{font-family:Exo2;src:url(src/Exo2.ttf)}"
bg = f"""<!doctype html><html><head><meta charset="utf-8"><style>{fonts}
html,body{{margin:0;width:1920px;height:1080px;overflow:hidden;background:#051019}}
svg.scene{{position:absolute;inset:0;width:1920px;height:1080px}}</style></head><body>{defs}{scene}{script}</body></html>"""
# shift the sub/title-free composition: keep as is (slice crop)
bg = bg.replace('<svg class="scene" viewBox="0 0 1130 600" preserveAspectRatio="xMidYMid slice">',
                '<svg class="scene" viewBox="0 0 1130 600" preserveAspectRatio="xMidYMid slice">')
open(os.path.join(out, "menubg.html"), "w", encoding="utf-8").write(bg)
icon = f"""<!doctype html><html><head><meta charset="utf-8"><style>html,body{{margin:0;width:512px;height:512px;background:transparent;overflow:hidden}}</style></head><body>{defs}
<svg width="512" height="512" viewBox="0 0 512 512"><defs><radialGradient id="ib" cx=".5" cy=".4" r=".7"><stop offset="0" stop-color="#16708a"/><stop offset="1" stop-color="#06141f"/></radialGradient></defs>
<rect x="16" y="16" width="480" height="480" rx="96" fill="url(#ib)" stroke="#3DF5E0" stroke-width="10"/>
<use href="#heart" x="76" y="70" width="360" height="360"/></svg></body></html>"""
open(os.path.join(out, "icon.html"), "w", encoding="utf-8").write(icon)
print("ok")
