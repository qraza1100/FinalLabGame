using UnityEditor;
using UnityEngine;

namespace DrownedSanctum.EditorTools
{
    public static partial class SanctumBuilder
    {
        const string CustomDir = "Assets/Models/Custom/";
        const string FishDir = "Assets/ThirdParty/Quaternius/";
        const string KenneyDir = "Assets/ThirdParty/Kenney/";

        static PhysicsMaterial slippery;
        static readonly Color CyanC = new Color(0.24f, 0.96f, 0.88f);
        static readonly Color AmberC = new Color(1f, 0.62f, 0.15f);

        static AudioClip Clip(string name) => Load<AudioClip>("Assets/Audio/SFX/" + name + ".ogg");
        static GameObject PF(string name) => Load<GameObject>(PrefabDir + "/" + name + ".prefab");

        static PhysicsMaterial Slippery()
        {
            foreach (var ext in new[] { ".physicMaterial", ".physicsMaterial", ".asset" })
            {
                var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Settings/Slippery" + ext);
                if (existing) return existing;
            }
            var pm = new PhysicsMaterial("Slippery")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            foreach (var ext in new[] { ".physicMaterial", ".physicsMaterial" })
            {
                try
                {
                    AssetDatabase.CreateAsset(pm, "Assets/Settings/Slippery" + ext);
                    if (AssetDatabase.Contains(pm)) return pm;
                }
                catch (System.Exception) { }
            }
            Debug.LogWarning("[Builder] could not save the Slippery physics material; colliders will use the default.");
            return null;
        }

        static GameObject Halo(Transform parent, Vector3 pos, float size, Material mat)
        {
            // Faces the fixed top-down camera (camera pitch ~62 degrees).
            return Quad("Halo", parent, pos, Quaternion.Euler(62f, 0f, 0f), new Vector2(size, size), mat);
        }

