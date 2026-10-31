using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Project.Scripts.Enemies;
using SibJam.Locations;
using SibJam.SecurityCameras;
using SibJam.Waves;
using SibJam.Waves.Testing;
using SibJam.Weapons;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SibJam.Editor
{
    // Инструмент для ИИ-агентов (сборка через Unity MCP), не для геймдизайна. Перезаписывает сцену целиком — ручные правки пропадут.
    // Генерирует сцену GameplayFlow: сейф-комната, лёгкая, 4 обычные, комната босса. Комнаты без потолка, между ними коридоры с подъёмными дверями
    public static class GameplayFlowBuilder
    {
        private const string BaseScenePath = "Assets/_Project/Tests/Scenes/CamerasTest.unity";
        // Отсюда берутся игровые системы: GameLifetimeScope, меню паузы, настройки, EventSystem, пост-обработка
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private static readonly string[] GameSceneRoots = { "Scope", "Global Volume", "UI", "EventSystem" };
        private const string ScenePath = "Assets/_Project/Scenes/GameplayFlow.unity";
        private const string ConfigsFolder = "Assets/_Project/Configs/Levels";
        private const string NoteFontPath = "Assets/_Project/Resources/Fonts & Materials/RubikGlitch-Regular SDF.asset";
        private const string EnemiesConfigsFolder = "Assets/_Project/Tests/Configs/Enemies";

        private const float RoomSize = 18f;
        private const float WallHeight = 4f;
        private const float WallThickness = 0.5f;
        private const float GapWidth = 4f;
        private const float CorridorLength = 14f;
        private const float RoomStep = RoomSize + CorridorLength;
        private const float Half = RoomSize / 2f;

        private static Material _floorMaterial;
        private static Material _wallMaterial;
        private static Material _doorMaterial;
        private static Material _cameraMaterial;
        private static Material _weaponMaterial;
        private static Material _brokenCameraMaterial;
        private static Material _deadEnemyMaterial;
        private static Material _noteMaterial;

        private class RoomSetup
        {
            public string Name;
            public (EnemyConfig enemy, int count)[][] Waves;
            public float Delay = 5f;
        }

        [MenuItem("SibJam/AI Tools/Build Gameplay Flow Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(ConfigsFolder);
            CreateMaterials();

            // Перезаписываем содержимое, сохраняя .meta: GUID сцены не меняется (Build Settings, ссылки)
            if (File.Exists(ScenePath))
            {
                File.Copy(BaseScenePath, ScenePath, true);
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            }
            else
            {
                AssetDatabase.CopyAsset(BaseScenePath, ScenePath);
            }
            var scene = EditorSceneManager.OpenScene(ScenePath);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("Location") || root.name.StartsWith("Ground") || root.name == "PlayerArrivalPoint")
                    Object.DestroyImmediate(root);
            }

            var gameLoop = Object.FindAnyObjectByType<GameLoop>();
            foreach (var teleporter in Object.FindObjectsByType<PlayerTeleporter>(FindObjectsInactive.Include))
                Object.DestroyImmediate(teleporter);

            var player = Object.FindAnyObjectByType<CharacterController>();
            var weapon = CreateMeleeWeapon(player);

            var cheats = new GameObject("DebugCheats").AddComponent<SibJam.Debugging.DebugCheats>();
            SetObject(cheats, "_gameLoop", gameLoop);

            var rooms = CreateRoomSetups();
            var level = new GameObject("Level").transform;
            var locations = new List<LocationLoop>();
            var doors = new List<(LocationDoor entry, LocationDoor exit)>();

            // Комната 0 — сейф-комната без боя, дальше боевые локации
            var (safeRoomExit, weaponPickup) = BuildSafeRoom(level, weapon);

            for (int i = 0; i < rooms.Length; i++)
            {
                bool isLast = i == rooms.Length - 1;
                var (location, entryDoor, exitDoor) = BuildRoom(level, rooms[i], i + 1, !isLast);
                locations.Add(location);
                doors.Add((entryDoor, exitDoor));
            }

            for (int i = 0; i < rooms.Length; i++)
            {
                LocationLoop from = i == 0 ? null : locations[i - 1];
                LocationDoor exitDoor = i == 0 ? safeRoomExit : doors[i - 1].exit;
                BuildCorridor(level, gameLoop, i, from, i == 0 ? weaponPickup : null, exitDoor, doors[i].entry, locations[i]);
            }

            SetArray(gameLoop, "_locations", locations.ToArray());

            player.enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -Half + 2.5f), Quaternion.identity);
            player.enabled = true;

            ImportGameSystems(scene);
            AddToBuildSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameplayFlowBuilder] Сцена собрана: {ScenePath}");
        }

        // Делает GameplayFlow полноценной игровой сценой: переносит игровые системы из Game вместо тестового скоупа
        private static void ImportGameSystems(Scene scene)
        {
            var testScope = Object.FindAnyObjectByType<TestLifetimeScope>();
            var injected = testScope != null ? GetArray<GameObject>(testScope, "autoInjectGameObjects") : new List<GameObject>();

            if (testScope != null)
                Object.DestroyImmediate(testScope.gameObject);

            var gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);

            foreach (GameObject root in gameScene.GetRootGameObjects())
            {
                if (GameSceneRoots.Contains(root.name))
                    SceneManager.MoveGameObjectToScene(root, scene);
            }

            var gameScope = Object.FindObjectsByType<GameLifetimeScope>().First(s => s.gameObject.scene == scene);
            injected.AddRange(GetArray<GameObject>(gameScope, "autoInjectGameObjects").Where(go => go != null && go.scene == scene));
            SetArray(gameScope, "autoInjectGameObjects", injected.Distinct().ToArray());

            EditorSceneManager.CloseScene(gameScene, true);
            SceneManager.SetActiveScene(scene);
        }

        private static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static RoomSetup[] CreateRoomSetups()
        {
            var basic = Load<EnemyConfig>($"{EnemiesConfigsFolder}/TestEnemyConfig.asset");
            var medium = Load<EnemyConfig>($"{EnemiesConfigsFolder}/TestEnemy2Config.asset");
            var heavy = Load<EnemyConfig>($"{EnemiesConfigsFolder}/TestEnemy3Config.asset");
            var boss = CreateBossConfig(heavy);

            return new[]
            {
                new RoomSetup { Name = "Light", Delay = 4f, Waves = new[] { new[] { (basic, 2) } } },
                new RoomSetup { Name = "Location_1", Waves = new[] { new[] { (basic, 3) }, new[] { (basic, 2), (medium, 1) } } },
                new RoomSetup { Name = "Location_2", Waves = new[] { new[] { (basic, 3), (medium, 1) }, new[] { (basic, 2), (medium, 2) } } },
                new RoomSetup { Name = "Location_3", Waves = new[] { new[] { (medium, 2), (heavy, 1) }, new[] { (basic, 2), (heavy, 2) } } },
                new RoomSetup { Name = "Location_4", Waves = new[] { new[] { (medium, 3), (heavy, 2) }, new[] { (medium, 2), (heavy, 3) } } },
                new RoomSetup { Name = "Boss", Delay = 6f, Waves = new[] { new[] { (boss, 1), (basic, 2) }, new[] { (basic, 2), (medium, 1) } } },
            };
        }

        private static (LocationLoop, LocationDoor entry, LocationDoor exit) BuildRoom(
            Transform parent, RoomSetup setup, int index, bool hasExit)
        {
            float z = index * RoomStep;
            var room = new GameObject(setup.Name).transform;
            room.SetParent(parent);
            room.position = new Vector3(0f, 0f, z);

            CreateBlock("Floor", room, new Vector3(0f, -0.25f, 0f), new Vector3(RoomSize, 0.5f, RoomSize), _floorMaterial);
            CreateBlock("Wall_Left", room, new Vector3(-Half, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, RoomSize), _wallMaterial);
            CreateBlock("Wall_Right", room, new Vector3(Half, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, RoomSize), _wallMaterial);
            LocationDoor entryDoor = BuildDoorWall("Wall_Entry", room, -Half, true);
            LocationDoor exitDoor = BuildDoorWall("Wall_Exit", room, Half, hasExit);

            // Волны
            var waves = new GameObject("Waves");
            waves.transform.SetParent(room, false);
            var spawner = waves.AddComponent<WaveSpawner>();
            var spawnPoints = new[]
            {
                new Vector3(-5f, 1f, 5f), new Vector3(5f, 1f, 5f), new Vector3(-6f, 1f, 0f), new Vector3(6f, 1f, 0f), new Vector3(0f, 1f, 6f),
            }.Select((p, i) => CreatePoint($"SpawnPoint_{i + 1}", waves.transform, p, Quaternion.Euler(0f, 180f, 0f))).ToArray();
            SetArray(spawner, "_spawnPoints", spawnPoints);

            var wavesLoop = waves.AddComponent<WavesLoop>();
            SetObject(wavesLoop, "_wavesLoopConfig", CreateWavesConfig(setup));
            SetObject(wavesLoop, "_waveSpawner", spawner);

            // Камеры на высоте удара: две на боковых стенах, одна у стены выхода
            var cameras = new GameObject("SecurityCameras");
            cameras.transform.SetParent(room, false);
            var securityCameras = new[]
            {
                new Vector3(-Half + 0.6f, 1.9f, -2f), new Vector3(Half - 0.6f, 1.9f, 3f), new Vector3(-5f, 1.9f, Half - 0.6f),
            }.Select((p, i) => CreateSecurityCamera(cameras.transform, i + 1, p)).ToArray();
            var goal = cameras.AddComponent<CameraDestructionGoal>();
            SetArray(goal, "_securityCameras", securityCameras);

            var location = room.gameObject.AddComponent<LocationLoop>();
            SetObject(location, "_wavesLoop", wavesLoop);
            SetObject(location, "_cameraDestructionGoal", goal);
            SetObject(location, "_arrivalPoint", CreatePoint("ArrivalPoint", room, new Vector3(0f, 0.05f, -Half + 3f), Quaternion.identity));

            return (location, entryDoor, exitDoor);
        }

        private static LocationDoor BuildDoorWall(string name, Transform room, float z, bool withDoor)
        {
            if (!withDoor)
            {
                CreateBlock(name, room, new Vector3(0f, WallHeight / 2f, z), new Vector3(RoomSize, WallHeight, WallThickness), _wallMaterial);
                return null;
            }

            float sideWidth = (RoomSize - GapWidth) / 2f;
            float sideX = GapWidth / 2f + sideWidth / 2f;
            CreateBlock($"{name}_L", room, new Vector3(-sideX, WallHeight / 2f, z), new Vector3(sideWidth, WallHeight, WallThickness), _wallMaterial);
            CreateBlock($"{name}_R", room, new Vector3(sideX, WallHeight / 2f, z), new Vector3(sideWidth, WallHeight, WallThickness), _wallMaterial);

            var door = CreateBlock($"{name}_Door", room, new Vector3(0f, WallHeight / 2f, z), new Vector3(GapWidth, WallHeight, WallThickness * 0.8f), _doorMaterial);
            return door.AddComponent<LocationDoor>();
        }

        private static void BuildCorridor(Transform parent, GameLoop gameLoop, int index, LocationLoop from, WeaponPickup requiredPickup, LocationDoor exitDoor, LocationDoor entryDoor, LocationLoop to)
        {
            float startZ = index * RoomStep + Half;
            float centerZ = startZ + CorridorLength / 2f;
            var corridor = new GameObject($"Corridor_{index + 1}").transform;
            corridor.SetParent(parent);
            corridor.position = new Vector3(0f, 0f, centerZ);

            float wallX = GapWidth / 2f + WallThickness / 2f;
            CreateBlock("Floor", corridor, new Vector3(0f, -0.25f, 0f), new Vector3(GapWidth + WallThickness * 2f, 0.5f, CorridorLength), _floorMaterial);
            CreateBlock("Wall_Left", corridor, new Vector3(-wallX, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, CorridorLength), _wallMaterial);
            CreateBlock("Wall_Right", corridor, new Vector3(wallX, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, CorridorLength), _wallMaterial);

            // Зашёл чуть дальше выхода — выход закрывается; подошёл к дальней двери — локация стартует и дверь открывается;
            // прошёл в комнату — дверь закрывается
            var safeZone = CreateZone("SafeZone", corridor, -CorridorLength / 2f + 3f, 1f, GapWidth);
            var entryApproachZone = CreateZone("EntryApproachZone", corridor, CorridorLength / 2f - 2f, 2f, GapWidth);
            var roomEntryZone = CreateZone("RoomEntryZone", corridor, CorridorLength / 2f + 2.5f, 1f, RoomSize - 1f);

            var passage = corridor.gameObject.AddComponent<LocationPassage>();
            SetObject(passage, "_gameLoop", gameLoop);
            SetObject(passage, "_fromLocation", from);
            SetObject(passage, "_requiredPickup", requiredPickup);
            SetObject(passage, "_toLocation", to);
            SetObject(passage, "_exitDoor", exitDoor);
            SetObject(passage, "_entryDoor", entryDoor);
            SetObject(passage, "_safeZone", safeZone);
            SetObject(passage, "_entryApproachZone", entryApproachZone);
            SetObject(passage, "_roomEntryZone", roomEntryZone);
        }

        private static PassageZone CreateZone(string name, Transform corridor, float localZ, float depth, float width)
        {
            var zone = CreateBlock(name, corridor, new Vector3(0f, 1.5f, localZ), new Vector3(width, 3f, depth), null);
            zone.GetComponent<Collider>().isTrigger = true;
            return zone.AddComponent<PassageZone>();
        }

        // Стартовая сейф-комната: боя нет, выход открывается после подбора меча. Следы прошлого боя, оружие и записка с правилами
        private static (LocationDoor exit, WeaponPickup pickup) BuildSafeRoom(Transform parent, MeleeWeapon weapon)
        {
            var room = new GameObject("SafeRoom").transform;
            room.SetParent(parent);

            CreateBlock("Floor", room, new Vector3(0f, -0.25f, 0f), new Vector3(RoomSize, 0.5f, RoomSize), _floorMaterial);
            CreateBlock("Wall_Left", room, new Vector3(-Half, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, RoomSize), _wallMaterial);
            CreateBlock("Wall_Right", room, new Vector3(Half, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, RoomSize), _wallMaterial);
            BuildDoorWall("Wall_Entry", room, -Half, false);
            LocationDoor exitDoor = BuildDoorWall("Wall_Exit", room, Half, true);

            var decor = new GameObject("Decor").transform;
            decor.SetParent(room, false);

            // Разбитые камеры валяются под местами крепления
            var brokenCameras = new[] { new Vector3(-Half + 1f, 0.2f, -1f), new Vector3(Half - 1.2f, 0.2f, 4f), new Vector3(-4f, 0.2f, Half - 1f) };
            for (int i = 0; i < brokenCameras.Length; i++)
            {
                var brokenCamera = CreateBlock($"BrokenCamera_{i + 1}", decor, brokenCameras[i], new Vector3(0.6f, 0.4f, 0.6f), _brokenCameraMaterial);
                brokenCamera.transform.localRotation = Quaternion.Euler(25f, 40f * i + 15f, 30f);
            }

            // Убитые враги — лежащие капсулы
            var deadEnemies = new[] { new Vector3(-4f, 0.5f, 3f), new Vector3(5f, 0.5f, -2f), new Vector3(2f, 0.5f, 6f) };
            for (int i = 0; i < deadEnemies.Length; i++)
            {
                var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.name = $"DeadEnemy_{i + 1}";
                enemy.transform.SetParent(decor, false);
                enemy.transform.SetLocalPositionAndRotation(deadEnemies[i], Quaternion.Euler(90f, 70f * i + 20f, 0f));
                enemy.GetComponent<MeshRenderer>().sharedMaterial = _deadEnemyMaterial;
            }

            WeaponPickup pickup = BuildWeaponPickup(room, weapon, new Vector3(0f, 1f, -Half + 7f));
            BuildNote(room, new Vector3(-2.5f, 0f, -Half + 5f));
            return (exitDoor, pickup);
        }

        private static void BuildNote(Transform parent, Vector3 localPosition)
        {
            var note = new GameObject("GuideNote").transform;
            note.SetParent(parent, false);
            // Лицом к стартовой точке игрока
            note.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(0f, 160f, 0f));

            CreateBlock("Stand", note, new Vector3(0f, 0.6f, -0.12f), new Vector3(0.15f, 1.2f, 0.15f), _wallMaterial);
            CreateBlock("Board", note, new Vector3(0f, 1.6f, 0f), new Vector3(2.6f, 1.7f, 0.08f), _noteMaterial);

            var text = new GameObject("Text").AddComponent<TextMeshPro>();
            text.transform.SetParent(note, false);
            // Перед доской со стороны игрока; TMP читается вдоль -forward, поэтому разворачиваем
            text.transform.SetLocalPositionAndRotation(new Vector3(0f, 1.6f, 0.05f), Quaternion.Euler(0f, 180f, 0f));
            text.rectTransform.sizeDelta = new Vector2(2.4f, 1.5f);
            text.font = Load<TMP_FontAsset>(NoteFontPath);
            text.fontSize = 0.95f;
            text.color = new Color(0.1f, 0.1f, 0.1f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.text =
                "<b>ВНИМАНИЕ</b>\n" +
                "Камеры анализируют вас и ваше сражение, предоставляя данные " +
                "для создания более совершенных охранников. С каждой волной вам будет сложнее.\n\n" +
                "WASD — перемещение\n" +
                "ЛКМ — атаковать\n\n" +
                "1. Разбей все камеры — волны прекратятся\n" +
                "2. Добей оставшихся — откроется выход\n" +
                "3. Коридор — безопасная зона, отдышись";
        }

        private static WeaponPickup BuildWeaponPickup(Transform parent, MeleeWeapon weapon, Vector3 localPosition)
        {
            // Корень без масштаба, иначе масштаб модели раздувает радиус триггера
            var pickup = new GameObject("WeaponPickup");
            pickup.transform.SetParent(parent, false);
            pickup.transform.localPosition = localPosition;
            var model = CreateSword("Sword", pickup.transform);
            model.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            var trigger = pickup.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;
            var weaponPickup = pickup.AddComponent<WeaponPickup>();
            SetObject(weaponPickup, "_weapon", weapon);
            return weaponPickup;
        }

        // Меч из примитивов, без коллайдеров. Пивот — у рукояти, клинок вдоль +Y
        private static GameObject CreateSword(string name, Transform parent)
        {
            var sword = new GameObject(name);
            sword.transform.SetParent(parent, false);

            var parts = new[]
            {
                CreateBlock("Grip", sword.transform, new Vector3(0f, -0.02f, 0f), new Vector3(0.05f, 0.22f, 0.05f), _wallMaterial),
                CreateBlock("Guard", sword.transform, new Vector3(0f, 0.1f, 0f), new Vector3(0.3f, 0.05f, 0.07f), _wallMaterial),
                CreateBlock("Blade", sword.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.08f, 0.95f, 0.02f), _weaponMaterial),
            };

            foreach (GameObject part in parts)
                Object.DestroyImmediate(part.GetComponent<Collider>());

            return sword;
        }

        // Меняет тестовый луч на оружие ближнего боя; модель — в руке у камеры, видна после подбора
        private static MeleeWeapon CreateMeleeWeapon(CharacterController player)
        {
            var raycast = Object.FindAnyObjectByType<RaycastEnemyDamager>(FindObjectsInactive.Include);
            var aim = raycast != null
                ? (Transform)new SerializedObject(raycast).FindProperty("_aimTransform").objectReferenceValue
                : null;

            if (aim == null)
                aim = Camera.main.transform;

            if (raycast != null)
                Object.DestroyImmediate(raycast);

            // Хват как у меча: рукоять внизу справа, клинок вверх и чуть вперёд-влево
            var model = CreateSword("SwordInHand", aim);
            model.transform.SetLocalPositionAndRotation(new Vector3(0.4f, -0.5f, 0.75f), Quaternion.Euler(35f, 0f, 25f));
            model.SetActive(false);

            var weapon = player.gameObject.AddComponent<MeleeWeapon>();
            SetObject(weapon, "_aimTransform", aim);
            SetObject(weapon, "_model", model.transform);
            weapon.enabled = false;
            return weapon;
        }

        private static SecurityCamera CreateSecurityCamera(Transform parent, int number, Vector3 localPosition)
        {
            var go = CreateBlock($"SecurityCamera_{number}", parent, localPosition, new Vector3(0.6f, 0.6f, 0.6f), _cameraMaterial);
            return go.AddComponent<SecurityCamera>();
        }

        private static WavesLoopConfig CreateWavesConfig(RoomSetup setup)
        {
            var config = ScriptableObject.CreateInstance<WavesLoopConfig>();
            config.RepeatLastWave = true;

            for (int i = 0; i < setup.Waves.Length; i++)
            {
                var wave = ScriptableObject.CreateInstance<WaveConfig>();
                wave.Enemies = setup.Waves[i].Select(e => new WaveEnemyEntry { Enemy = e.enemy, Count = e.count }).ToList();
                CreateAsset(wave, $"{ConfigsFolder}/{setup.Name}_Wave_{i + 1}.asset");
                config.WavesWithDelay.Add(new WaveWithDelayConfig { Wave = wave, DelayInSeconds = setup.Delay });
            }

            return CreateAsset(config, $"{ConfigsFolder}/{setup.Name}_WavesLoop.asset");
        }

        private static EnemyConfig CreateBossConfig(EnemyConfig source)
        {
            string prefabPath = $"{ConfigsFolder}/Boss.prefab";
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source.Prefab.gameObject);
            instance.transform.localScale = Vector3.one * 2.5f;
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            var config = Object.Instantiate(source);
            var serialized = new SerializedObject(config);
            serialized.FindProperty("<Prefab>k__BackingField").objectReferenceValue = prefab.GetComponent<Enemy>();
            serialized.FindProperty("<MaxHealth>k__BackingField").floatValue = 1500f;
            serialized.FindProperty("<Damage>k__BackingField").floatValue = 30f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return CreateAsset(config, $"{ConfigsFolder}/BossConfig.asset");
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;

            if (material != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
            else
                Object.DestroyImmediate(go.GetComponent<MeshRenderer>());

            return go;
        }

        private static Transform CreatePoint(string name, Transform parent, Vector3 localPosition, Quaternion rotation)
        {
            var point = new GameObject(name).transform;
            point.SetParent(parent, false);
            point.SetLocalPositionAndRotation(localPosition, rotation);
            return point;
        }

        private static void CreateMaterials()
        {
            _floorMaterial = CreateMaterial("Floor", new Color(0.35f, 0.36f, 0.38f));
            _wallMaterial = CreateMaterial("Wall", new Color(0.18f, 0.2f, 0.24f));
            _doorMaterial = CreateMaterial("Door", new Color(0.95f, 0.7f, 0.1f));
            _cameraMaterial = CreateMaterial("SecurityCamera", new Color(0.9f, 0.1f, 0.1f));
            _weaponMaterial = CreateMaterial("Weapon", new Color(0.1f, 0.85f, 0.95f));
            _brokenCameraMaterial = CreateMaterial("BrokenCamera", new Color(0.25f, 0.08f, 0.08f));
            _deadEnemyMaterial = CreateMaterial("DeadEnemy", new Color(0.3f, 0.25f, 0.22f));
            _noteMaterial = CreateMaterial("Note", new Color(0.92f, 0.88f, 0.75f));
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = GraphicsSettings.currentRenderPipeline != null
                ? GraphicsSettings.currentRenderPipeline.defaultMaterial.shader
                : Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            return CreateAsset(material, $"{ConfigsFolder}/{name}.mat");
        }

        private static T CreateAsset<T>(T asset, string path) where T : Object
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void SetObject(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<T> GetArray<T>(Object target, string field) where T : Object
        {
            var property = new SerializedObject(target).FindProperty(field);
            var values = new List<T>();

            for (int i = 0; i < property.arraySize; i++)
                values.Add(property.GetArrayElementAtIndex(i).objectReferenceValue as T);

            return values;
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            property.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int slash = path.LastIndexOf('/');
            EnsureFolder(path[..slash]);
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }
    }
}
