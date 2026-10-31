using System.Linq;
using _Project.Scripts.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SibJam.Editor
{
    // Враги на моделях Mob1/Mob2: префабы с Idle-анимацией и конфиги со статами тестовых врагов
    public static partial class GameplayFlowBuilder
    {
        private const string EnemyModelsFolder = "Assets/_Project/Art/Models/Enemies";
        private const string EnemyPrefabsFolder = "Assets/_Project/Prefabs/Enemies";
        private const string EnemyConfigsFolder = "Assets/_Project/Configs/Enemies";
        private const string MeleeModel = "Mob1Animations";
        private const string GunnerModel = "Mob2Animations";

        private static (EnemyConfig melee, EnemyConfig gunner, EnemyConfig brute) CreateEnemies()
        {
            EnsureFolder(EnemyPrefabsFolder);
            EnsureFolder(EnemyConfigsFolder);
            SetupModelImport(MeleeModel);
            SetupModelImport(GunnerModel);

            var melee = CreateEnemy("Melee", MeleeModel, 1f, _mobMeleeMaterial, Load<EnemyConfig>($"{EnemiesConfigsFolder}/TestEnemyConfig.asset"));
            var gunner = CreateEnemy("Gunner", GunnerModel, 1f, _mobGunnerMaterial, Load<EnemyConfig>($"{EnemiesConfigsFolder}/TestEnemy2Config.asset"));
            var brute = CreateEnemy("Brute", MeleeModel, 1.35f, _mobBruteMaterial, Load<EnemyConfig>($"{EnemiesConfigsFolder}/TestEnemy3Config.asset"));
            return (melee, gunner, brute);
        }

        // Пивот префаба — у ног, модель смотрит по +Z. Статы копируются из тестового конфига
        private static EnemyConfig CreateEnemy(string name, string model, float scale, Material material, EnemyConfig stats)
        {
            var root = new GameObject($"Enemy{name}");
            root.transform.localScale = Vector3.one * scale;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1f, 0f);
            capsule.height = 2f;
            capsule.radius = 0.45f;
            root.AddComponent<Enemy>();

            var modelInstance = InstantiateModel(model, root.transform, material);
            if (!modelInstance.TryGetComponent(out Animator animator))
                animator = modelInstance.AddComponent<Animator>();

            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath(model)).OfType<Avatar>().FirstOrDefault();
            animator.runtimeAnimatorController = CreateIdleController(model);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            SetObject(root.AddComponent<RandomAnimationStart>(), "_animator", animator);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{EnemyPrefabsFolder}/Enemy{name}.prefab");
            Object.DestroyImmediate(root);

            var config = Object.Instantiate(stats);
            var serialized = new SerializedObject(config);
            serialized.FindProperty("<Prefab>k__BackingField").objectReferenceValue = prefab.GetComponent<Enemy>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return CreateAsset(config, $"{EnemyConfigsFolder}/Enemy{name}Config.asset");
        }

        private static GameObject InstantiateModel(string model, Transform parent, Material material)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(ModelPath(model)), parent);
            go.name = "Model";

            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = material;

            return go;
        }

        // Неподвижная модель в позе Idle — для декора (колбы, конвейер, тела)
        private static GameObject BuildStaticMob(string name, Transform parent, string model, Vector3 localPosition, Vector3 euler, float scale, Material material)
        {
            var go = InstantiateModel(model, parent, material);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            LoadClip(model, "Idle").SampleAnimation(go, 0.5f);

            if (go.TryGetComponent(out Animator animator))
                Object.DestroyImmediate(animator);

            go.name = name;
            go.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(euler));
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static void SetupModelImport(string model)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath(model));
            var clips = importer.defaultClipAnimations;

            foreach (var clip in clips)
                clip.loopTime = clip.name is "Idle" or "Walking";

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        // Контроллер с одним зацикленным Idle; существующий переписывается на месте, GUID не меняется
        private static AnimatorController CreateIdleController(string model)
        {
            string path = $"{EnemyModelsFolder}/{model}.controller";
            var controller = Load<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            var stateMachine = controller.layers[0].stateMachine;

            foreach (var state in stateMachine.states)
                stateMachine.RemoveState(state.state);

            var idle = stateMachine.AddState("Idle");
            idle.motion = LoadClip(model, "Idle");
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip LoadClip(string model, string clip) =>
            AssetDatabase.LoadAllAssetsAtPath(ModelPath(model)).OfType<AnimationClip>().First(c => c.name == clip);

        private static string ModelPath(string model) => $"{EnemyModelsFolder}/{model}.fbx";
    }
}
