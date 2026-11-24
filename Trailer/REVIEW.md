# REVIEW: The Drowned Sanctum gameplay trailer

## Creative calls
- Built storyline A, "POV: You Stole It". It's a result-first hook (the stolen Heart and the alarm), then a record scratch and VHS rewind, then one game rule per beat, then the real escape at speed, the win, the leaderboard and the end card. Not built: B "Any% Speedrun" and C "Rules of the Deep" (see STORYLINES.md).
- All gameplay is real. The built game was captured frame by frame with a scripted autopilot (`DrownedSanctum.exe -capture`), swimming BFS paths through the actual maze. Pickups, gates, hits, the escape and the win and lose screens all happened in-engine; only the camera offsets were scripted.
- Voice: local Kokoro "af_heart" at 1.1x, because HeyGen voices need a paid plan (the person chose free local). Script: Ollama qwen3.5:4b drafted six variants; the final 17 lines were rewritten by hand.
- Music: "Funky House" (CC0, Of Far Different Nature), 128 BPM, entered at its strongest section (7.5 s in). Cuts sit on the beat grid (0.469 s per beat).
- Captions: TikTok-style punch captions (Archivo Black, white with a dark stroke, one amber highlight box per group) timed from the voice. Stamps are sticker cards for game events (+10 PTS, -12 O2, NOT FRIENDLY, ...).
- Transitions used: zoom-through the DIVE IN button, whip-ins, white-flash hits with camera shake, freeze-frame (shark), the VHS rewind, and hard cuts on the beat.

## Measured vs looked at
- `hyperframes check`: 0 errors. Kept warnings:
  - `nested_media_start_basis_ambiguous`: the times are local to each scene, as intended.
  - `timeline_track_too_dense`: the captions and the 15-clip rules montage.
- Looked at about 40 snapshot frames across every beat and fixed every P0/P1 found:
  - the black crane open
  - caption sync in the hook
  - the duplicate REWIND text
  - the end-card mark overlapping the title
  - stamps near the frame edge
- Sound was measured, not heard: SFX start times were placed from their measured peaks, the voice lines were normalised to -16 LUFS, and the bed sits at -16 LUFS x 0.42 under them.
  - The raw render measured -10.9 LUFS and was normalised to -14.2 LUFS integrated (true peak -3.2 dBTP, no clipping, no silent gaps).
- The rendered file was spot-checked at 12 frames and matches the snapshots. Render time: 4 m 19 s, 1218 frames, on the integrated GPU.

## Weakest shot
- 7.3-7.8 s: the first half second of the crane shot is very dark, because the camera starts above the fog. It reads as "diving into the deep", but a brighter recapture (camera starting at 30 m instead of 42 m) would be better.

## With more time
- Recapture the crane lower, and add one more close-up angle of the shark charging.
- A second voice take with more energy, or a HeyGen voice if a plan is added.
