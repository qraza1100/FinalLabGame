import json, sys
from collections import deque
rows = [l.rstrip("\r\n") for l in open(sys.argv[1], encoding="utf-8") if l.strip() and not l.startswith(";")]
H, W = len(rows), len(rows[0])
def walk(c, x, y, gates_open, x_open):
    if c in "#KU": return False
    if c == "X": return x_open
    if c in "abcE": return c in gates_open
    return True
def bfs(a, b, gates_open="", x_open=False):
    prev = {a: None}; q = deque([a])
    while q:
        cur = q.popleft()
        if cur == b: break
        for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
            n = (cur[0]+dx, cur[1]+dy)
            if 0 <= n[0] < W and 0 <= n[1] < H and n not in prev and (n == b or walk(rows[n[1]][n[0]], n[0], n[1], gates_open, x_open)):
                prev[n] = cur; q.append(n)
    assert b in prev, (a, b)
    p = [b]
    while prev[p[-1]] is not None: p.append(prev[p[-1]])
    return p[::-1]
def corners(p):
    out = [p[0]]
    for i in range(1, len(p)-1):
        d1 = (p[i][0]-p[i-1][0], p[i][1]-p[i-1][1]); d2 = (p[i+1][0]-p[i][0], p[i+1][1]-p[i][1])
        if d1 != d2: out.append(p[i])
    out.append(p[-1]); return out
def world(c): return [round(c[0]*3.0, 2), round((H-1-c[1])*3.0, 2)]
def route(*legs, gates="", x=False):
    cells = []
    for a, b in zip(legs, legs[1:]):
        seg = bfs(a, b, gates, x)
        cells += seg if not cells else seg[1:]
    pts = []
    for c in corners(cells): pts += world(c)
    return pts, len(cells)
shots = []
def S(**kw): shots.append(kw)
S(name="m01_menu_type", action="menu_type", text="QAIM", duration=2.4)
S(name="dive", action="menu_dive", text="QAIM", record=False)
S(name="g01_intro", action="intro", duration=1.6)
p, n = route((13,18), (13,18)); S(name="g02_crane", action="swim", path=p, duration=3.2, tail=3.2, hud=False, camA=[0,42,-16], camB=[0,15,-8], oxygen=100)
p, n = route((13,18), (10,15), (8,16)); S(name="g03_sonar_pearl_sigil", action="swim", path=p, sonarAt=0.4, duration=n*0.6+2.5, tail=1.0, oxygen=100)
p, n = route((9,17), (4,14), gates="a"); S(name="g04_moon_gate", action="swim", path=p, duration=n*0.6+2.5, tail=0.5, camA=[0,15,-8], camB=[0,11,-6], oxygen=95)
p, n = route((9,5), (2,5)); S(name="g05_current", action="swim", path=p, duration=5.0, tail=0.5, oxygen=90)
p, n = route((5,7), (6,7)); p += [18.0, 36.0]; S(name="g06_urchin", action="swim", path=p, duration=2.6, tail=1.0, camA=[0,13,-7], camB=[0,10,-5.5], oxygen=72)
p, n = route((6,7), (4,7)); S(name="g07_vent", action="swim", path=p, duration=4.0, tail=2.6, camA=[0,12,-6.5], camB=[0,9.5,-5], oxygen=28)
p, n = route((3,2), (1,1)); S(name="g08_sun_sigil", action="swim", path=p, duration=n*0.6+1.8, tail=1.0, oxygen=90)
p, n = route((17,17), (23,15), gates="ab"); S(name="g09_sun_gate", action="swim", path=p, duration=n*0.6+2.5, tail=0.4, oxygen=90)
p, n = route((23,6), (23,5), (18,5)); S(name="g10_shark", action="swim", path=p, duration=4.2, tail=0.6, camA=[0,13,-7], camB=[0,15,-8], oxygen=95)
p, n = route((25,3), (25,1)); S(name="g11_star_sigil", action="swim", path=p, duration=2.6, tail=1.0, oxygen=90)
p, n = route((17,3), (15,2), gates="abc"); S(name="g12_heart_reveal", action="swim", path=p, duration=4.4, tail=1.6, hud=False, camA=[0,13,-9], camB=[-3.5,6.0,-7.5], oxygen=90)
p, n = route((15,2), (13,2)); S(name="g13_take_heart", action="swim", path=p, duration=5.2, tail=3.6, camA=[0,13,-6.5], camB=[0,20,-10], oxygen=85)
p, n = route((13,3), (13,17), gates="ab", x=True); S(name="g14_escape", action="swim", path=p, boost=True, duration=9.0, tail=0.4, oxygen=80)
p, n = route((13,17), (13,20), gates="abE"); p += [39.0, -1.6]; S(name="g15_exit_win", action="swim", path=p, boost=True, duration=6.5, tail=3.2, oxygen=70)
S(name="restart", action="restart", record=False)
p, n = route((11,17), (11,17)); S(name="g16_lose", action="swim", path=p, duration=3.6, tail=3.6, oxygen=2.5)
S(name="menu", action="to_menu", record=False)
S(name="m02_board", action="board", duration=2.2)
S(name="m03_profile", action="profile", duration=1.8)
json.dump({"fps": 30, "quality": 92, "shots": shots}, open(sys.argv[2], "w"), indent=1)
print(len(shots), "shots; recorded seconds ~", round(sum(s.get("duration", 0) for s in shots if s.get("record", True)), 1))
for s in shots: print(" ", s["name"], s.get("action"), "pts", len(s.get("path", []))//2, "dur", s.get("duration"))
