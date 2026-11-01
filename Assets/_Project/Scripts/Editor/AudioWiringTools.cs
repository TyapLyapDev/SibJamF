using System.Linq;
using SibJam.SecurityCameras;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace SibJam.Editor
{
    // Раскладывает звуки из Audio/Sfx/List по игроку, врагам, камерам наблюдения и эмбиенту. Можно перезапускать после замены файлов
    public static class AudioWiringTools
    {
        private const string Sfx = "Assets/_Project/Audio/Sfx/List/SFX/";
        private const string Prefabs = "Assets/_Project/Prefabs/";
        private static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Gameplay.unity",
            "Assets/_Project/Scenes/GameplayFlow.unity",
            "Assets/_Project/Scenes/Main.unity"
        };

        [MenuItem("SibJam/Audio/Wire Game Sounds")]
        public static void WireAll()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/_Project/Audio/Mixer.mixer");
            AudioMixerGroup sfxGroup = mixer.FindMatchingGroups(MixerNames.Sfx).FirstOrDefault();

            WirePlayer(sfxGroup);
            WireEnemy("EnemyMelee", sfxGroup, 1f,
                steps: "Mob1/Mob1_Walking/Mob1_Walking_Steps", body: "Mob1/Mob1_Walking/Mob1_Walking_Body",
                voice: "Mob1/Mob1_Walking/Mob1_Walking_Voice", windup: "Mob1/Mob1_Attacking/Mob1_Attaking_Voice",
                release: "Mob1/Mob1_Attacking/Mob1_Attaking_Body.wav", releaseBody: null,
                death: "Mob1/Mob1_Attacking/Mob1_Attaking_Voice");
            WireEnemy("EnemyBrute", sfxGroup, 0.78f,
                steps: "Boss/Boss_Walking/Boss_Walking_Steps", body: "Mob1/Mob1_Walking/Mob1_Walking_Body",
                voice: "Mob1/Mob1_Walking/Mob1_Walking_Voice", windup: "Mob1/Mob1_Attacking/Mob1_Attaking_Voice",
                release: "Mob1/Mob1_Attacking/Mob1_Attaking_Body.wav", releaseBody: "Mob2/Mob2_HitWall/Mob2_HitWall_Hit",
                death: "Mob1/Mob1_Attacking/Mob1_Attaking_Voice");
            WireEnemy("EnemyGunner", sfxGroup, 1f,
                steps: "Mob2/Mob2_Walking/Mob2_Walking_Steps", body: "Mob2/Mob2_Walking/Mob2_Walking_Body.wav",
                voice: null, windup: "Mob2/Mob2_Shooting/Mob2_Shooting_Gun",
                release: "Mob2/Mob2_Shooting/Mob2_Shooting_Shot", releaseBody: null,
                death: "Mob2/Mob2_HitWall/Mob2_HitWall_Hit");
            WireMusic();
            WireSecurityCameras(sfxGroup);

            AssetDatabase.SaveAssets();
            Debug.Log("[AudioWiring] Звуки разложены");
        }

        private static void WirePlayer(AudioMixerGroup group)
        {
            EditPrefab(Prefabs + "Player.prefab", root =>
            {
                SfxEmitter emitter = GetOrAdd<SfxEmitter>(root);
                SetEmitter(emitter, group, is2D: true, voices: 8);

                var so = new SerializedObject(GetOrAdd<PlayerSfx>(root));
                so.FindProperty("_sfx").objectReferenceValue = emitter;
                SetSound(so, "_stepSound", Clips("Footsteps/Steps"), 0.35f, 0.45f, 0.92f, 1.08f);
                SetSound(so, "_stepBodySound", Clips("Footsteps/Body"), 0.2f, 0.3f, 0.95f, 1.05f);
                SetSound(so, "_swingSound", Clips("SwordWhoosh"), 0.75f, 0.9f, 0.92f, 1.1f);
                SetSound(so, "_swingBodySound", Clips("SwordBodyMove"), 0.4f, 0.5f, 0.95f, 1.05f);
                SetSound(so, "_hitSound", Clips("HitEnemy"), 0.9f, 1f, 0.9f, 1.1f);
                SetSound(so, "_blockSound", Clips("HitWall"), 0.8f, 0.9f, 0.95f, 1.1f);
                SetSound(so, "_parrySound", Clips("WeaponParry.wav"), 1f, 1f, 0.95f, 1.08f);
                SetSound(so, "_damageSound", Clips("Damage"), 0.85f, 1f, 0.95f, 1.05f);
                SetSound(so, "_deathSound", Clips("Death.wav"), 1f, 1f, 1f, 1f);
                SetSound(so, "_lowHealthLoop", Clips("../UI/MaloHP.wav"), 0.6f, 0.6f, 1f, 1f);
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void WireEnemy(string prefab, AudioMixerGroup group, float pitch, string steps, string body,
            string voice, string windup, string release, string releaseBody, string death)
        {
            EditPrefab(Prefabs + "Enemies/" + prefab + ".prefab", root =>
            {
                SfxEmitter emitter = GetOrAdd<SfxEmitter>(root);
                SetEmitter(emitter, group, is2D: false, voices: 5);

                var so = new SerializedObject(GetOrAdd<EnemySfx>(root));
                so.FindProperty("_sfx").objectReferenceValue = emitter;
                SetSound(so, "_stepSound", Clips(steps), 0.35f, 0.45f, pitch * 0.92f, pitch * 1.08f);
                SetSound(so, "_stepBodySound", Clips(body), 0.25f, 0.35f, pitch * 0.95f, pitch * 1.05f);
                SetSound(so, "_idleVoice", Clips(voice), 0.5f, 0.7f, pitch * 0.9f, pitch * 1.1f);
                SetSound(so, "_windupSound", Clips(windup), 0.8f, 1f, pitch * 0.95f, pitch * 1.05f);
                SetSound(so, "_releaseSound", Clips(release), 0.85f, 1f, pitch * 0.92f, pitch * 1.08f);
                SetSound(so, "_releaseBodySound", Clips(releaseBody), 0.6f, 0.8f, pitch * 0.95f, pitch * 1.05f);
                SetSound(so, "_deathSound", Clips(death), 0.9f, 1f, pitch * 0.7f, pitch * 0.8f);
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void WireMusic()
        {
            EditPrefab(Prefabs + "DI/ProjectScope.prefab", root =>
            {
                Music music = root.GetComponentInChildren<Music>(true);

                if (music == null)
                    return;

                var so = new SerializedObject(music);
                SerializedProperty musicSource = so.FindProperty("_source");
                SerializedProperty ambienceSource = so.FindProperty("_ambienceSource");

                if (ambienceSource.objectReferenceValue == null)
                {
                    var holder = new GameObject("Ambience");
                    holder.transform.SetParent(music.transform, false);
                    AudioSource source = holder.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = true;
                    source.spatialBlend = 0f;
                    source.volume = 0.35f;

                    if (musicSource.objectReferenceValue is AudioSource reference)
                        source.outputAudioMixerGroup = reference.outputAudioMixerGroup;

                    ambienceSource.objectReferenceValue = source;
                }

                so.FindProperty("_ambience").objectReferenceValue = Clips("../ATM/AMB_Spaceship.wav").FirstOrDefault();
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void WireSecurityCameras(AudioMixerGroup group)
        {
            AudioClip[] hits = Clips("HitWall");
            AudioClip[] breaks = Clips("CameraBreaking");

            foreach (string path in Scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool wasLoaded = scene.isLoaded;

                if (!wasLoaded)
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                SecurityCamera[] cameras = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<SecurityCamera>(true)).ToArray();

                foreach (SecurityCamera camera in cameras)
                {
                    var so = new SerializedObject(camera);
                    so.FindProperty("_sfxOutput").objectReferenceValue = group;
                    SetSound(so, "_hitSound", hits, 0.6f, 0.75f, 1.1f, 1.3f);
                    SetSound(so, "_breakSound", breaks, 1f, 1f, 0.95f, 1.05f);
                    so.ApplyModifiedProperties();
                }

                if (cameras.Length > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }

                if (!wasLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void EditPrefab(string path, System.Action<GameObject> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component =>
            root.TryGetComponent(out T component) ? component : root.AddComponent<T>();

        private static void SetEmitter(SfxEmitter emitter, AudioMixerGroup group, bool is2D, int voices)
        {
            var so = new SerializedObject(emitter);
            so.FindProperty("_output").objectReferenceValue = group;
            so.FindProperty("_voices").intValue = voices;
            so.FindProperty("_spatialBlend").floatValue = is2D ? 0f : 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Путь от папки SFX: файл или папка (все клипы внутри, включая подпапки)
        private static AudioClip[] Clips(string relative)
        {
            if (string.IsNullOrEmpty(relative))
                return new AudioClip[0];

            string path = System.IO.Path.GetFullPath(Sfx + relative).Replace('\\', '/');
            path = path.Substring(path.IndexOf("Assets/"));

            if (path.EndsWith(".wav"))
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

                if (clip == null)
                    Debug.LogWarning($"[AudioWiring] Нет клипа {path}");

                return clip != null ? new[] { clip } : new AudioClip[0];
            }

            return AssetDatabase.FindAssets("t:AudioClip", new[] { path })
                .Select(guid => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid)))
                .OrderBy(clip => clip.name)
                .ToArray();
        }

        private static void SetSound(SerializedObject so, string field, AudioClip[] clips, float minVolume, float maxVolume, float minPitch, float maxPitch)
        {
            SerializedProperty sound = so.FindProperty(field);
            SerializedProperty array = sound.FindPropertyRelative(nameof(SoundSet.Clips));
            array.arraySize = clips.Length;

            for (int i = 0; i < clips.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];

            sound.FindPropertyRelative(nameof(SoundSet.Volume)).vector2Value = new Vector2(minVolume, maxVolume);
            sound.FindPropertyRelative(nameof(SoundSet.Pitch)).vector2Value = new Vector2(minPitch, maxPitch);
        }
    }
}