        static void CreatePrefabs()
        {
            slippery = Slippery();

            // ---------------------------------------------------------- Player (explorer sub)
            {
                var root = new GameObject("Player");
                var rb = root.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.mass = 1f;
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                var col = root.AddComponent<SphereCollider>();
                col.radius = 0.62f;
                if (slippery) col.sharedMaterial = slippery;
                var pc = root.AddComponent<PlayerController>();

                var visual = Child("Visual", root.transform);
                var sub = Model(CustomDir + "Sub.fbx", visual.transform);
                SetMaterials(sub, ByName);
                var lamp = FindDeep(sub.transform, "Lamp");
                var prop = FindDeep(sub.transform, "Propeller");
                FaceForward(sub.transform, lamp, prop);
                Fit(sub.transform, 1.9f, 0f, true);

                Vector3 lampLocal = lamp ? visual.transform.InverseTransformPoint(lamp.position) : new Vector3(0f, 0f, 0.95f);
                var head = AddLight(visual.transform, "Headlamp", LightType.Spot, new Color(1f, 0.93f, 0.78f), 10f, 18f, lampLocal);
                head.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
                head.spotAngle = 85f;
                head.innerSpotAngle = 40f;
                AddLight(root.transform, "Glow", LightType.Point, new Color(0.55f, 0.85f, 0.95f), 1.6f, 7f, new Vector3(0f, 1.3f, 0f));

                var bub = AddParticles(visual.transform, "Bubbles", prop ? visual.transform.InverseTransformPoint(prop.position) : new Vector3(0f, 0f, -1f));
                FxUtil.ConfigureLoop(bub, M["Particle"], new Color(0.8f, 1f, 1f, 0.55f), 6f, 1.6f, 0.4f, 0.13f,
                                     ParticleSystemShapeType.Sphere, 0.15f, Vector3.one, -0.25f);

                var engine = root.AddComponent<AudioSource>();
                engine.clip = Clip("engine_loop");
                engine.loop = true;
                engine.playOnAwake = true;
                engine.volume = 0.06f;
                engine.spatialBlend = 0f;

                var arrow = Child("SonarArrow", root.transform);
                Quad("Arrow", arrow.transform, Vector3.zero, Quaternion.identity, new Vector2(1.1f, 2.2f), M["SonarArrow"]);
                var ring = Child("SonarRing", root.transform);
                Quad("Ring", ring.transform, Vector3.zero, Quaternion.identity, Vector2.one, M["SonarRing"]);

                pc.visual = visual.transform;
                pc.propeller = prop;
                pc.bubbles = bub;
                pc.engineAudio = engine;
                pc.sonarArrow = arrow.transform;
                pc.sonarRing = ring.transform;
                SavePrefab(root, "Player");
            }

            // ---------------------------------------------------------- Pearl
            {
                var root = new GameObject("Pearl");
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.8f;
                col.center = new Vector3(0f, PlayerY, 0f);
                var pk = root.AddComponent<Pickup>();
                pk.type = PickupType.Pearl;
                pk.fxColor = new Color(1f, 1f, 0.95f);
                var vis = Child("Visual", root.transform, new Vector3(0f, PlayerY, 0f));
                var fl = vis.AddComponent<Floater>();
                fl.spinSpeed = 90f;
                fl.bobAmplitude = 0.12f;
                var m = Model(CustomDir + "Pearl.fbx", vis.transform);
                SetMaterials(m, ByName);
                Fit(m.transform, 0.45f, 0f, true);
                Halo(root.transform, new Vector3(0f, PlayerY, 0f), 1.1f, M["HaloCyan"]);
                SavePrefab(root, "Pearl");
            }

            // ---------------------------------------------------------- Sigils
            string[] sigilNames = { "Moon", "Sun", "Star" };
            for (int i = 0; i < 3; i++)
            {
                var root = new GameObject("Sigil_" + sigilNames[i]);
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.0f;
                col.center = new Vector3(0f, PlayerY, 0f);
                var pk = root.AddComponent<Pickup>();
                pk.type = PickupType.Sigil;
                pk.sigilIndex = i;
                pk.fxColor = CyanC;
                Prim(PrimitiveType.Cylinder, "Pedestal", root.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 0.2f, 0.9f), M["Stone"]);
                var vis = Child("Visual", root.transform, new Vector3(0f, 1.25f, 0f));
                var fl = vis.AddComponent<Floater>();
                fl.spinSpeed = 50f;
                fl.bobAmplitude = 0.15f;
                var tab = Model(CustomDir + "SigilTablet.fbx", vis.transform);
                SetMaterials(tab, ByName);
                Fit(tab.transform, 0.75f, 0f, true);
                var b = RendererBounds(tab);
                bool thinX = b.size.x < b.size.z;
                float half = (thinX ? b.size.x : b.size.z) * 0.5f + 0.025f;
                foreach (int s in new[] { -1, 1 })
                {
                    Vector3 off = thinX ? new Vector3(s * half, 0f, 0f) : new Vector3(0f, 0f, s * half);
                    Vector3 local = b.center - vis.transform.position + off;
                    Quad("Emblem", vis.transform, local, Quaternion.LookRotation(off.normalized), new Vector2(0.5f, 0.5f), M["Emblem" + sigilNames[i]]);
                }
                AddLight(root.transform, "Glow", LightType.Point, CyanC, 2.5f, 5.5f, new Vector3(0f, 1.5f, 0f));
                var sp = AddParticles(root.transform, "Sparkle", new Vector3(0f, 1.2f, 0f));
                FxUtil.ConfigureLoop(sp, M["Particle"], new Color(0.5f, 1f, 0.95f, 0.8f), 7f, 1.6f, 0.35f, 0.14f,
                                     ParticleSystemShapeType.Sphere, 0.6f, Vector3.one, -0.05f);
                Halo(root.transform, new Vector3(0f, 1.25f, 0f), 2.4f, M["HaloCyan"]);
                SavePrefab(root, "Sigil_" + sigilNames[i]);
            }

            // ---------------------------------------------------------- Heart of Eternity on its altar
            {
                var root = new GameObject("HeartOfEternity");
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.2f;
                col.center = new Vector3(0f, PlayerY, 0f);
                var pk = root.AddComponent<Pickup>();
                pk.type = PickupType.Heart;
                pk.fxColor = AmberC;
                var altar = Model(CustomDir + "Altar.fbx", root.transform);
                SetMaterials(altar, ByName);
                Fit(altar.transform, 2.0f, 0f);
                pk.keepOnCollect = new[] { altar };
                var vis = Child("Visual", root.transform, new Vector3(0f, 1.6f, 0f));
                var fl = vis.AddComponent<Floater>();
                fl.spinSpeed = 40f;
                fl.bobAmplitude = 0.15f;
                var h = Model(CustomDir + "HeartOfEternity.fbx", vis.transform);
                SetMaterials(h, ByName);
                Fit(h.transform, 0.9f, 0f, true);
                AddLight(root.transform, "Glow", LightType.Point, AmberC, 4f, 10f, new Vector3(0f, 1.9f, 0f));
                var sp = AddParticles(root.transform, "Sparkle", new Vector3(0f, 1.6f, 0f));
                FxUtil.ConfigureLoop(sp, M["Particle"], new Color(1f, 0.75f, 0.3f, 0.9f), 10f, 1.8f, 0.4f, 0.16f,
                                     ParticleSystemShapeType.Sphere, 0.7f, Vector3.one, -0.05f);
                Halo(root.transform, new Vector3(0f, 1.6f, 0f), 3.4f, M["HaloAmber"]);
                SavePrefab(root, "HeartOfEternity");
            }

