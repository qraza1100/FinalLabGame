# The Drowned Sanctum: gameplay trailer (HyperFrames)

The finished video is [`../Documentation/DrownedSanctum_Trailer.mp4`](../Documentation/DrownedSanctum_Trailer.mp4): 40.6 s, 1920×1080, 30 fps, −14 LUFS.

It's a funky-house, cut-on-the-beat edit with a "POV: you just stole the Heart" hook, a rewind gag, one game rule per beat (air, sonar, pearls, sigils + gates, currents, urchins, vents, the game-over gag, the shark), then the real escape, the win, the leaderboard and an end card. Story choices are in `STORYLINES.md`, the brief is in `SPEC.md`, the review notes are in `REVIEW.md`, and credits are in `CREDITS.md`.

## Rebuild it

1. **Capture real gameplay.** The game's autopilot plays a scripted run and saves every frame:
   ```
   python tools/make_shotlist.py ../Assets/Level/level_map.txt tools/shotlist.json
   ..\Build\DrownedSanctum.exe -capture tools\shotlist.json -captureOut <frames-folder> -screen-fullscreen 0 -popupwindow -screen-width 1920 -screen-height 1080
   ```
   Then encode each shot folder to `assets/footage/<shot>.mp4`, for example `ffmpeg -framerate 30 -i <shot>/f_%05d.jpg -c:v libx264 -crf 15 -pix_fmt yuv420p -g 6 assets/footage/<shot>.mp4`. The VHS clip `rewind_vhs.mp4` is built from reversed frames of several shots.
2. **Voice:** `npx hyperframes tts "<line>" -v af_heart -s 1.1 -o assets/voice/vNN.wav` (lines in `assets/voice/lines.tsv`), then loudness-normalise into `assets/voice/norm/`.
3. **Edit:** `python tools/build_trailer.py` writes `index.html` and `compositions/*.html` from the edit table at the top of that script. Change a time there and re-run to retime the film. `compositions/endcard.html` is hand-written.
4. **Check and render:** `npx hyperframes check`, `npx hyperframes preview`, then `npx hyperframes render -q delivery`.

`assets/footage/` and `renders/` are git-ignored (large and regenerable).
