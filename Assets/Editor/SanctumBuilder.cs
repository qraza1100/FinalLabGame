using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DrownedSanctum.EditorTools
{
    /// <summary>
    /// Builds the whole game from scripts: project settings, URP, materials, prefabs,
    /// the main menu, the game level (read from Assets/Level/level_map.txt) and the Windows build.
    /// Menu: Drowned Sanctum > ...
    /// </summary>
    public static partial class SanctumBuilder
    {
        public const float Cell = 3f;
        public const float WallH = 2.6f;
        public const float PlayerY = 0.9f;

        const string MapPath = "Assets/Level/level_map.txt";
        const string MatDir = "Assets/Materials";
        const string PrefabDir = "Assets/Prefabs";
        const string GenDir = "Assets/Generated";
        const string SceneDir = "Assets/Scenes";
        const string MenuScene = "Assets/Scenes/MainMenu.unity";
        const string GameScene = "Assets/Scenes/Game.unity";
        const string SettingsPath = "Assets/Settings/GameSettings.asset";

        static readonly Dictionary<string, Material> M = new Dictionary<string, Material>();

        // ------------------------------------------------------------------ menu items
        [MenuItem("Drowned Sanctum/1. Build Everything (settings, prefabs, scenes)", priority = 1)]
        public static void BuildAll()
        {
            EnsureFolders();
            ConfigureProject();
            ConfigureImporters();
            SetupRenderPipeline();
            CreateSettingsAsset();
            CreateMaterials();
            CreatePrefabs();
            BuildMenuScene();
            BuildGameScene();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Builder] Build Everything finished.");
        }

        [MenuItem("Drowned Sanctum/2. Build Windows EXE", priority = 2)]
        public static void BuildExeMenu()
        {
            bool ok = BuildExe();
            EditorUtility.DisplayDialog("Drowned Sanctum", ok ? "Build succeeded: Build/DrownedSanctum.exe" : "Build failed - see the Console.", "OK");
        }

        [MenuItem("Drowned Sanctum/Open Level Map", priority = 20)]
        public static void OpenMap()
        {
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(MapPath));
        }

        [MenuItem("Drowned Sanctum/Edit Game Settings", priority = 21)]
        public static void SelectSettings()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
        }

        /// <summary>Batch-mode entry: -executeMethod DrownedSanctum.EditorTools.SanctumBuilder.BatchBuild</summary>
        public static void BatchBuild()
        {
            int code = 0;
            try
            {
                BuildAll();
                if (!Environment.GetCommandLineArgs().Contains("-noexe") && !BuildExe()) code = 2;
            }
            catch (Exception e)
            {
                Debug.LogError("[Builder] FAILED: " + e);
                code = 1;
            }
            Debug.Log("[Builder] exit code " + code);
            EditorApplication.Exit(code);
        }

        /// <summary>Batch-mode entry that only builds the exe from the scenes as they are (keeps hand edits).</summary>
        public static void BatchBuildExe()
        {
            int code = 0;
            try
            {
                if (!BuildExe()) code = 2;
            }
            catch (Exception e)
            {
                Debug.LogError("[Builder] FAILED: " + e);
                code = 1;
            }
            Debug.Log("[Builder] exit code " + code);
            EditorApplication.Exit(code);
        }

        public static bool BuildExe()
        {
            SetBuildScenes();
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build"));
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { MenuScene, GameScene },
                locationPathName = Path.Combine(outDir, "DrownedSanctum.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            foreach (var junk in Directory.GetDirectories(outDir, "*DoNotShip*"))
            {
                try { Directory.Delete(junk, true); } catch (Exception) { }
            }
            Debug.Log("[Builder] Build " + s.result + "  size=" + (s.totalSize / (1024 * 1024)) + "MB  errors=" + s.totalErrors + "  time=" + s.totalTime);
            return s.result == BuildResult.Succeeded;
        }

        // ------------------------------------------------------------------ project
        static void EnsureFolders()
        {
            foreach (var d in new[] { "Assets/Settings", "Assets/Settings/URP", "Assets/Settings/Volumes", MatDir, PrefabDir, GenDir, GenDir + "/Meshes", GenDir + "/Animators", SceneDir })
                if (!AssetDatabase.IsValidFolder(d))
                {
                    string parent = Path.GetDirectoryName(d).Replace('\\', '/');
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(d));
                }
        }

        static void ConfigureProject()
        {
            PlayerSettings.companyName = "FGDLab";
            PlayerSettings.productName = "The Drowned Sanctum";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/app_icon.png");
            if (icon) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            // Old Input Manager (Input.GetAxis) - keeps the code simple and readable.
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (ps != null)
            {
                var so = new SerializedObject(ps);
                var p = so.FindProperty("activeInputHandler");
                if (p != null && p.intValue != 0) { p.intValue = 0; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.vSyncCount = 1;
            }
        }

        static void ConfigureImporters()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string file = Path.GetFileNameWithoutExtension(path);
                bool sprite = file.StartsWith("ui_") || file.StartsWith("glyph_") || file == "menu_bg";
                ti.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
                if (sprite) ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = !sprite;
                ti.wrapMode = file == "fx_chevrons" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                ti.sRGBTexture = true;
                if (file == "ui_panel" || file == "ui_panel_outline") ti.spriteBorder = new Vector4(30, 30, 30, 30);
                else if (file == "ui_pill" || file == "ui_pill_outline") ti.spriteBorder = new Vector4(31, 31, 31, 31);
                else if (file == "ui_bar") ti.spriteBorder = new Vector4(10, 10, 10, 10);
                if (file == "menu_bg") { ti.maxTextureSize = 2048; ti.textureCompression = TextureImporterCompression.CompressedHQ; }
                ti.SaveAndReimport();
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ThirdParty/ambientCG" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                if (path.Contains("_NormalGL")) ti.textureType = TextureImporterType.NormalMap;
                if (path.Contains("_AmbientOcclusion")) ti.sRGBTexture = false;
                ti.maxTextureSize = 1024;
                ti.SaveAndReimport();
            }
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ai = AssetImporter.GetAtPath(path) as AudioImporter;
                if (ai == null) continue;
                var s = ai.defaultSampleSettings;
                bool music = path.Contains("/Music/");
                s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = music ? 0.6f : 0.8f;
                ai.defaultSampleSettings = s;
                ai.loadInBackground = music;
                ai.SaveAndReimport();
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models", "Assets/ThirdParty" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null) continue;
                bool fish = path.Contains("/Quaternius/");
                mi.importCameras = false;
                mi.importLights = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.importAnimation = fish;
                mi.animationType = fish ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
                mi.SaveAndReimport();
                if (fish)
                {
                    var clips = mi.defaultClipAnimations;
                    foreach (var c in clips) c.loopTime = true;
                    mi.clipAnimations = clips;
                    mi.SaveAndReimport();
                }
            }
        }

        static void SetupRenderPipeline()
        {
            const string path = "Assets/Settings/URP/SanctumURP.asset";
            const string rdPath = "Assets/Settings/URP/SanctumURP_Renderer.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create();
                AssetDatabase.CreateAsset(asset, path);
                asset.LoadBuiltinRendererData(RendererType.UniversalRenderer); // creates Assets/UniversalRenderer.asset
                AssetDatabase.SaveAssets();
                AssetDatabase.MoveAsset("Assets/UniversalRenderer.asset", rdPath);
            }
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rdPath);
            if (rd != null)
            {
                rd.renderingMode = RenderingMode.ForwardPlus;
                EditorUtility.SetDirty(rd);
            }
            asset.supportsHDR = true;
            asset.msaaSampleCount = 1;
            asset.renderScale = 1f;
            asset.shadowDistance = 30f;
            EditorUtility.SetDirty(asset);
            GraphicsSettings.defaultRenderPipeline = asset;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            try
            {
                var t = typeof(UniversalRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings");
                var ensure = t?.GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                ensure?.Invoke(null, new object[] { true });
            }
            catch (Exception e) { Debug.LogWarning("[Builder] URP global settings: " + e.Message); }
            AssetDatabase.SaveAssets();
        }

        static void CreateSettingsAsset()
        {
            var s = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<GameSettings>();
                AssetDatabase.CreateAsset(s, SettingsPath);
            }
        }

        static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScene, true),
                new EditorBuildSettingsScene(GameScene, true)
            };
        }

        // ------------------------------------------------------------------ materials
        static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit");
        static Shader UnlitShader => Shader.Find("Universal Render Pipeline/Unlit");
        static Shader ParticleShader => Shader.Find("Universal Render Pipeline/Particles/Unlit");

        static T Load<T>(string path) where T : UnityEngine.Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogWarning("[Builder] missing asset " + path);
            return a;
        }

        static Texture2D Tex(string path) => Load<Texture2D>(path);
        static Sprite Spr(string name)
        {
            string p = "Assets/Textures/Generated/" + name + ".png";
            if (!File.Exists(p)) p = "Assets/Textures/UI/" + name + ".png";
            return Load<Sprite>(p);
        }

        static Material MakeMat(string name, Shader sh, Action<Material> setup)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = sh;
            setup(m);
            if (sh == LitShader) BaseShaderGUI.SetMaterialKeywords(m, LitGUI.SetMaterialKeywords);
            else if (sh == ParticleShader) BaseShaderGUI.SetMaterialKeywords(m, null, ParticleGUI.SetMaterialKeywords);
            else BaseShaderGUI.SetMaterialKeywords(m);
            EditorUtility.SetDirty(m);
            M[name] = m;
            return m;
        }

        static Material LitMat(string name, Color color, Texture2D baseMap = null, Texture2D normal = null, Texture2D ao = null,
                               float smooth = 0.3f, float metal = 0f, Color? emission = null, bool doubleSided = false, Vector2? tiling = null)
        {
            return MakeMat(name, LitShader, m =>
            {
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_Cull", doubleSided ? 0f : 2f);
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", baseMap);
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 1f);
                m.SetTexture("_OcclusionMap", ao);
                m.SetFloat("_OcclusionStrength", 1f);
                m.SetFloat("_Smoothness", smooth);
                m.SetFloat("_Metallic", metal);
                m.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
                if (emission.HasValue)
                {
                    m.SetColor("_EmissionColor", emission.Value);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    m.SetColor("_EmissionColor", Color.black);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
            });
        }

        static Material UnlitMat(string name, Color color, Texture tex, bool additive, bool doubleSided = true)
        {
            return MakeMat(name, UnlitShader, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", tex);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetFloat("_Cull", doubleSided ? 0f : 2f);
            });
        }

        static void CreateMaterials()
        {
            M.Clear();
            string acg = "Assets/ThirdParty/ambientCG/";
            var rockC = Tex(acg + "Rock064/Rock064_Color.jpg");
            var rockN = Tex(acg + "Rock064/Rock064_NormalGL.jpg");
            var rockA = Tex(acg + "Rock064/Rock064_AmbientOcclusion.jpg");
            var paveC = Tex(acg + "PavingStones138/PavingStones138_Color.jpg");
            var paveN = Tex(acg + "PavingStones138/PavingStones138_NormalGL.jpg");
            var paveA = Tex(acg + "PavingStones138/PavingStones138_AmbientOcclusion.jpg");
            var sandC = Tex(acg + "Ground054/Ground054_Color.jpg");
            var sandN = Tex(acg + "Ground054/Ground054_NormalGL.jpg");
            var sandA = Tex(acg + "Ground054/Ground054_AmbientOcclusion.jpg");
            string g = "Assets/Textures/Generated/";

            Color cyan = new Color(0.24f, 0.96f, 0.88f);
            Color amber = new Color(1f, 0.62f, 0.15f);

            LitMat("Wall", new Color(0.55f, 0.68f, 0.68f), rockC, rockN, rockA, 0.15f);
            LitMat("WallTop", new Color(0.72f, 0.84f, 0.80f), rockC, rockN, rockA, 0.1f);
            LitMat("WallCracked", new Color(0.48f, 0.5f, 0.46f), rockC, rockN, rockA, 0.1f);
            LitMat("Floor", new Color(0.58f, 0.70f, 0.70f), paveC, paveN, paveA, 0.25f);
            LitMat("FloorSand", new Color(0.62f, 0.70f, 0.64f), sandC, sandN, sandA, 0.15f);
            LitMat("FloorChamber", new Color(0.78f, 0.66f, 0.50f), paveC, paveN, paveA, 0.35f);
            LitMat("Stone", new Color(0.6f, 0.68f, 0.64f), rockC, rockN, rockA, 0.2f);
            LitMat("Debris", new Color(0.5f, 0.56f, 0.54f), rockC, rockN, null, 0.1f);

            LitMat("Brass", new Color(0.95f, 0.62f, 0.2f), null, null, null, 0.6f, 0.55f);
            LitMat("BrassDark", new Color(0.5f, 0.3f, 0.08f), null, null, null, 0.45f, 0.5f);
            LitMat("Glass", cyan, null, null, null, 0.9f, 0f, cyan * 2.2f);
            LitMat("Lamp", new Color(1f, 0.95f, 0.8f), null, null, null, 0.9f, 0f, new Color(1f, 0.92f, 0.7f) * 4f);
            LitMat("HeartGem", amber, null, null, null, 0.92f, 0f, amber * 3f);
            LitMat("Pearl", new Color(0.95f, 0.94f, 0.9f), null, null, null, 0.9f, 0f, new Color(0.5f, 0.55f, 0.55f));
            LitMat("UrchinBody", new Color(0.36f, 0.09f, 0.27f), null, null, null, 0.5f);
            LitMat("UrchinSpike", new Color(0.2f, 0.05f, 0.15f), null, null, null, 0.5f, 0f, new Color(0.35f, 0.04f, 0.08f));
            LitMat("VentGlow", cyan, null, null, null, 0.8f, 0f, cyan * 2.5f);
            LitMat("Dark", new Color(0.05f, 0.06f, 0.07f), null, null, null, 0.3f);
            LitMat("Seaweed", new Color(0.05f, 0.36f, 0.22f), null, null, null, 0.15f, 0f, new Color(0.01f, 0.08f, 0.05f), true);
            LitMat("Coral", new Color(0.95f, 0.42f, 0.38f), null, null, null, 0.3f, 0f, new Color(0.15f, 0.04f, 0.04f));
            LitMat("Glyph", cyan, null, null, null, 0.5f, 0f, cyan * 2.5f);
            LitMat("GateBars", new Color(0.32f, 0.26f, 0.18f), null, null, null, 0.5f, 0.8f);
            LitMat("Kenney", new Color(0.62f, 0.74f, 0.74f), Tex("Assets/ThirdParty/Kenney/Textures/colormap.png"), null, null, 0.2f);

            UnlitMat("EmblemMoon", cyan * 3f, Tex(g + "glyph_moon.png"), true);
            UnlitMat("EmblemSun", cyan * 3f, Tex(g + "glyph_sun.png"), true);
            UnlitMat("EmblemStar", cyan * 3f, Tex(g + "glyph_star.png"), true);
            UnlitMat("EmblemHeart", amber * 3f, Tex(g + "glyph_heart.png"), true);
            UnlitMat("WallGlyphMoon", cyan * 0.9f, Tex(g + "glyph_moon.png"), true);
            UnlitMat("WallGlyphSun", cyan * 0.9f, Tex(g + "glyph_sun.png"), true);
            UnlitMat("WallGlyphStar", cyan * 0.9f, Tex(g + "glyph_star.png"), true);
            UnlitMat("Cracks", new Color(1f, 0.55f, 0.2f) * 1.6f, Tex(g + "fx_cracks.png"), true);
            UnlitMat("SonarArrow", cyan * 2.5f, Tex(g + "fx_arrow.png"), true);
            UnlitMat("SonarRing", cyan * 2f, Tex(g + "fx_ring.png"), true);
            UnlitMat("CurrentArrows", cyan * 0.7f, Tex(g + "fx_chevrons.png"), true);
            UnlitMat("ExitGlow", new Color(0.7f, 1f, 0.95f) * 1.4f, Tex(g + "fx_dot.png"), true);
            UnlitMat("HaloAmber", amber * 1.2f, Tex(g + "fx_dot.png"), true);
            UnlitMat("HaloCyan", cyan * 0.9f, Tex(g + "fx_dot.png"), true);

            MakeMat("Particle", ParticleShader, m =>
            {
                m.SetTexture("_BaseMap", Tex(g + "fx_dot.png"));
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 2f);
            });
            AssetDatabase.SaveAssets();
        }

        /// <summary>URP Lit copy of an imported material's colour (used for the Quaternius fish).</summary>
        static Material ColorCopy(Material src, string prefix)
        {
            string name = prefix + "_" + src.name.Replace(" ", "");
            if (M.TryGetValue(name, out var existing)) return existing;
            Color c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.HasProperty("_Color") ? src.color : Color.gray;
            return LitMat(name, c, null, null, null, 0.35f);
        }

        // ------------------------------------------------------------------ helpers
        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, name);
                if (r) return r;
            }
            return null;
        }

        static Bounds RendererBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Rotates a model about Y so the vector back->front points along +Z.</summary>
        static void FaceForward(Transform model, Transform front, Transform back)
        {
            if (!front || !back) return;
            Vector3 f = front.position - back.position;
            f.y = 0f;
            if (f.sqrMagnitude < 1e-6f) return;
            float ang = Vector3.SignedAngle(f, Vector3.forward, Vector3.up);
            model.rotation = Quaternion.AngleAxis(ang, Vector3.up) * model.rotation;
        }

        /// <summary>Scales a model so its largest horizontal size equals 'size', centres it on X/Z and puts its base at 'baseY'.</summary>
        static void Fit(Transform model, float size, float baseY, bool centreY = false)
        {
            var b = RendererBounds(model.gameObject);
            float cur = Mathf.Max(b.size.x, b.size.z);
            if (cur > 1e-4f) model.localScale *= size / cur;
            b = RendererBounds(model.gameObject);
            Vector3 parentPos = model.parent ? model.parent.position : Vector3.zero;
            Vector3 offset = new Vector3(parentPos.x - b.center.x, centreY ? parentPos.y + baseY - b.center.y : parentPos.y + baseY - b.min.y, parentPos.z - b.center.z);
            model.position += offset;
        }

        static void SetMaterials(GameObject go, Func<Material, Material> map)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null) mats[i] = map(mats[i]) ?? mats[i];
                r.sharedMaterials = mats;
            }
        }

        static Material ByName(Material m)
        {
            string n = m.name.Replace(" (Instance)", "");
            int dot = n.IndexOf('.');
            if (dot > 0) n = n.Substring(0, dot);
            return M.TryGetValue(n, out var r) ? r : null;
        }

        static GameObject Model(string path, Transform parent)
        {
            var asset = Load<GameObject>(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject Child(string name, Transform parent, Vector3 localPos = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Quad(string name, Transform parent, Vector3 pos, Quaternion rot, Vector2 size, Material mat)
        {
            var go = Prim(PrimitiveType.Quad, name, parent, pos, new Vector3(size.x, size.y, 1f), mat);
            go.transform.localRotation = rot;
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static Light AddLight(Transform parent, string name, LightType type, Color color, float intensity, float range, Vector3 pos)
        {
            var go = Child(name, parent, pos);
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        static ParticleSystem AddParticles(Transform parent, string name, Vector3 pos)
        {
            var go = Child(name, parent, pos);
            return go.AddComponent<ParticleSystem>();
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            string path = PrefabDir + "/" + name + ".prefab";
            var p = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return p;
        }

        static RuntimeAnimatorController SwimController(string fbxPath, string name)
        {
            string ctrlPath = GenDir + "/Animators/" + name + ".controller";
            var clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null) { Debug.LogWarning("[Builder] no animation clip in " + fbxPath); return null; }
            AssetDatabase.DeleteAsset(ctrlPath);
            return AnimatorController.CreateAnimatorControllerAtPathWithClip(ctrlPath, clip);
        }

        static string[] ReadMap()
        {
            var lines = File.ReadAllLines(MapPath)
                .Select(l => l.TrimEnd('\r'))
                .Where(l => l.Length > 0 && !l.StartsWith(";"))
                .ToArray();
            int w = lines.Max(l => l.Length);
            return lines.Select(l => l.PadRight(w, '#')).ToArray();
        }
    }
}