            // ---------------------------------------------------------- Urchin
            {
                var root = new GameObject("Urchin");
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.85f;
                col.center = new Vector3(0f, PlayerY, 0f);
                root.AddComponent<Hazard>();
                var vis = Child("Visual", root.transform, new Vector3(0f, 0.85f, 0f));
                var sw = vis.AddComponent<Sway>();
                sw.angle = 0f;
                sw.pulseAmount = 0.06f;
                sw.pulseSpeed = 2.2f;
                var fl = vis.AddComponent<Floater>();
                fl.spinSpeed = 12f;
                fl.bobAmplitude = 0.05f;
                var m = Model(CustomDir + "Urchin.fbx", vis.transform);
                SetMaterials(m, ByName);
                Fit(m.transform, 1.5f, 0f, true);
                SavePrefab(root, "Urchin");
            }

            // ---------------------------------------------------------- Air vent
            {
                var root = new GameObject("AirVent");
                var col = root.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(2.4f, 2.4f, 2.4f);
                col.center = new Vector3(0f, 1.2f, 0f);
                root.AddComponent<AirVent>();
                var m = Model(CustomDir + "AirVent.fbx", root.transform);
                SetMaterials(m, ByName);
                Fit(m.transform, 1.7f, 0.02f);
                AddLight(root.transform, "Glow", LightType.Point, CyanC, 1.8f, 5f, new Vector3(0f, 0.9f, 0f));
                var bub = AddParticles(root.transform, "Bubbles", new Vector3(0f, 0.2f, 0f));
                bub.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                FxUtil.ConfigureLoop(bub, M["Particle"], new Color(0.8f, 1f, 1f, 0.7f), 20f, 2.4f, 1.4f, 0.18f,
                                     ParticleSystemShapeType.Cone, 0.5f, Vector3.one, -0.05f);
                var shape = bub.shape;
                shape.angle = 8f;
                Quad("FloorGlow", root.transform, new Vector3(0f, 0.04f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector2(2.6f, 2.6f), M["HaloCyan"]);
                SavePrefab(root, "AirVent");
            }

            // ---------------------------------------------------------- Shark guardian
            {
                var root = new GameObject("Shark");
                var rb = root.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                var col = root.AddComponent<CapsuleCollider>();
                col.isTrigger = true;
                col.direction = 2;
                col.radius = 0.75f;
                col.height = 3.0f;
                var sp = root.AddComponent<SharkPatrol>();
                var m = Model(FishDir + "Shark.fbx", root.transform);
                FaceForward(m.transform, FindDeep(m.transform, "Face"), FindDeep(m.transform, "Tail"));
                Fit(m.transform, 3.2f, 0f, true);
                SetMaterials(m, src => ColorCopy(src, "Shark"));
                var anim = m.GetComponent<Animator>();
                if (!anim) anim = m.AddComponent<Animator>();
                anim.runtimeAnimatorController = SwimController(FishDir + "Shark.fbx", "SharkSwim");
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                sp.animator = anim;
                SavePrefab(root, "Shark");
            }

            // ---------------------------------------------------------- decorative fish
            foreach (var (file, name, len) in new[] { ("Fish1.fbx", "Fish1", 0.7f), ("Fish2.fbx", "Fish2", 0.65f), ("Fish3.fbx", "Fish3", 0.6f), ("MantaRay.fbx", "MantaRay", 1.6f) })
            {
                var root = new GameObject(name);
                var m = Model(FishDir + file, root.transform);
                FaceForward(m.transform, FindDeep(m.transform, "Face"), FindDeep(m.transform, "Tail"));
                Fit(m.transform, len, 0f, true);
                SetMaterials(m, src => ColorCopy(src, name));
                var anim = m.GetComponent<Animator>();
                if (!anim) anim = m.AddComponent<Animator>();
                anim.runtimeAnimatorController = SwimController(FishDir + file, name + "Swim");
                anim.applyRootMotion = false;
                SavePrefab(root, name);
            }

            // ---------------------------------------------------------- water current tile
            {
                var root = new GameObject("Current");
                var col = root.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(Cell, 2.4f, Cell);
                col.center = new Vector3(0f, 1.2f, 0f);
                var cz = root.AddComponent<CurrentZone>();
                var arrows = Quad("Arrows", root.transform, new Vector3(0f, 0.06f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector2(Cell * 0.7f, Cell), M["CurrentArrows"]);
                cz.arrowRenderer = arrows.GetComponent<Renderer>();
                var ps = AddParticles(root.transform, "Stream", new Vector3(0f, 0.9f, -Cell * 0.45f));
                FxUtil.ConfigureLoop(ps, M["Particle"], new Color(0.6f, 1f, 1f, 0.5f), 14f, 0.9f, 3.6f, 0.12f,
                                     ParticleSystemShapeType.Box, 0f, new Vector3(Cell * 0.8f, 1.2f, 0.2f), 0f, ParticleSystemSimulationSpace.Local);
                SavePrefab(root, "Current");
            }

            // ---------------------------------------------------------- gate (portcullis)
            {
                var root = new GameObject("Gate");
                var col = root.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(Cell * 0.9f, 2.4f, Cell * 2.4f);
                col.center = new Vector3(0f, 1.2f, 0f);
                var gate = root.AddComponent<Gate>();
                Prim(PrimitiveType.Cube, "PillarL", root.transform, new Vector3(-Cell / 2f + 0.3f, (WallH + 0.3f) / 2f, 0f), new Vector3(0.6f, WallH + 0.3f, 1.0f), M["Wall"]);
                Prim(PrimitiveType.Cube, "PillarR", root.transform, new Vector3(Cell / 2f - 0.3f, (WallH + 0.3f) / 2f, 0f), new Vector3(0.6f, WallH + 0.3f, 1.0f), M["Wall"]);
                Prim(PrimitiveType.Cube, "Lintel", root.transform, new Vector3(0f, WallH + 0.25f, 0f), new Vector3(Cell, 0.5f, 1.0f), M["WallTop"]);
                var bars = Child("Bars", root.transform, new Vector3(0f, WallH / 2f, 0f));
                for (int k = 0; k < 7; k++)
                    Prim(PrimitiveType.Cylinder, "Bar" + k, bars.transform, new Vector3(-1.05f + k * 0.35f, 0f, 0f), new Vector3(0.09f, WallH / 2f, 0.09f), M["GateBars"]);
                Prim(PrimitiveType.Cube, "CrossLow", bars.transform, new Vector3(0f, -0.5f, 0f), new Vector3(2.3f, 0.08f, 0.08f), M["GateBars"]);
                Prim(PrimitiveType.Cube, "CrossHigh", bars.transform, new Vector3(0f, 0.6f, 0f), new Vector3(2.3f, 0.08f, 0.08f), M["GateBars"]);
                var block = bars.AddComponent<BoxCollider>();
                block.size = new Vector3(Cell, WallH, 0.5f);
                Quad("EmblemFront", root.transform, new Vector3(0f, WallH + 0.25f, -0.52f), Quaternion.identity, new Vector2(0.62f, 0.62f), M["EmblemMoon"]);
                Quad("EmblemBack", root.transform, new Vector3(0f, WallH + 0.25f, 0.52f), Quaternion.identity, new Vector2(0.62f, 0.62f), M["EmblemMoon"]);
                var glow = AddLight(root.transform, "Glow", LightType.Point, CyanC, 2.2f, 5f, new Vector3(0f, WallH - 0.3f, 0f));
                gate.bars = bars.transform;
                gate.blocker = block;
                gate.glowLight = glow;
                gate.openDepth = WallH + 0.3f;
                SavePrefab(root, "Gate");
            }

            // ---------------------------------------------------------- decor
            {
                var root = new GameObject("Seaweed");
                var m = Model(CustomDir + "Seaweed.fbx", root.transform);
                SetMaterials(m, ByName);
                Fit(m.transform, 0.8f, 0f);
                var sw = m.AddComponent<Sway>();
                sw.angle = 7f;
                sw.speed = 0.8f;
                SavePrefab(root, "Seaweed");
            }
            {
                var root = new GameObject("Coral");
                var m = Model(CustomDir + "Coral.fbx", root.transform);
                SetMaterials(m, ByName);
                Fit(m.transform, 0.9f, 0f);
                SavePrefab(root, "Coral");
            }
            {
                var root = new GameObject("Column");
                var col = root.AddComponent<BoxCollider>();
                col.size = new Vector3(1.2f, WallH, 1.2f);
                col.center = new Vector3(0f, WallH / 2f, 0f);
                var m = Model(KenneyDir + "kenney_column.fbx", root.transform);
                SetMaterials(m, _ => M["Kenney"]);
                Fit(m.transform, 1.15f, 0f);
                SavePrefab(root, "Column");
            }
            foreach (var (file, name, size) in new[] { ("kenney_rocks.fbx", "Rocks", 1.3f), ("kenney_stones.fbx", "Stones", 1.0f), ("kenney_pot.fbx", "Pot", 0.6f), ("kenney_barrel.fbx", "Barrel", 0.7f) })
            {
                var root = new GameObject(name);
                var m = Model(KenneyDir + file, root.transform);
                SetMaterials(m, _ => M["Kenney"]);
                Fit(m.transform, size, 0f);
                SavePrefab(root, name);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
