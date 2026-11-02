using System.IO;
using BangkokLastSurvivor.CameraSystem;
using BangkokLastSurvivor.Core;
using BangkokLastSurvivor.Input;
using BangkokLastSurvivor.Player;
using BangkokLastSurvivor.Testing;
using BangkokLastSurvivor.UI;
using BangkokLastSurvivor.UI.Touch;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BangkokLastSurvivor.EditorTools
{
    /// <summary>
    /// Builds the Milestone 1 playable test scene: a Bangkok street block, a test course
    /// (crouch tunnel, ramp, stairs, hazard, heal kits), the player, isometric camera, Thai HUD
    /// and touch controls. Menu: BANGKOK > Milestone 1 > Create Test Scene.
    /// </summary>
    public static class Milestone1SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/M1_TestScene.unity";
        private const string MaterialFolder = "Assets/_Project/Materials/M1";

        private static Font _editorFont;
        private static Sprite _uiSprite;
        private static Sprite _knobSprite;

        [MenuItem("BANGKOK/Milestone 1/Create Test Scene (สร้างฉากทดสอบ)", priority = 1)]
        public static void CreateTestSceneMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("BANGKOK: LAST SURVIVOR",
                    $"มีไฟล์ {ScenePath} อยู่แล้ว ต้องการสร้างใหม่ทับหรือไม่?\n(The test scene already exists. Rebuild it?)",
                    "สร้างใหม่ (Rebuild)", "ยกเลิก (Cancel)"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildScene();
        }

        [MenuItem("BANGKOK/Milestone 1/Open Test Scene (เปิดฉากทดสอบ)", priority = 2)]
        public static void OpenTestSceneMenu()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("BANGKOK: LAST SURVIVOR",
                    "ยังไม่มีฉากทดสอบ กรุณาใช้เมนู Create Test Scene ก่อน", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        /// <summary>Entry point usable from batch mode: -executeMethod BangkokLastSurvivor.EditorTools.Milestone1SceneBuilder.BuildScene</summary>
        public static void BuildScene()
        {
            _editorFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            _knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            EnsureFolder(MaterialFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLighting();
            Transform env = new GameObject("Environment").transform;
            BuildStreet(env);
            BuildTestCourse(env);

            var bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<GameBootstrap>();

            var spawn = new GameObject("PlayerSpawn").transform;
            spawn.SetPositionAndRotation(new Vector3(0f, 0.2f, -4f), Quaternion.identity);

            GameObject player = BuildPlayer(spawn);
            var reader = player.GetComponent<PlayerInputReader>();
            Camera cam = BuildCamera(player.transform, reader);
            SetRef(player.GetComponent<PlayerMotor>(), "cameraTransform", cam.transform);

            BuildEventSystem();
            BuildCanvas(player, reader);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BANGKOK] Milestone 1 test scene created: {ScenePath}  — press Play to test.");
        }

        // ------------------------------------------------------------------ Lighting

        private static void SetupLighting()
        {
            var sun = new GameObject("Sun (Directional Light)");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.88f, 0.72f);
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = light;

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.60f, 0.58f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.42f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.18f);

            // Smoky, polluted city haze.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.63f, 0.58f, 0.52f);
            RenderSettings.fogDensity = 0.011f;
        }

        // ------------------------------------------------------------------ Environment

        private static void BuildStreet(Transform env)
        {
            Material asphalt = GetMat("Asphalt", new Color(0.18f, 0.18f, 0.19f), 0.15f);
            Material sidewalk = GetMat("Sidewalk", new Color(0.55f, 0.53f, 0.50f), 0.1f);
            Material lineYellow = GetMat("LineYellow", new Color(0.85f, 0.70f, 0.15f), 0.2f);
            Material lineWhite = GetMat("LineWhite", new Color(0.85f, 0.85f, 0.82f), 0.2f);
            Material dirt = GetMat("Ground", new Color(0.30f, 0.28f, 0.24f), 0.05f);

            Transform street = Group("Street", env);
            Box("Ground", street, new Vector3(0f, -0.55f, 0f), new Vector3(160f, 1f, 160f), dirt);
            Box("Road_Main", street, new Vector3(0f, -0.04f, 10f), new Vector3(12f, 0.1f, 110f), asphalt);
            Box("Road_Cross", street, new Vector3(0f, -0.039f, 34f), new Vector3(110f, 0.1f, 12f), asphalt);
            Box("CenterLine", street, new Vector3(0f, 0.015f, 10f), new Vector3(0.2f, 0.02f, 110f), lineYellow);
            for (int i = 0; i < 12; i++)
            {
                float x = -48f + i * 8.5f;
                if (Mathf.Abs(x) < 7f) continue;
                Box("Dash", street, new Vector3(x, 0.016f, 34f), new Vector3(3f, 0.02f, 0.2f), lineWhite);
            }
            // Zebra crossing.
            for (int i = 0; i < 6; i++)
                Box("Zebra", street, new Vector3(-5f + i * 2f, 0.017f, 26.5f), new Vector3(1f, 0.02f, 3f), lineWhite);

            // Raised sidewalks along the main road (tests CharacterController step offset).
            Box("Sidewalk_L", street, new Vector3(-8f, 0.075f, 2f), new Vector3(4f, 0.15f, 52f), sidewalk);
            Box("Sidewalk_R", street, new Vector3(8f, 0.075f, 2f), new Vector3(4f, 0.15f, 52f), sidewalk);

            var rng = new System.Random(2026);
            Color[] facade =
            {
                new Color(0.78f, 0.70f, 0.55f), new Color(0.62f, 0.68f, 0.66f), new Color(0.75f, 0.60f, 0.52f),
                new Color(0.58f, 0.58f, 0.62f), new Color(0.80f, 0.76f, 0.66f), new Color(0.55f, 0.62f, 0.52f)
            };
            Color[] awning =
            {
                new Color(0.15f, 0.35f, 0.65f), new Color(0.70f, 0.18f, 0.15f), new Color(0.20f, 0.50f, 0.30f),
                new Color(0.80f, 0.60f, 0.10f)
            };
            Material shutter = GetMat("Shutter", new Color(0.42f, 0.44f, 0.46f), 0.5f, 0.6f);
            Material window = GetMat("Window", new Color(0.12f, 0.15f, 0.18f), 0.85f);

            // Shophouse rows (ตึกแถว) on both sides.
            Transform shophouses = Group("Shophouses", env);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 8; i++)
                {
                    float z = -20f + i * 5.5f;
                    float h = 7f + (float)rng.NextDouble() * 6f;
                    Material f = GetMat($"Facade{rng.Next(facade.Length)}", facade[rng.Next(facade.Length)], 0.1f);
                    BuildShophouse(shophouses, new Vector3(side * 13f, 0f, z), side, h, f,
                        GetMat($"Awning{i % awning.Length}", awning[i % awning.Length], 0.3f), shutter, window);
                }
            }

            // Back row across the cross street.
            for (int i = 0; i < 10; i++)
            {
                float x = -40f + i * 9f;
                if (Mathf.Abs(x) < 8f) continue;
                float h = 10f + (float)rng.NextDouble() * 14f;
                Material f = GetMat($"Facade{i % facade.Length}", facade[i % facade.Length], 0.1f);
                Box("Building", shophouses, new Vector3(x, h * 0.5f, 47f), new Vector3(8f, h, 12f), f);
            }

            // Props: abandoned cars, electric poles, barricades.
            Transform props = Group("Props", env);
            Material[] carPaints =
            {
                GetMat("Car_Red", new Color(0.55f, 0.10f, 0.08f), 0.7f, 0.3f),
                GetMat("Car_White", new Color(0.80f, 0.80f, 0.78f), 0.7f, 0.3f),
                GetMat("Car_Taxi", new Color(0.90f, 0.35f, 0.55f), 0.7f, 0.3f), // pink taxi
                GetMat("Car_Green", new Color(0.25f, 0.55f, 0.25f), 0.7f, 0.3f)
            };
            BuildCar(props, new Vector3(-2.8f, 0f, 8f), 12f, carPaints[2], window);
            BuildCar(props, new Vector3(3.2f, 0f, 16f), 160f, carPaints[1], window);
            BuildCar(props, new Vector3(-1.5f, 0f, 22f), -35f, carPaints[0], window);
            BuildCar(props, new Vector3(14f, 0f, 33f), 85f, carPaints[3], window);
            BuildCar(props, new Vector3(-18f, 0f, 35.5f), 100f, carPaints[1], window);

            Material pole = GetMat("Concrete", new Color(0.62f, 0.62f, 0.60f), 0.1f);
            for (int i = 0; i < 6; i++)
            {
                float z = -18f + i * 8f;
                Box("ElectricPole", props, new Vector3(-6.5f, 4.5f, z), new Vector3(0.3f, 9f, 0.3f), pole);
                Box("ElectricPole", props, new Vector3(6.5f, 4.5f, z + 4f), new Vector3(0.3f, 9f, 0.3f), pole);
            }

            Material barrier = GetMat("Barrier", new Color(0.85f, 0.45f, 0.10f), 0.2f);
            Box("Barricade", props, new Vector3(2f, 0.5f, 28.5f), new Vector3(3f, 1f, 0.5f), barrier);
            Box("Barricade", props, new Vector3(-4f, 0.5f, 30f), new Vector3(2.5f, 1f, 0.5f), barrier)
                .transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            Material crate = GetMat("Crate", new Color(0.50f, 0.36f, 0.20f), 0.1f);
            Box("Crate", props, new Vector3(-7.5f, 0.6f, -2f), new Vector3(1f, 0.9f, 1f), crate);
            Box("Crate", props, new Vector3(-7.4f, 1.5f, -2.1f), new Vector3(0.8f, 0.8f, 0.8f), crate);
            Box("Crate", props, new Vector3(7.6f, 0.6f, 12f), new Vector3(1.2f, 0.9f, 1.2f), crate);
        }

        private static void BuildShophouse(Transform parent, Vector3 basePos, int side, float height,
            Material facade, Material awning, Material shutter, Material window)
        {
            Transform root = Group("Shophouse", parent);
            root.position = basePos;
            float depth = 8f;
            float width = 5.3f;
            Box("Body", root, basePos + new Vector3(0f, height * 0.5f, 0f), new Vector3(depth, height, width), facade);

            float front = basePos.x - side * depth * 0.5f; // street-facing face
            Box("Shutter", root, new Vector3(front - side * 0.03f, 1.4f, basePos.z), new Vector3(0.06f, 2.8f, width * 0.8f), shutter);
            for (float y = 4.5f; y < height - 1f; y += 3f)
                Box("Window", root, new Vector3(front - side * 0.03f, y, basePos.z), new Vector3(0.06f, 1.4f, width * 0.6f), window);

            // Awning over the sidewalk: walkable underneath (3.2m clearance).
            var a = Box("Awning", root, new Vector3(front - side * 1.1f, 3.2f, basePos.z), new Vector3(2.2f, 0.12f, width), awning);
            a.transform.rotation = Quaternion.Euler(0f, 0f, side * -8f);
        }

        private static void BuildCar(Transform parent, Vector3 pos, float yaw, Material paint, Material glass)
        {
            Transform car = Group("AbandonedCar", parent);
            car.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            Material tyre = GetMat("Tyre", new Color(0.06f, 0.06f, 0.06f), 0.2f);
            Box("Body", car, Vector3.zero, new Vector3(1.8f, 0.7f, 4.2f), paint, local: true).transform.localPosition = new Vector3(0f, 0.65f, 0f);
            Box("Cabin", car, Vector3.zero, new Vector3(1.6f, 0.6f, 2.2f), glass, local: true).transform.localPosition = new Vector3(0f, 1.3f, -0.2f);
            for (int i = 0; i < 4; i++)
            {
                var w = Prim(PrimitiveType.Cylinder, "Wheel", car, tyre, keepCollider: false);
                w.transform.localPosition = new Vector3(i % 2 == 0 ? -0.85f : 0.85f, 0.35f, i < 2 ? 1.35f : -1.35f);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                w.transform.localScale = new Vector3(0.7f, 0.12f, 0.7f);
            }
        }

        private static void BuildTestCourse(Transform env)
        {
            Transform course = Group("TestCourse", env);
            Material concrete = GetMat("Concrete", new Color(0.62f, 0.62f, 0.60f), 0.1f);
            Material plaza = GetMat("Plaza", new Color(0.46f, 0.44f, 0.40f), 0.1f);
            Material tunnelMat = GetMat("TunnelPipe", new Color(0.40f, 0.42f, 0.45f), 0.3f, 0.2f);
            Box("Plaza", course, new Vector3(0f, -0.02f, -32f), new Vector3(40f, 0.1f, 24f), plaza);

            // 1) Crouch tunnel: 1.4m clearance (standing 1.8m, crouched 1.1m).
            Transform tunnel = Group("CrouchTunnel", course);
            float tz = -30f;
            Box("Tunnel_Roof", tunnel, new Vector3(-12f, 1.65f, tz), new Vector3(3f, 0.5f, 7f), tunnelMat);
            Box("Tunnel_WallL", tunnel, new Vector3(-13.75f, 0.95f, tz), new Vector3(0.5f, 1.9f, 7f), tunnelMat);
            Box("Tunnel_WallR", tunnel, new Vector3(-10.25f, 0.95f, tz), new Vector3(0.5f, 1.9f, 7f), tunnelMat);
            Label(tunnel, new Vector3(-12f, 3.2f, tz + 4f), "อุโมงค์ทดสอบหมอบ\nกด C หรือปุ่ม \"หมอบ\" แล้วลอดเข้าไป");

            // 2) Ramp.
            var ramp = Box("Ramp", course, new Vector3(0f, 0.9f, -30f), new Vector3(4f, 0.3f, 7f), concrete);
            ramp.transform.rotation = Quaternion.Euler(15f, 0f, 0f); // rises toward -Z
            Box("RampTop", course, new Vector3(0f, 1.75f, -36f), new Vector3(4f, 0.3f, 5f), concrete);
            Label(course, new Vector3(0f, 4f, -27f), "ทางลาด");

            // 3) Stairs (0.25m steps).
            Transform stairs = Group("Stairs", course);
            for (int i = 0; i < 8; i++)
            {
                float h = 0.25f * (i + 1);
                Box("Step", stairs, new Vector3(9f, h * 0.5f, -27f - i * 0.5f), new Vector3(3f, h, 0.5f), concrete);
            }
            Box("StairTop", stairs, new Vector3(9f, 1f, -32.25f), new Vector3(3f, 2f, 3f), concrete);
            Label(stairs, new Vector3(9f, 4f, -27f), "บันได");

            // 4) Hazard zone (damage test).
            Material fire = GetMat("Fire", new Color(1f, 0.35f, 0.05f), 0.0f, 0f, new Color(2.5f, 0.8f, 0.1f));
            var hazard = Box("DamageZone_Fire", course, new Vector3(15f, 0.05f, -38f), new Vector3(4f, 0.1f, 4f), fire);
            var hazardCol = hazard.GetComponent<BoxCollider>();
            hazardCol.size = new Vector3(1f, 20f, 1f); // tall trigger volume above the flat visual
            hazardCol.center = new Vector3(0f, 10f, 0f);
            hazardCol.isTrigger = true;
            hazard.AddComponent<DamageZone>();
            var glow = new GameObject("FireLight").AddComponent<Light>();
            glow.transform.SetParent(hazard.transform, false);
            glow.transform.position = hazard.transform.position + Vector3.up * 1.2f;
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.5f, 0.15f);
            glow.range = 8f;
            glow.intensity = 3f;
            Label(course, new Vector3(15f, 3f, -38f), "โซนอันตราย (ลดเลือด)");

            // 5) Heal kits.
            Material kit = GetMat("MedKit", new Color(0.9f, 0.9f, 0.9f), 0.3f);
            Material cross = GetMat("MedCross", new Color(0.1f, 0.75f, 0.25f), 0.3f, 0f, new Color(0.1f, 0.8f, 0.2f));
            BuildHealKit(course, new Vector3(-4f, 0f, -40f), kit, cross);
            BuildHealKit(course, new Vector3(4f, 0f, -40f), kit, cross);
            BuildHealKit(course, new Vector3(-6f, 0.15f, 6f), kit, cross);
            Label(course, new Vector3(0f, 3f, -41f), "ชุดปฐมพยาบาล (เพิ่มเลือด)");

            Label(course, new Vector3(0f, 3.5f, -12f), "สนามทดสอบ Milestone 1");
        }

        private static void BuildHealKit(Transform parent, Vector3 pos, Material kit, Material cross)
        {
            var root = new GameObject("HealPickup");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.9f, 0f);
            col.size = new Vector3(1.2f, 1.8f, 1.2f);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            visual.localPosition = new Vector3(0f, 0.6f, 0f);
            var box = Prim(PrimitiveType.Cube, "Box", visual, kit, keepCollider: false);
            box.transform.localScale = new Vector3(0.6f, 0.4f, 0.35f);
            var c1 = Prim(PrimitiveType.Cube, "CrossH", visual, cross, keepCollider: false);
            c1.transform.localPosition = new Vector3(0f, 0f, -0.18f);
            c1.transform.localScale = new Vector3(0.3f, 0.09f, 0.02f);
            var c2 = Prim(PrimitiveType.Cube, "CrossV", visual, cross, keepCollider: false);
            c2.transform.localPosition = new Vector3(0f, 0f, -0.18f);
            c2.transform.localScale = new Vector3(0.09f, 0.3f, 0.02f);

            var pickup = root.AddComponent<HealPickup>();
            SetRef(pickup, "visual", visual.gameObject);
        }

        // ------------------------------------------------------------------ Player

        private static GameObject BuildPlayer(Transform spawn)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.05f;
            cc.minMoveDistance = 0f;

            var reader = player.AddComponent<PlayerInputReader>();
            var stats = player.AddComponent<PlayerStats>();
            var motor = player.AddComponent<PlayerMotor>();
            var respawn = player.AddComponent<PlayerRespawn>();
            SetRef(motor, "input", reader);
            SetRef(motor, "stats", stats);
            SetRef(respawn, "input", reader);
            SetRef(respawn, "motor", motor);
            SetRef(respawn, "stats", stats);
            SetRef(respawn, "spawnPoint", spawn);

            // Primitive survivor model (replace with a rigged model + PlayerAnimatorBridge later).
            Material skin = GetMat("Player_Skin", new Color(0.85f, 0.66f, 0.50f), 0.3f);
            Material shirt = GetMat("Player_Shirt", new Color(0.20f, 0.38f, 0.55f), 0.2f);
            Material pants = GetMat("Player_Pants", new Color(0.18f, 0.18f, 0.22f), 0.2f);
            Material bag = GetMat("Player_Backpack", new Color(0.35f, 0.30f, 0.18f), 0.2f);
            Material hair = GetMat("Player_Hair", new Color(0.08f, 0.07f, 0.06f), 0.4f);

            var model = new GameObject("Model").transform;
            model.SetParent(player.transform, false);

            Transform legL = Pivot("Leg_L_Pivot", model, new Vector3(-0.13f, 0.9f, 0f));
            Part(PrimitiveType.Cube, "Leg_L", legL, pants, new Vector3(0f, -0.45f, 0f), new Vector3(0.22f, 0.9f, 0.26f));
            Transform legR = Pivot("Leg_R_Pivot", model, new Vector3(0.13f, 0.9f, 0f));
            Part(PrimitiveType.Cube, "Leg_R", legR, pants, new Vector3(0f, -0.45f, 0f), new Vector3(0.22f, 0.9f, 0.26f));

            Transform torso = Pivot("Torso_Pivot", model, new Vector3(0f, 0.9f, 0f));
            Part(PrimitiveType.Cube, "Torso", torso, shirt, new Vector3(0f, 0.33f, 0f), new Vector3(0.52f, 0.66f, 0.30f));
            Part(PrimitiveType.Cube, "Backpack", torso, bag, new Vector3(0f, 0.36f, -0.24f), new Vector3(0.42f, 0.5f, 0.2f));
            Part(PrimitiveType.Sphere, "Head", torso, skin, new Vector3(0f, 0.86f, 0f), new Vector3(0.32f, 0.34f, 0.32f));
            Part(PrimitiveType.Sphere, "Hair", torso, hair, new Vector3(0f, 0.93f, -0.02f), new Vector3(0.33f, 0.24f, 0.33f));
            // Small "nose" so facing direction is readable from above.
            Part(PrimitiveType.Cube, "Face", torso, skin, new Vector3(0f, 0.84f, 0.16f), new Vector3(0.06f, 0.06f, 0.06f));

            Transform armL = Pivot("Arm_L_Pivot", torso, new Vector3(-0.34f, 0.6f, 0f));
            Part(PrimitiveType.Cube, "Arm_L", armL, shirt, new Vector3(0f, -0.3f, 0f), new Vector3(0.15f, 0.62f, 0.17f));
            Transform armR = Pivot("Arm_R_Pivot", torso, new Vector3(0.34f, 0.6f, 0f));
            Part(PrimitiveType.Cube, "Arm_R", armR, shirt, new Vector3(0f, -0.3f, 0f), new Vector3(0.15f, 0.62f, 0.17f));

            var visual = player.AddComponent<PlayerProceduralVisual>();
            visual.Configure(motor, stats, model, torso, armL, armR, legL, legR);
            player.AddComponent<PlayerAnimatorBridge>();

            return player;
        }

        private static Camera BuildCamera(Transform target, PlayerInputReader reader)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            go.AddComponent<AudioListener>();
            var iso = go.AddComponent<IsometricCameraController>();
            SetRef(iso, "target", target);
            SetRef(iso, "input", reader);
            go.transform.position = target.position + new Vector3(-8f, 12f, -8f);
            go.transform.LookAt(target);
            return cam;
        }

        private static void BuildEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>(); // default UI actions are assigned at runtime
        }

        // ------------------------------------------------------------------ UI

        private static void BuildCanvas(GameObject player, PlayerInputReader reader)
        {
            var stats = player.GetComponent<PlayerStats>();
            var motor = player.GetComponent<PlayerMotor>();
            var respawn = player.GetComponent<PlayerRespawn>();

            var canvasGo = new GameObject("HUD Canvas", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            RectTransform root = (RectTransform)canvasGo.transform;

            // Damage vignette (behind everything).
            Image vignette = Img("DamageVignette", root, new Color(0.8f, 0f, 0f, 0f));
            Stretch(vignette.rectTransform);
            vignette.raycastTarget = false;

            // ---- Status panel (top-left)
            Image panel = Img("StatusPanel", root, new Color(0f, 0f, 0f, 0.45f), _uiSprite, Image.Type.Sliced);
            Anchor(panel.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(540f, 200f));
            panel.raycastTarget = false;
            Txt("Title", panel.rectTransform, "BANGKOK: LAST SURVIVOR", 24, TextAnchor.UpperLeft,
                new Color(1f, 0.82f, 0.35f), new Vector2(20f, -12f), new Vector2(500f, 34f));

            Txt("HealthLabel", panel.rectTransform, "เลือด", 22, TextAnchor.MiddleLeft, Color.white,
                new Vector2(20f, -52f), new Vector2(80f, 32f));
            Image hpBg = Img("HealthBarBG", panel.rectTransform, new Color(0.1f, 0.1f, 0.1f, 0.9f), _uiSprite, Image.Type.Sliced);
            Anchor(hpBg.rectTransform, new Vector2(0f, 1f), new Vector2(100f, -52f), new Vector2(420f, 32f));
            Image hpLag = FillBar("HealthLag", hpBg.rectTransform, new Color(1f, 0.9f, 0.8f, 0.6f));
            Image hpFill = FillBar("HealthFill", hpBg.rectTransform, new Color(0.3f, 0.85f, 0.35f));
            Text hpText = Txt("HealthText", hpBg.rectTransform, "เลือด 100/100", 20, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.zero);
            Stretch(hpText.rectTransform);

            Txt("StaminaLabel", panel.rectTransform, "แรง", 22, TextAnchor.MiddleLeft, Color.white,
                new Vector2(20f, -94f), new Vector2(80f, 28f));
            Image stBg = Img("StaminaBarBG", panel.rectTransform, new Color(0.1f, 0.1f, 0.1f, 0.9f), _uiSprite, Image.Type.Sliced);
            Anchor(stBg.rectTransform, new Vector2(0f, 1f), new Vector2(100f, -94f), new Vector2(420f, 26f));
            Image stFill = FillBar("StaminaFill", stBg.rectTransform, new Color(0.95f, 0.78f, 0.2f));
            Text stText = Txt("StaminaText", stBg.rectTransform, "แรง 100", 18, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.zero);
            Stretch(stText.rectTransform);

            Text stateText = Txt("StateText", panel.rectTransform, "สถานะ: ยืน", 24, TextAnchor.MiddleLeft,
                new Color(0.85f, 0.95f, 1f), new Vector2(20f, -136f), new Vector2(500f, 40f));

            // ---- PC help (top-right)
            Image help = Img("PCHelp", root, new Color(0f, 0f, 0f, 0.4f), _uiSprite, Image.Type.Sliced);
            Anchor(help.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(430f, 330f), new Vector2(1f, 1f));
            help.raycastTarget = false;
            Text helpText = Txt("HelpText", help.rectTransform,
                "ปุ่มควบคุม (PC)\n" +
                "W A S D / ลูกศร : เดิน\n" +
                "Shift ค้าง : วิ่ง\n" +
                "C : หมอบ/ลุก   Ctrl ค้าง : หมอบ\n" +
                "Q / E : หมุนกล้อง\n" +
                "คลิกขวาลาก : หมุนกล้อง\n" +
                "ล้อเมาส์ / + - : ซูม\n" +
                "R : เกิดใหม่ (เมื่อตาย)\n" +
                "F1 : เปิด/ปิดปุ่มสัมผัส", 21, TextAnchor.UpperLeft, Color.white, Vector2.zero, Vector2.zero);
            Stretch(helpText.rectTransform, 18f);

            // ---- Touch controls
            var touchRoot = new GameObject("TouchControls", typeof(RectTransform));
            touchRoot.transform.SetParent(root, false);
            RectTransform touchRect = (RectTransform)touchRoot.transform;
            Stretch(touchRect);

            // Camera drag/pinch zone (right side, behind buttons).
            Image camZone = Img("CameraTouchZone", touchRect, new Color(1f, 1f, 1f, 0f));
            camZone.rectTransform.anchorMin = new Vector2(0.45f, 0f);
            camZone.rectTransform.anchorMax = new Vector2(1f, 0.8f);
            camZone.rectTransform.offsetMin = camZone.rectTransform.offsetMax = Vector2.zero;
            var camArea = camZone.gameObject.AddComponent<TouchCameraArea>();
            SetRef(camArea, "input", reader);
            Text camHint = Txt("Hint", camZone.rectTransform, "ลากเพื่อหมุนกล้อง • ใช้สองนิ้วเพื่อซูม", 20,
                TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.35f), Vector2.zero, Vector2.zero);
            Stretch(camHint.rectTransform, 12f);

            // Joystick zone (left side).
            Image joyZone = Img("JoystickZone", touchRect, new Color(1f, 1f, 1f, 0f));
            joyZone.rectTransform.anchorMin = Vector2.zero;
            joyZone.rectTransform.anchorMax = new Vector2(0.45f, 0.75f);
            joyZone.rectTransform.offsetMin = joyZone.rectTransform.offsetMax = Vector2.zero;
            Image joyBase = Img("StickBase", joyZone.rectTransform, new Color(1f, 1f, 1f, 0.22f), _knobSprite);
            joyBase.rectTransform.sizeDelta = new Vector2(260f, 260f);
            joyBase.rectTransform.anchoredPosition = new Vector2(-180f, -120f);
            joyBase.raycastTarget = false;
            Image joyKnob = Img("StickKnob", joyBase.rectTransform, new Color(1f, 1f, 1f, 0.6f), _knobSprite);
            joyKnob.rectTransform.sizeDelta = new Vector2(120f, 120f);
            joyKnob.raycastTarget = false;
            var joystick = joyZone.gameObject.AddComponent<VirtualJoystick>();
            SetRef(joystick, "input", reader);
            SetRef(joystick, "stickBase", joyBase.rectTransform);
            SetRef(joystick, "stickKnob", joyKnob.rectTransform);

            // Action buttons (bottom-right).
            TouchButton(touchRect, "Btn_Run", "วิ่ง", TouchAction.Run, reader, null,
                new Vector2(-90f, 90f), 200f, 34);
            TouchButton(touchRect, "Btn_Crouch", "หมอบ", TouchAction.CrouchToggle, reader, motor,
                new Vector2(-320f, 70f), 150f, 28);
            TouchButton(touchRect, "Btn_RotL", "< หมุน", TouchAction.RotateLeft, reader, null,
                new Vector2(-330f, 260f), 110f, 22);
            TouchButton(touchRect, "Btn_RotR", "หมุน >", TouchAction.RotateRight, reader, null,
                new Vector2(-90f, 330f), 110f, 22);
            TouchButton(touchRect, "Btn_ZoomIn", "ซูม +", TouchAction.ZoomIn, reader, null,
                new Vector2(-90f, 480f), 100f, 22);
            TouchButton(touchRect, "Btn_ZoomOut", "ซูม -", TouchAction.ZoomOut, reader, null,
                new Vector2(-220f, 420f), 100f, 22);

            // ---- Death panel (on top of everything)
            Image death = Img("DeathPanel", root, new Color(0.15f, 0f, 0f, 0.75f));
            Stretch(death.rectTransform);
            Text deathTitle = Txt("DeathTitle", death.rectTransform, "คุณเสียชีวิตแล้ว", 72, TextAnchor.MiddleCenter,
                new Color(1f, 0.3f, 0.25f), new Vector2(0f, 120f), new Vector2(1200f, 120f));
            CenterAnchor(deathTitle.rectTransform);
            Text deathHint = Txt("DeathHint", death.rectTransform, "...", 30, TextAnchor.MiddleCenter,
                Color.white, new Vector2(0f, 20f), new Vector2(1200f, 60f));
            CenterAnchor(deathHint.rectTransform);
            Image respawnBtn = Img("Btn_Respawn", death.rectTransform, new Color(1f, 1f, 1f, 0.35f), _uiSprite, Image.Type.Sliced);
            CenterAnchor(respawnBtn.rectTransform);
            respawnBtn.rectTransform.anchoredPosition = new Vector2(0f, -100f);
            respawnBtn.rectTransform.sizeDelta = new Vector2(360f, 100f);
            Text rbLabel = Txt("Label", respawnBtn.rectTransform, "เกิดใหม่", 40, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.zero);
            Stretch(rbLabel.rectTransform);
            var rb = respawnBtn.gameObject.AddComponent<TouchActionButton>();
            rb.Configure(reader, TouchAction.Respawn, respawnBtn);
            death.gameObject.SetActive(false);

            // ---- Controllers
            canvasGo.AddComponent<ThaiFontProvider>();
            var hud = canvasGo.AddComponent<PlayerHUD>();
            hud.Configure(stats, motor, respawn, hpFill, hpLag, hpText, stFill, stText, stateText,
                vignette, death.gameObject, deathHint);
            var vis = canvasGo.AddComponent<TouchControlsVisibility>();
            SetRef(vis, "touchControlsRoot", touchRoot);
            SetRef(vis, "pcOnlyRoot", help.gameObject);
            SetRef(vis, "input", reader);
        }

        private static void TouchButton(RectTransform parent, string name, string label, TouchAction action,
            PlayerInputReader reader, PlayerMotor motor, Vector2 posFromBottomRight, float size, int fontSize)
        {
            Image img = Img(name, parent, new Color(1f, 1f, 1f, 0.35f), _knobSprite);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = posFromBottomRight + new Vector2(-size * 0.5f, size * 0.5f);
            Text t = Txt("Label", rt, label, fontSize, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.zero);
            Stretch(t.rectTransform);
            var btn = img.gameObject.AddComponent<TouchActionButton>();
            btn.Configure(reader, action, img, motor);
        }

        private static Image Img(string name, RectTransform parent, Color color, Sprite sprite = null,
            Image.Type type = Image.Type.Simple)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            img.color = color;
            return img;
        }

        private static Image FillBar(string name, RectTransform parent, Color color)
        {
            Image img = Img(name, parent, color, _uiSprite, Image.Type.Filled);
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            img.raycastTarget = false;
            Stretch(img.rectTransform, 3f);
            return img;
        }

        private static Text Txt(string name, RectTransform parent, string content, int size, TextAnchor align,
            Color color, Vector2 topLeftPos, Vector2 boxSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = _editorFont;
            text.text = content;
            text.fontSize = size;
            text.alignment = align;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
            Anchor(text.rectTransform, new Vector2(0f, 1f), topLeftPos, boxSize);
            return text;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void CenterAnchor(RectTransform rt)
        {
            Vector2 pos = rt.anchoredPosition;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }

        private static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>World-space Thai sign with its own font provider.</summary>
        private static void Label(Transform parent, Vector3 worldPos, string content)
        {
            var go = new GameObject("Sign", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(600f, 160f);
            rt.localScale = Vector3.one * 0.01f;
            Text t = Txt("Text", rt, content, 44, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.75f),
                Vector2.zero, Vector2.zero);
            Stretch(t.rectTransform);
            go.AddComponent<ThaiFontProvider>();
            go.AddComponent<FaceCamera>();
        }

        // ------------------------------------------------------------------ Helpers

        private static Transform Group(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat,
            bool local = false)
        {
            GameObject go = Prim(PrimitiveType.Cube, name, parent, mat, keepCollider: true);
            if (local) go.transform.localPosition = pos; else go.transform.position = pos;
            go.transform.localScale = scale;
            go.isStatic = true;
            return go;
        }

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Material mat, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static Transform Pivot(string name, Transform parent, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        private static void Part(PrimitiveType type, string name, Transform parent, Material mat, Vector3 localPos, Vector3 scale)
        {
            GameObject go = Prim(type, name, parent, mat, keepCollider: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
        }

        private static Material GetMat(string name, Color color, float smoothness, float metallic = 0f,
            Color? emission = null)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void SetRef(Object component, string field, Object value)
        {
            var so = new SerializedObject(component);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[BANGKOK] Field '{field}' not found on {component.GetType().Name}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
