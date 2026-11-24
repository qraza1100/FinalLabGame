# SPEC: The Drowned Sanctum, gameplay trailer ("POV: You Stole It")

GOAL:    In ~40 s a stranger learns the whole game (air, sonar, pearls, sigils + gates, currents, urchins,
         vents, shark, the Heart, the 90 s escape, win/lose, leaderboard) and wants to play it.
OUTPUT:  HyperFrames, 1920 x 1080, 30 fps, landscape (gameplay is 16:9 and the HUD must stay readable).
TIME:    ~40 s x 30 = ~1200 frames. Music 128 BPM = 14.06 frames/beat, bar = 56.25 frames (1.875 s).
         Cuts land on beats from assets/music/bed_beats.json; the voice is the clock inside each beat.

LOOK:    Medium = real captured gameplay (Unity URP, teal fog, amber Heart, red alarm), always full-frame.
         Ink: #FFFFFF captions with a 10 px #05121B stroke (paint-order stroke) and 0 8px 0 #05121B shadow.
         Accent A #FFB43A (amber): the one highlighted caption word per group, the Heart, the end-card mark.
         Accent B #3DF5E0 (cyan): stamps for good things (+10, sigils, refill). Alarm #FF4D4D: hits, the escape.
         Display face Archivo Black (bundled), captions 92-118 px caps, tracking -1%.
         Title face Cinzel 900 (the game's own font, @font-face from assets/fonts), end card 150 px.
         Stamps: sticker cards, 6 px #05121B border, 14 px radius, rotate -5 to 6 deg, hard shadow 8px 8px 0.

OBJECTS: the brass mini-sub, the glowing Heart on its altar, sigil tablets, the portcullis gates, urchins,
         the guardian shark, air vents, pearls, the HUD (O2 bar, score, sigils, ESCAPE timer); all from the game.

MOTION:  Captions pop: scale 0.55 -> 1 with back.out(2.2) 0.16 s, rotate -4 -> 0; exit 0.07 s power3.in.
         Punch-ins: scale 1 -> 1.18-1.6 on a wrapper, 0.22 s power3.out, held still (no creeping zoom).
         Whip cut: outgoing x 0 -> -260 px power3.in 0.18 s, incoming x 260 -> 0 power3.out 0.2 s, motion blur on.
         Hits: 6-frame camera shake (+-18 px, decaying), red flash already in the footage.
         Stamps slam: scale 1.7 -> 1, rotate +/-8 -> final, 0.14 s expo.out; hold; exit 0.08 s.
         Freeze-frame: 0.45 s still with a white 3-frame flash.

SHOTS:
  1  HOOK     0.0-4.2   g13 grab of the Heart -> alarm. Caption "P.O.V." then the line. Punch-in 1.3 on the grab,
                        white flash + bass hit at the grab frame. VO v01, v02.
  2  REWIND   4.2-5.9   freeze 0.25 s + record scratch, then VHS rewind montage (baked) with REWIND stamp. VO v03.
  3  DIVE     5.9-9.6   menu: QAIM typed (typing sfx); zoom through DIVE IN into the crane-down shot. VO v04.
  4  RULES    9.6-27    one rule per beat group, each with its own move:
                        air (punch-in on the O2 bar), sonar (ring), pearls (+10 stamp), sigils -> gate (whip),
                        currents -> urchin (shake, -12 O2 stamp), vent (REFILLING), "or don't" game-over gag,
                        shark (freeze on the bite + NOT FRIENDLY), three sigils -> Heart reveal (cinematic dolly).
  5  PAYOFF   27-36     grab it (alarm) -> riser -> GO: escape at 1.6x with the drop -> ESCAPED + score -> leaderboard.
  6  END      36-40.5   game art, title slams in letter by letter, Heart mark lands with impact, GitHub + "PLAY FREE".

BANNED:  dark void with floating cards; glow/particles added on top; purple/blue gradients; glass cards;
         everything fading and sliding up; titles that drift after landing; three cards in a row;
         a logo docked in a corner; CSS filters on footage; the same whoosh twice in a row.
QA:      t=0.1 (thumbnail: footage + caption, never black), the grab flash, REWIND, DIVE IN zoom-through,
         shark freeze, escape drop, ESCAPED, end-card lockup; captions readable at 360 px wide.
