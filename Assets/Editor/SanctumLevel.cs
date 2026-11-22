using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DrownedSanctum.EditorTools
{
    public static partial class SanctumBuilder
    {
        static string[] map;
        static int MW, MH;

        static char At(int x, int row) => (x < 0 || row < 0 || x >= MW || row >= MH) ? '#' : map[row][x];
        static Vector3 CellPos(int x, int row, float y = 0f) => new Vector3(x * Cell, y, (MH - 1 - row) * Cell);
        static bool Walkable(char c) => c != '#' && c != 'K' && c != 'X';

        static string FloorZone(int x, int row)
        {
            if (x >= 11 && x <= 15 && row >= 1 && row <= 4) return "FloorChamber";       // Heart chamber
            if ((x <= 9 && row <= 13) || (x <= 5 && row >= 15)) return "FloorSand";      // flooded west wing
            if (x >= 12 && x <= 14 && row >= 6 && row <= 13) return "FloorSand";          // hidden shaft
            return "Floor";
        }

        // ------------------------------------------------------------------ mesh helpers
        class MeshBuild
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int>[] tris;
            public MeshBuild(int subMeshes) { tris = new List<int>[subMeshes]; for (int i = 0; i < subMeshes; i++) tris[i] = new List<int>(); }

            public void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, Vector2 u0, Vector2 u1, Vector2 u2, Vector2 u3, int sub)
            {
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), normal) < 0f)
                {
                    (p1, p3) = (p3, p1);
                    (u1, u3) = (u3, u1);
                }
                int b = v.Count;
                v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
                for (int i = 0; i < 4; i++) n.Add(normal);
                uv.Add(u0); uv.Add(u1); uv.Add(u2); uv.Add(u3);
                tris[sub].AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name };
                if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
                m.SetVertices(v);
                m.SetNormals(n);
                m.SetUVs(0, uv);
                m.subMeshCount = tris.Length;
                for (int i = 0; i < tris.Length; i++) m.SetTriangles(tris[i], i);
                m.RecalculateBounds();
                m.RecalculateTangents();
                return m;
            }
        }

        static Mesh SaveMesh(Mesh m)
        {
            string path = GenDir + "/Meshes/" + m.name + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static GameObject MeshObject(string name, Transform parent, Mesh mesh, params Material[] mats)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        static GameObject Place(string prefab, Transform parent, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(PF(prefab));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (Math.Abs(scale - 1f) > 0.001f) go.transform.localScale = Vector3.one * scale;
            return go;
        }

        static Dictionary<Vector2Int, int> Bfs(Vector2Int start, Func<int, int, bool> passable)
        {
            var dist = new Dictionary<Vector2Int, int> { [start] = 0 };
            var q = new Queue<Vector2Int>();
            q.Enqueue(start);
            var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var d in dirs)
                {
                    var nb = c + d;
                    if (nb.x < 0 || nb.y < 0 || nb.x >= MW || nb.y >= MH || dist.ContainsKey(nb) || !passable(nb.x, nb.y)) continue;
                    dist[nb] = dist[c] + 1;
                    q.Enqueue(nb);
                }
            }
            return dist;
        }

        // ------------------------------------------------------------------ game scene
        static void BuildGameScene()
        {
            map = ReadMap();
            MH = map.Length;
            MW = map[0].Length;
            var rng = new System.Random(42);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- atmosphere
            Color fog = new Color(0.02f, 0.075f, 0.10f);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.17f, 0.32f, 0.36f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 17f;
            RenderSettings.fogEndDistance = 40f;
            RenderSettings.fogColor = fog;

            var sun = new GameObject("Moonlight (Directional)").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.45f, 0.78f, 0.85f);
            sun.intensity = 0.45f;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(62f, -30f, 0f);

            var alarm = new GameObject("Alarm Light (Directional)").AddComponent<Light>();
            alarm.type = LightType.Directional;
            alarm.color = new Color(1f, 0.15f, 0.12f);
            alarm.intensity = 0f;
            alarm.shadows = LightShadows.None;
            alarm.transform.rotation = Quaternion.Euler(80f, 20f, 0f);

            // ---------- camera + post processing
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = fog;
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 80f;
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var follow = camGo.AddComponent<CameraFollow>();

            var mainProfile = NewProfile("Assets/Settings/Volumes/MainVolume.asset");
            var bloom = AddOverride<Bloom>(mainProfile);
            bloom.intensity.value = 0.9f;
            bloom.threshold.value = 1.0f;
            bloom.scatter.value = 0.65f;
            var vig = AddOverride<Vignette>(mainProfile);
            vig.intensity.value = 0.32f;
            vig.smoothness.value = 0.5f;
            vig.color.value = new Color(0.01f, 0.04f, 0.06f);
            var ca = AddOverride<ColorAdjustments>(mainProfile);
            ca.postExposure.value = 0.15f;
            ca.contrast.value = 12f;
            ca.saturation.value = 8f;
            ca.colorFilter.value = Color.white;
            ca.hueShift.value = 0f;
            var tm = AddOverride<Tonemapping>(mainProfile);
            tm.mode.value = TonemappingMode.Neutral;
            var vol = new GameObject("Global Volume").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = mainProfile;

            var alarmProfile = NewProfile("Assets/Settings/Volumes/AlarmVolume.asset");
            var aca = AddOverride<ColorAdjustments>(alarmProfile);
            aca.colorFilter.value = new Color(1f, 0.6f, 0.58f);
            aca.postExposure.value = 0f;
            aca.contrast.value = 18f;
            aca.saturation.value = 0f;
            aca.hueShift.value = 0f;
            var avig = AddOverride<Vignette>(alarmProfile);
            avig.intensity.value = 0.48f;
            avig.smoothness.value = 0.6f;
            avig.color.value = new Color(0.45f, 0f, 0.02f);
            var alarmVol = new GameObject("Alarm Volume").AddComponent<Volume>();
            alarmVol.isGlobal = true;
            alarmVol.priority = 10f;
            alarmVol.weight = 0f;
            alarmVol.sharedProfile = alarmProfile;

            // ---------- managers + audio
            var mgr = new GameObject("GameManager");
            var gm = mgr.AddComponent<GameManager>();
            gm.settings = Load<GameSettings>(SettingsPath);
            gm.particleMaterial = M["Particle"];
            var esc = mgr.AddComponent<EscapeSequence>();
            var auto = mgr.AddComponent<DevAutoTest>();
            var sfx = BuildAudio(mgr.transform, true);

            // ---------- level geometry
            var level = new GameObject("Level").transform;
            var geo = Child("Geometry", level).transform;
            BuildFloorAndWalls(geo, rng);

            var objects = Child("Objects", level).transform;
            var decor = Child("Decor", level).transform;
            PlayerController player = null;
            var gates = new Gate[3];
            var sigils = new Pickup[3];
            Pickup heart = null;
            Gate exitGate = null;
            var cracked = new List<CrackedWall>();
            Vector2Int pCell = Vector2Int.zero, eCell = Vector2Int.zero, hCell = Vector2Int.zero;
            var gateCells = new Vector2Int[3];
            var sharkCells = new List<Vector2Int>();
            var ventCells = new List<Vector2Int>();
            var urchinCells = new List<Vector2Int>();
            var currentCells = new List<Vector2Int>();
            var crackCells = new List<Vector2Int>();
            string[] sigilNames = { "Moon", "Sun", "Star" };

            for (int row = 0; row < MH; row++)
            for (int x = 0; x < MW; x++)
            {
                char c = map[row][x];
                Vector3 p = CellPos(x, row);
                switch (c)
                {
                    case 'P':
                        pCell = new Vector2Int(x, row);
                        player = Place("Player", objects, p + Vector3.up * PlayerY).GetComponent<PlayerController>();
                        break;
                    case 'o': Place("Pearl", objects, p); break;
                    case '1': case '2': case '3':
                    {
                        int i = c - '1';
                        sigils[i] = Place("Sigil_" + sigilNames[i], objects, p).GetComponent<Pickup>();
                        break;
                    }
                    case 'H':
                        hCell = new Vector2Int(x, row);
                        heart = Place("HeartOfEternity", objects, p).GetComponent<Pickup>();
                        break;
                    case 'V': ventCells.Add(new Vector2Int(x, row)); Place("AirVent", objects, p); break;
                    case 'U': urchinCells.Add(new Vector2Int(x, row)); Place("Urchin", objects, p, (float)rng.NextDouble() * 360f); break;
                    case '^': case '>': case 'v': case '<':
                    {
                        float yaw = c == '^' ? 0f : c == '>' ? 90f : c == 'v' ? 180f : 270f;
                        currentCells.Add(new Vector2Int(x, row));
                        Place("Current", objects, p, yaw);
                        break;
                    }
                    case 'S': sharkCells.Add(new Vector2Int(x, row)); break;
                    case 'a': case 'b': case 'c':
                    {
                        int i = c - 'a';
                        gateCells[i] = new Vector2Int(x, row);
                        gates[i] = BuildGate(objects, x, row, sigilNames[i] + " Gate", i, false, i == 2, M["Emblem" + sigilNames[i]], CyanC);
                        break;
                    }
                    case 'E':
                        eCell = new Vector2Int(x, row);
                        exitGate = BuildGate(objects, x, row, "Sanctum Gate", -1, true, false, M["EmblemHeart"], AmberC);
                        break;
                    case 'X':
                        crackCells.Add(new Vector2Int(x, row));
                        cracked.Add(BuildCrackedWall(objects, x, row));
                        break;
                    case 'K': Place("Column", decor, p, rng.Next(4) * 90f); break;
                    case 'w': Place("Seaweed", decor, p + Jitter(rng, 0.6f), (float)rng.NextDouble() * 360f, 0.9f + (float)rng.NextDouble() * 0.5f); break;
                    case 'k': Place("Coral", decor, p + Jitter(rng, 0.6f), (float)rng.NextDouble() * 360f, 0.9f + (float)rng.NextDouble() * 0.5f); break;
                    case 'r': Place("Rocks", decor, p + Jitter(rng, 0.5f), (float)rng.NextDouble() * 360f); break;
                }
            }

            // sharks: patrol the longest straight run through their cell
            foreach (var s in sharkCells)
            {
                var (a, b) = LongestRun(s);
                var shark = Place("Shark", objects, CellPos(a.x, a.y, PlayerY + 0.2f)).GetComponent<SharkPatrol>();
                shark.pointA = CellPos(a.x, a.y, PlayerY + 0.2f);
                shark.pointB = CellPos(b.x, b.y, PlayerY + 0.2f);
            }

            ScatterDecor(decor, rng);
            WallGlyphs(geo, rng);

            // exit: win zone + light from the outside world
            if (exitGate)
            {
                Vector3 ep = CellPos(eCell.x, eCell.y);
                var exitZone = Child("ExitZone", objects.transform, ep + new Vector3(0f, 1.2f, -Cell * 0.3f));
                var ez = exitZone.AddComponent<BoxCollider>();
                ez.isTrigger = true;
                ez.size = new Vector3(Cell * 0.8f, 2.4f, Cell * 0.4f);
                exitZone.AddComponent<ExitZone>();
                AddLight(objects, "Outside Light", LightType.Point, new Color(0.7f, 1f, 0.95f), 3f, 8f, ep + new Vector3(0f, 1.5f, -Cell * 0.9f));
                Quad("Outside Seabed", objects, ep + new Vector3(0f, 0.01f, -Cell * 1.5f), Quaternion.Euler(90f, 0f, 0f), new Vector2(Cell * 3f, Cell * 2f), M["FloorSand"]);
                Halo(objects, ep + new Vector3(0f, 1.2f, -Cell * 0.8f), 5f, M["ExitGlow"]);
            }

            // ambient fish
            var hallSchool = Child("Fish School (Hall)", decor, CellPos(pCell.x, pCell.y - 1)).AddComponent<FishSchool>();
            hallSchool.radius = 4.5f;
            hallSchool.height = 2.1f;
            hallSchool.fish = new[] { "Fish1", "Fish2", "Fish1", "Fish3", "Fish2" }.Select(n => Place(n, hallSchool.transform, hallSchool.transform.position).transform).ToArray();
            var heartSchool = Child("Manta (Heart Chamber)", decor, CellPos(hCell.x, hCell.y + 1)).AddComponent<FishSchool>();
            heartSchool.radius = 2.8f;
            heartSchool.height = 2.6f;
            heartSchool.speed = 0.35f;
            heartSchool.fish = new[] { Place("MantaRay", heartSchool.transform, heartSchool.transform.position).transform };

            // marine snow around the camera
            var snow = AddParticles(camGo.transform, "Marine Snow", new Vector3(0f, -6f, 12f));
            FxUtil.ConfigureLoop(snow, M["Particle"], new Color(0.75f, 0.95f, 0.95f, 0.35f), 30f, 6f, 0.15f, 0.08f,
                                 ParticleSystemShapeType.Box, 0f, new Vector3(30f, 6f, 22f), -0.01f, ParticleSystemSimulationSpace.World, 400);

            // ---------- wiring
            follow.target = player ? player.transform : null;
            if (player) camGo.transform.position = player.transform.position + follow.offset;
            camGo.transform.rotation = Quaternion.LookRotation(-follow.offset.normalized, Vector3.up);

            gm.player = player;
            gm.cameraFollow = follow;
            gm.escape = esc;
            gm.sigilPickups = sigils;
            gm.sigilGates = gates;
            gm.heartPickup = heart;
            gm.exitGate = exitGate;

            esc.crackedWalls = cracked.ToArray();
            esc.gatesToClose = gates[2] ? new[] { gates[2] } : new Gate[0];
            esc.alarmLight = alarm;
            esc.alarmVolume = alarmVol;
            esc.mainCamera = cam;
            esc.music = sfx.musicSource;

            var hud = BuildHUD();
            gm.hud = hud;

            // ---------- autotest points
            var names = new List<string>();
            var pts = new List<Vector3>();
            void AddPt(string n, Vector2Int cell, Vector3 offset = default) { names.Add(n); pts.Add(CellPos(cell.x, cell.y, PlayerY) + offset); }
            for (int i = 0; i < 3; i++) AddPt("gate" + i + "_near", ApproachCell(pCell, gateCells[i]));
            AddPt("exit_near", ApproachCell(pCell, eCell));
            AddPt("exit_out", eCell, new Vector3(0f, 0f, -Cell * 0.3f));
            AddPt("heart_near", new Vector2Int(hCell.x, hCell.y + 1));
            if (sharkCells.Count > 0) { var (a, b) = LongestRun(sharkCells[0]); AddPt("shark_lane", Vector2Int.RoundToInt(Vector2.Lerp(a, b, 0.45f))); }
            if (currentCells.Count > 0) AddPt("west_view", currentCells[0] + new Vector2Int(-1, 0));
            if (crackCells.Count >= 2) AddPt("shaft", Vector2Int.RoundToInt(Vector2.Lerp(crackCells[0], crackCells[1], 0.5f)));
            if (ventCells.Count > 0) AddPt("vent", ventCells.OrderBy(v => (v - pCell).sqrMagnitude).First());
            if (urchinCells.Count > 0) AddPt("urchin", urchinCells.OrderBy(v => (v - pCell).sqrMagnitude).First());
            auto.pointNames = names.ToArray();
            auto.points = pts.ToArray();

            EditorSceneManager.SaveScene(scene, GameScene);
        }

        static Vector3 Jitter(System.Random rng, float amount) =>
            new Vector3(((float)rng.NextDouble() - 0.5f) * amount, 0f, ((float)rng.NextDouble() - 0.5f) * amount);

        static (Vector2Int, Vector2Int) LongestRun(Vector2Int s)
        {
            Vector2Int l = s, r = s, u = s, d = s;
            while (Walkable(At(l.x - 1, l.y)) && !IsGateChar(At(l.x - 1, l.y))) l.x--;
            while (Walkable(At(r.x + 1, r.y)) && !IsGateChar(At(r.x + 1, r.y))) r.x++;
            while (Walkable(At(u.x, u.y - 1)) && !IsGateChar(At(u.x, u.y - 1))) u.y--;
            while (Walkable(At(d.x, d.y + 1)) && !IsGateChar(At(d.x, d.y + 1))) d.y++;
            return (r.x - l.x) >= (d.y - u.y) ? (l, r) : (u, d);
        }

        static bool IsGateChar(char c) => c == 'a' || c == 'b' || c == 'c' || c == 'E';

        static Vector2Int ApproachCell(Vector2Int start, Vector2Int gate)
        {
            var dist = Bfs(start, (x, y) => Walkable(At(x, y)) && !(x == gate.x && y == gate.y));
            Vector2Int best = gate + Vector2Int.up;
            int bestD = int.MaxValue;
            foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
            {
                var nb = gate + d;
                if (dist.TryGetValue(nb, out int dd) && dd < bestD) { bestD = dd; best = nb; }
            }
            return best;
        }

        // ------------------------------------------------------------------ geometry
        static void BuildFloorAndWalls(Transform parent, System.Random rng)
        {
            var floors = new Dictionary<string, MeshBuild> { ["Floor"] = new MeshBuild(1), ["FloorSand"] = new MeshBuild(1), ["FloorChamber"] = new MeshBuild(1) };
            var walls = new MeshBuild(2);
            Vector2[] uvRot = { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };

            for (int row = 0; row < MH; row++)
            for (int x = 0; x < MW; x++)
            {
                char c = map[row][x];
                Vector3 o = CellPos(x, row) - new Vector3(Cell / 2f, 0f, Cell / 2f);
                Vector3 p00 = o, p01 = o + new Vector3(0, 0, Cell), p11 = o + new Vector3(Cell, 0, Cell), p10 = o + new Vector3(Cell, 0, 0);
                if (c != '#')
                {
                    int r = rng.Next(4);
                    floors[FloorZone(x, row)].Quad(p00, p01, p11, p10, Vector3.up, uvRot[r], uvRot[(r + 1) % 4], uvRot[(r + 2) % 4], uvRot[(r + 3) % 4], 0);
                    continue;
                }
                Vector3 up = Vector3.up * WallH;
                walls.Quad(p00 + up, p01 + up, p11 + up, p10 + up, Vector3.up, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), 1);
                float vH = WallH / Cell;
                // east (+x), west (-x), north (+z), south (-z) faces where the neighbour is open
                if (x + 1 < MW && At(x + 1, row) != '#')
                    walls.Quad(p10, p10 + up, p11 + up, p11, Vector3.right, new Vector2(0, 0), new Vector2(0, vH), new Vector2(1, vH), new Vector2(1, 0), 0);
                if (x - 1 >= 0 && At(x - 1, row) != '#')
                    walls.Quad(p01, p01 + up, p00 + up, p00, Vector3.left, new Vector2(0, 0), new Vector2(0, vH), new Vector2(1, vH), new Vector2(1, 0), 0);
                if (row - 1 >= 0 && At(x, row - 1) != '#')
                    walls.Quad(p11, p11 + up, p01 + up, p01, Vector3.forward, new Vector2(0, 0), new Vector2(0, vH), new Vector2(1, vH), new Vector2(1, 0), 0);
                if (row + 1 < MH && At(x, row + 1) != '#')
                    walls.Quad(p00, p00 + up, p10 + up, p10, Vector3.back, new Vector2(0, 0), new Vector2(0, vH), new Vector2(1, vH), new Vector2(1, 0), 0);
            }

            foreach (var kv in floors)
                if (kv.Value.v.Count > 0)
                    MeshObject(kv.Key, parent, SaveMesh(kv.Value.ToMesh("Level_" + kv.Key)), M[kv.Key]);
            MeshObject("Walls", parent, SaveMesh(walls.ToMesh("Level_Walls")), M["Wall"], M["WallTop"]);

            // colliders: merge horizontal runs of wall cells
            var colRoot = Child("Wall Colliders", parent);
            for (int row = 0; row < MH; row++)
            {
                int x = 0;
                while (x < MW)
                {
                    if (map[row][x] != '#') { x++; continue; }
                    int start = x;
                    while (x < MW && map[row][x] == '#') x++;
                    int len = x - start;
                    var bc = colRoot.AddComponent<BoxCollider>();
                    Vector3 a = CellPos(start, row), b = CellPos(x - 1, row);
                    bc.center = (a + b) / 2f + Vector3.up * (WallH / 2f);
                    bc.size = new Vector3(len * Cell, WallH, Cell);
                }
            }
            var floorCol = Child("Floor Collider", parent).AddComponent<BoxCollider>();
            floorCol.center = new Vector3((MW - 1) * Cell / 2f, -0.25f, (MH - 1) * Cell / 2f);
            floorCol.size = new Vector3(MW * Cell, 0.5f, MH * Cell);
        }

        static Gate BuildGate(Transform parent, int x, int row, string gateName, int sigil, bool needsHeart, bool closeOnEscape, Material emblem, Color glow)
        {
            bool spanX = At(x - 1, row) == '#' && At(x + 1, row) == '#';
            var go = Place("Gate", parent, CellPos(x, row), spanX ? 0f : 90f);
            go.name = gateName;
            var g = go.GetComponent<Gate>();
            g.gateName = gateName;
            g.requiredSigil = sigil;
            g.requiresHeart = needsHeart;
            g.closeOnEscape = closeOnEscape;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                if (r.name.StartsWith("Emblem")) r.sharedMaterial = emblem;
            if (g.glowLight) g.glowLight.color = glow;
            return g;
        }

        static CrackedWall BuildCrackedWall(Transform parent, int x, int row)
        {
            var root = Child("Cracked Wall", parent, CellPos(x, row));
            var cube = Prim(PrimitiveType.Cube, "Wall", root.transform, new Vector3(0f, WallH / 2f, 0f), new Vector3(Cell, WallH, Cell), M["WallCracked"], true);
            var renderers = new List<Renderer> { cube.GetComponent<Renderer>() };
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                var q = Quad("Cracks", root.transform, d * (Cell / 2f + 0.02f) + Vector3.up * 1.3f, Quaternion.LookRotation(d), new Vector2(2.2f, 2.2f), M["Cracks"]);
                renderers.Add(q.GetComponent<Renderer>());
            }
            var top = Quad("CracksTop", root.transform, Vector3.up * (WallH + 0.02f), Quaternion.Euler(90f, 0f, 0f), new Vector2(2.6f, 2.6f), M["Cracks"]);
            renderers.Add(top.GetComponent<Renderer>());
            var cw = root.AddComponent<CrackedWall>();
            cw.renderers = renderers.ToArray();
            cw.wallCollider = cube.GetComponent<Collider>();
            cw.debrisMaterial = M["Debris"];
            cw.cellSize = Cell;
            cw.wallHeight = WallH;
            return cw;
        }

        static void ScatterDecor(Transform parent, System.Random rng)
        {
            string[] small = { "Seaweed", "Seaweed", "Stones", "Rocks", "Pot", "Coral", "Barrel" };
            for (int row = 1; row < MH - 1; row++)
            for (int x = 1; x < MW - 1; x++)
            {
                if (map[row][x] != '.') continue;
                var open = new List<Vector2Int>();
                Vector3 towardWall = Vector3.zero;
                if (At(x + 1, row) == '#') towardWall += Vector3.right;
                if (At(x - 1, row) == '#') towardWall += Vector3.left;
                if (At(x, row - 1) == '#') towardWall += Vector3.forward;
                if (At(x, row + 1) == '#') towardWall += Vector3.back;
                if (towardWall == Vector3.zero || rng.NextDouble() > 0.16) continue;
                string pick = small[rng.Next(small.Length)];
                Vector3 pos = CellPos(x, row) + towardWall.normalized * (Cell * 0.36f) + Jitter(rng, 0.4f);
                Place(pick, parent, pos, (float)rng.NextDouble() * 360f, 0.8f + (float)rng.NextDouble() * 0.4f);
            }
        }

        static void WallGlyphs(Transform parent, System.Random rng)
        {
            var root = Child("Wall Glyphs", parent).transform;
            string[] mats = { "WallGlyphMoon", "WallGlyphSun", "WallGlyphStar" };
            var dirs = new[] { (Vector2Int.right, Vector3.right), (Vector2Int.left, Vector3.left), (new Vector2Int(0, -1), Vector3.forward), (new Vector2Int(0, 1), Vector3.back) };
            for (int row = 0; row < MH; row++)
            for (int x = 0; x < MW; x++)
            {
                if (map[row][x] != '#') continue;
                foreach (var (d, n) in dirs)
                {
                    char nb = At(x + d.x, row + d.y);
                    if (nb == '#' || x + d.x < 0 || row + d.y < 0 || x + d.x >= MW || row + d.y >= MH) continue;
                    if (rng.NextDouble() > 0.07) continue;
                    Vector3 pos = CellPos(x, row) + n * (Cell / 2f + 0.03f) + Vector3.up * 1.75f;
                    Quad("Glyph", root, pos, Quaternion.LookRotation(n), new Vector2(0.55f, 0.55f), M[mats[rng.Next(mats.Length)]]);
                }
            }
        }

        // ------------------------------------------------------------------ volumes
        static VolumeProfile NewProfile(string path)
        {
            AssetDatabase.DeleteAsset(path);
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);
            return p;
        }

        static T AddOverride<T>(VolumeProfile p) where T : VolumeComponent
        {
            var c = p.Add<T>(true);
            c.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(c, p);
            EditorUtility.SetDirty(p);
            return c;
        }

        // ------------------------------------------------------------------ audio
        static SFX BuildAudio(Transform parent, bool game)
        {
            var go = Child("Audio", parent);
            var sfx = go.AddComponent<SFX>();
            sfx.sfxSource = go.AddComponent<AudioSource>();
            sfx.sfxSource.playOnAwake = false;
            sfx.musicSource = go.AddComponent<AudioSource>();
            sfx.musicSource.clip = Load<AudioClip>("Assets/Audio/Music/Aquaria_cynicmusic.mp3");
            sfx.musicSource.loop = true;
            sfx.musicSource.playOnAwake = true;
            sfx.ambientSource = go.AddComponent<AudioSource>();
            sfx.ambientSource.clip = Load<AudioClip>("Assets/Audio/Music/UnderwaterPad_isaiah658.ogg");
            sfx.ambientSource.loop = true;
            sfx.ambientSource.playOnAwake = true;
            sfx.musicBaseVolume = game ? 0.45f : 0.6f;
            sfx.ambientBaseVolume = game ? 0.5f : 0.3f;
            sfx.click = Clip("ui_click"); sfx.hover = Clip("ui_hover"); sfx.pearl = Clip("pickup_pearl"); sfx.sigil = Clip("pickup_sigil");
            sfx.heart = Clip("pickup_heart"); sfx.denied = Clip("denied"); sfx.win = Clip("win"); sfx.lose = Clip("lose"); sfx.tick = Clip("tick");
            sfx.gateOpen = Clip("gate_open"); sfx.gateClose = Clip("gate_close"); sfx.vent = Clip("vent"); sfx.hit = Clip("hit");
            sfx.collapse = Clip("collapse"); sfx.sonar = Clip("sonar"); sfx.boost = Clip("boost");
            return sfx;
        }
    }
}
