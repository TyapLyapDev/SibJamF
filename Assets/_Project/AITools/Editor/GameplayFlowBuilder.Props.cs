using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SibJam.Editor
{
    // Материалы декора и пропсы из примитивов. У пропса корень без масштаба: детали — только визуал, коллайдер один на корне (Solid)
    public static partial class GameplayFlowBuilder
    {
        private const string DecorMaterialsFolder = ConfigsFolder + "/Materials";

        // Сторона комнаты: точка на внутренней грани стены, +forward смотрит в комнату
        private enum Side { Left, Right, Entry, Exit }

        private static Material _floorPanelMaterial;
        private static Material _trimMaterial;
        private static Material _metalMaterial;
        private static Material _darkMetalMaterial;
        private static Material _woodMaterial;
        private static Material _cardboardMaterial;
        private static Material _concreteMaterial;
        private static Material _hazardMaterial;
        private static Material _rubberMaterial;
        private static Material _redPaintMaterial;
        private static Material _bluePaintMaterial;
        private static Material _screenMaterial;
        private static Material _glassMaterial;
        private static Material _liquidMaterial;
        private static Material _lampMaterial;
        private static Material _ledMaterial;
        private static Material _signalMaterial;
        private static Material _safeMaterial;
        private static Material _puddleMaterial;
        private static Material _fabricMaterial;
        private static Material _whiteMaterial;
        private static Material _medicalMaterial;
        private static Material _specimenMaterial;
        private static Material _mobMeleeMaterial;
        private static Material _mobGunnerMaterial;
        private static Material _mobBruteMaterial;
        private static Material _bossMaterial;
        private static Material _skyMaterial;

        private static void CreateDecorMaterials()
        {
            EnsureFolder(DecorMaterialsFolder);

            _floorPanelMaterial = CreateDecorMaterial("FloorPanel", new Color(0.26f, 0.27f, 0.29f), 0.5f, 0.2f);
            _trimMaterial = CreateDecorMaterial("Trim", new Color(0.09f, 0.1f, 0.12f), 0.4f, 0.5f);
            _metalMaterial = CreateDecorMaterial("Metal", new Color(0.5f, 0.52f, 0.55f), 0.55f, 0.6f);
            _darkMetalMaterial = CreateDecorMaterial("DarkMetal", new Color(0.13f, 0.14f, 0.16f), 0.45f, 0.5f);
            _woodMaterial = CreateDecorMaterial("Wood", new Color(0.5f, 0.34f, 0.18f), 0.15f);
            _cardboardMaterial = CreateDecorMaterial("Cardboard", new Color(0.6f, 0.47f, 0.3f), 0.1f);
            _concreteMaterial = CreateDecorMaterial("Concrete", new Color(0.45f, 0.44f, 0.41f), 0.1f);
            _hazardMaterial = CreateDecorMaterial("HazardBlack", new Color(0.04f, 0.04f, 0.04f), 0.3f);
            _rubberMaterial = CreateDecorMaterial("Rubber", new Color(0.03f, 0.03f, 0.035f), 0.2f);
            _redPaintMaterial = CreateDecorMaterial("RedPaint", new Color(0.62f, 0.1f, 0.08f), 0.4f, 0.3f);
            _bluePaintMaterial = CreateDecorMaterial("BluePaint", new Color(0.12f, 0.28f, 0.55f), 0.4f, 0.3f);
            _screenMaterial = CreateDecorMaterial("Screen", new Color(0.02f, 0.04f, 0.06f), 0.9f, 0f, 0.6f);
            _glassMaterial = CreateDecorMaterial("Glass", new Color(0.7f, 0.9f, 1f, 0.15f), 0.95f, 0f, 0f, true);
            _liquidMaterial = CreateDecorMaterial("Liquid", new Color(0.3f, 1f, 0.5f, 0.35f), 0.9f, 0f, 0.8f, true);
            _lampMaterial = CreateDecorMaterial("Lamp", new Color(1f, 0.95f, 0.85f), 0.5f, 0f, 4f);
            _ledMaterial = CreateDecorMaterial("Led", new Color(0.2f, 1f, 0.4f), 0.5f, 0f, 1.5f);
            _signalMaterial = CreateDecorMaterial("Signal", new Color(1f, 0.35f, 0.1f), 0.5f, 0f, 2f);
            _safeMaterial = CreateDecorMaterial("SafeZone", new Color(0.3f, 1f, 0.5f), 0.5f, 0f, 1.3f);
            _puddleMaterial = CreateDecorMaterial("Puddle", new Color(0.02f, 0.02f, 0.02f), 0.95f);
            _fabricMaterial = CreateDecorMaterial("Fabric", new Color(0.28f, 0.32f, 0.27f), 0.05f);
            _whiteMaterial = CreateDecorMaterial("White", new Color(0.85f, 0.86f, 0.88f), 0.3f);
            _medicalMaterial = CreateDecorMaterial("MedicalRed", new Color(0.8f, 0.05f, 0.05f), 0.3f, 0f, 0.5f);
            _specimenMaterial = CreateDecorMaterial("Specimen", new Color(0.72f, 0.78f, 0.75f), 0.6f);
            _mobMeleeMaterial = CreateDecorMaterial("Mob_Melee", new Color(0.3f, 0.42f, 0.6f), 0.5f, 0.4f);
            _mobGunnerMaterial = CreateDecorMaterial("Mob_Gunner", new Color(0.32f, 0.36f, 0.26f), 0.4f, 0.3f);
            _mobBruteMaterial = CreateDecorMaterial("Mob_Brute", new Color(0.5f, 0.13f, 0.1f), 0.45f, 0.4f);
            _bossMaterial = CreateDecorMaterial("Mob_Boss", new Color(0.35f, 0.05f, 0.05f), 0.6f, 0.5f, 0.4f);
            _skyMaterial = CreateSkyMaterial();
        }

        private static Material CreateDecorMaterial(string name, Color color, float smoothness = 0.2f, float metallic = 0f,
            float emission = 0f, bool transparent = false) =>
            CreateMaterial(name, color, smoothness, metallic, emission, transparent, DecorMaterialsFolder);

        // Настройки URP Lit: гладкость, металл, свечение (цвет × интенсивность, для bloom), прозрачность
        private static void ApplyMaterialSettings(Material material, Color color, float smoothness, float metallic, float emission, bool transparent)
        {
            material.color = color;
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);

            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            material.SetFloat("_Surface", transparent ? 1f : 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", transparent ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
            material.SetFloat("_DstBlend", transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            material.SetFloat("_SrcBlendAlpha", transparent ? (float)BlendMode.One : (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
            material.SetShaderPassEnabled("DepthOnly", !transparent);
            material.SetShaderPassEnabled("ShadowCaster", !transparent);
            material.renderQueue = transparent ? (int)RenderQueue.Transparent : -1;

            if (transparent)
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            else
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static Material CreateSkyMaterial()
        {
            string path = $"{DecorMaterialsFolder}/Sky.mat";
            var sky = Load<Material>(path);

            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, path);
            }

            // Сумерки: низкое тёплое солнце, лиловое небо
            sky.shader = Shader.Find("Skybox/Procedural");
            sky.SetFloat("_SunSize", 0.05f);
            sky.SetFloat("_AtmosphereThickness", 1.6f);
            sky.SetColor("_SkyTint", new Color(0.42f, 0.36f, 0.55f));
            sky.SetColor("_GroundColor", new Color(0.12f, 0.12f, 0.14f));
            sky.SetFloat("_Exposure", 0.85f);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        // ---------- Примитивы ----------

        private static Transform Group(string name, Transform parent, Vector3 localPosition, float yaw = 0f)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(0f, yaw, 0f));
            return group;
        }

        private static Transform OnWall(string name, Transform room, Side side, float along, float y = 0f)
        {
            float face = Half - WallThickness / 2f;

            var (position, yaw) = side switch
            {
                Side.Left => (new Vector3(-face, y, along), 90f),
                Side.Right => (new Vector3(face, y, along), -90f),
                Side.Entry => (new Vector3(along, y, -face), 0f),
                _ => (new Vector3(along, y, face), 180f),
            };

            return Group(name, room, position, yaw);
        }

        // Визуальная деталь без коллайдера
        private static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material, Vector3 euler = default)
        {
            var go = CreateBlock(name, parent, localPosition, size, material);
            go.transform.localRotation = Quaternion.Euler(euler);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // Цилиндр вдоль локальной Y (до поворота): диаметр и полная длина
        private static GameObject Cylinder(string name, Transform parent, Vector3 localPosition, float diameter, float length, Material material, Vector3 euler = default)
        {
            var go = Primitive(PrimitiveType.Cylinder, name, parent, localPosition, new Vector3(diameter, length / 2f, diameter), material);
            go.transform.localRotation = Quaternion.Euler(euler);
            return go;
        }

        private static GameObject Sphere(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material) =>
            Primitive(PrimitiveType.Sphere, name, parent, localPosition, size, material);

        // Цилиндр между двумя точками — трубы, кабели, стержни
        private static GameObject Rod(string name, Transform parent, Vector3 from, Vector3 to, float diameter, Material material)
        {
            Vector3 direction = to - from;
            var go = Cylinder(name, parent, (from + to) / 2f, diameter, direction.magnitude, material);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
            return go;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Solid(Transform prop, Vector3 center, Vector3 size)
        {
            var collider = prop.gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private static Light CreateLight(Transform parent, Vector3 localPosition, Color color, float intensity, float range)
        {
            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = localPosition;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        // TMP читается вдоль -forward: текст развёрнут к зрителю, который стоит перед родителем (по его +forward)
        private static TextMeshPro CreateText(string name, Transform parent, Vector3 localPosition, string content, Color color, Vector2 size, float maxFontSize = 3.5f)
        {
            var text = new GameObject(name).AddComponent<TextMeshPro>();
            text.transform.SetParent(parent, false);
            text.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(0f, 180f, 0f));
            text.rectTransform.sizeDelta = size;
            text.font = Load<TMP_FontAsset>(NoteFontPath);
            text.enableAutoSizing = true;
            text.fontSizeMin = 0.3f;
            text.fontSizeMax = maxFontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.text = content;
            return text;
        }

        // ---------- Пропсы ----------

        private static Transform BuildCrate(Transform parent, Vector3 position, float yaw, float size = 1f, Material material = null)
        {
            var crate = Group("Crate", parent, position, yaw);
            Vector3 center = new(0f, size / 2f, 0f);
            Box("Body", crate, center, Vector3.one * size, material ?? _woodMaterial);

            foreach (float height in new[] { 0.12f, 0.88f })
                Box("Strap", crate, new Vector3(0f, size * height, 0f), new Vector3(size + 0.03f, size * 0.07f, size + 0.03f), _darkMetalMaterial);

            // Диагональные доски на гранях
            Box("Brace_Z", crate, center, new Vector3(size * 1.2f, size * 0.1f, size + 0.02f), _darkMetalMaterial, new Vector3(0f, 0f, 45f));
            Box("Brace_X", crate, center, new Vector3(size + 0.02f, size * 0.1f, size * 1.2f), _darkMetalMaterial, new Vector3(45f, 0f, 0f));
            Solid(crate, center, Vector3.one * size);
            return crate;
        }

        private static Transform BuildBarrel(Transform parent, Vector3 position, Material paint, Vector3 euler = default)
        {
            var barrel = Group("Barrel", parent, position);
            barrel.localRotation = Quaternion.Euler(euler);
            Cylinder("Body", barrel, new Vector3(0f, 0.45f, 0f), 0.6f, 0.9f, paint);

            foreach (float height in new[] { 0.15f, 0.75f })
                Cylinder("Ring", barrel, new Vector3(0f, height, 0f), 0.63f, 0.05f, _darkMetalMaterial);

            Cylinder("Lid", barrel, new Vector3(0f, 0.9f, 0f), 0.5f, 0.02f, _darkMetalMaterial);
            Solid(barrel, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.6f));
            return barrel;
        }

        private static void BuildBarrelCluster(Transform parent, Vector3 position, params Material[] paints)
        {
            var offsets = new[] { new Vector3(0f, 0f, 0f), new Vector3(0.64f, 0f, 0f), new Vector3(0.32f, 0f, 0.56f), new Vector3(-0.3f, 0f, 0.62f) };

            for (int i = 0; i < offsets.Length; i++)
                BuildBarrel(parent, position + offsets[i], paints[i % paints.Length]);
        }

        private static void BuildPallet(Transform parent, Vector3 position, float yaw)
        {
            var pallet = Group("Pallet", parent, position, yaw);

            for (int i = 0; i < 5; i++)
                Box("Slat", pallet, new Vector3(0f, 0.13f, -0.44f + i * 0.22f), new Vector3(1.2f, 0.03f, 0.14f), _woodMaterial);

            for (int i = -1; i <= 1; i++)
                Box("Beam", pallet, new Vector3(i * 0.55f, 0.06f, 0f), new Vector3(0.1f, 0.11f, 1f), _woodMaterial);
        }

        // Стеллаж с коробками; длина вдоль локальной X, глубина 0.7 по Z
        private static void BuildShelf(Transform parent, Vector3 position, float yaw, float length, int seed)
        {
            const float depth = 0.7f;
            const float height = 2.4f;
            var shelf = Group("Shelf", parent, position, yaw);
            var random = new System.Random(seed);

            foreach (float x in new[] { -length / 2f + 0.04f, length / 2f - 0.04f })
            foreach (float z in new[] { -depth / 2f + 0.04f, depth / 2f - 0.04f })
                Box("Post", shelf, new Vector3(x, height / 2f, z), new Vector3(0.06f, height, 0.06f), _bluePaintMaterial);

            foreach (float y in new[] { 0.12f, 0.92f, 1.72f })
            {
                Box("Board", shelf, new Vector3(0f, y, 0f), new Vector3(length, 0.05f, depth), _darkMetalMaterial);

                for (float x = -length / 2f + 0.1f; ;)
                {
                    float width = 0.3f + 0.35f * (float)random.NextDouble();
                    if (x + width > length / 2f - 0.1f) break;

                    float boxHeight = 0.25f + 0.4f * (float)random.NextDouble();

                    if (random.NextDouble() > 0.2)
                    {
                        Box("Box", shelf, new Vector3(x + width / 2f, y + 0.025f + boxHeight / 2f, ((float)random.NextDouble() - 0.5f) * 0.15f),
                            new Vector3(width * 0.9f, boxHeight, 0.5f), random.NextDouble() > 0.5 ? _woodMaterial : _cardboardMaterial,
                            new Vector3(0f, ((float)random.NextDouble() - 0.5f) * 12f, 0f));
                    }

                    x += width + 0.05f;
                }
            }

            Box("Board_Top", shelf, new Vector3(0f, height - 0.03f, 0f), new Vector3(length, 0.05f, depth), _darkMetalMaterial);
            Solid(shelf, new Vector3(0f, height / 2f, 0f), new Vector3(length, height, depth));
        }

        private static void BuildServerRack(Transform parent, Vector3 position, float yaw, Material accent, int seed)
        {
            var rack = Group("ServerRack", parent, position, yaw);
            var random = new System.Random(seed);
            Box("Body", rack, new Vector3(0f, 1.05f, 0f), new Vector3(0.8f, 2.1f, 0.9f), _darkMetalMaterial);
            Box("Door", rack, new Vector3(0f, 1.05f, 0.455f), new Vector3(0.7f, 1.95f, 0.02f), _screenMaterial);

            for (int i = 0; i < 10; i++)
            {
                float width = 0.15f + 0.4f * (float)random.NextDouble();
                Box("Leds", rack, new Vector3(-0.3f + width / 2f, 0.3f + i * 0.17f, 0.47f), new Vector3(width, 0.025f, 0.01f), i % 3 == 0 ? _ledMaterial : accent);
            }

            Box("Vent", rack, new Vector3(0f, 2.11f, 0f), new Vector3(0.6f, 0.03f, 0.7f), _trimMaterial);
            Solid(rack, new Vector3(0f, 1.05f, 0f), new Vector3(0.8f, 2.1f, 0.9f));
        }

        private static void BuildCoolingUnit(Transform parent, Vector3 position, float yaw)
        {
            var unit = Group("CoolingUnit", parent, position, yaw);
            Box("Body", unit, new Vector3(0f, 1.1f, 0f), new Vector3(1.6f, 2.2f, 1.6f), _metalMaterial);
            Box("Grille", unit, new Vector3(0f, 0.9f, 0.81f), new Vector3(1.3f, 1.4f, 0.02f), _darkMetalMaterial);

            for (int i = 0; i < 6; i++)
                Box("Slot", unit, new Vector3(0f, 0.35f + i * 0.22f, 0.83f), new Vector3(1.2f, 0.04f, 0.02f), _trimMaterial);

            foreach (float x in new[] { -0.4f, 0.4f })
            {
                Cylinder("Fan", unit, new Vector3(x, 2.22f, 0f), 0.65f, 0.04f, _darkMetalMaterial);
                Box("Blade", unit, new Vector3(x, 2.25f, 0f), new Vector3(0.6f, 0.02f, 0.08f), _rubberMaterial, new Vector3(0f, 30f, 0f));
                Box("Blade", unit, new Vector3(x, 2.25f, 0f), new Vector3(0.6f, 0.02f, 0.08f), _rubberMaterial, new Vector3(0f, -60f, 0f));
            }

            Solid(unit, new Vector3(0f, 1.1f, 0f), new Vector3(1.6f, 2.2f, 1.6f));
        }

        // Пульт: оператор со стороны -Z, мониторы светят в сторону -Z (к входу в комнату)
        private static void BuildConsole(Transform parent, Vector3 position, float yaw, Material accent)
        {
            var desk = Group("Console", parent, position, yaw);
            Box("Top", desk, new Vector3(0f, 0.8f, 0f), new Vector3(2f, 0.06f, 0.8f), _darkMetalMaterial);
            Box("Front", desk, new Vector3(0f, 0.4f, 0.36f), new Vector3(2f, 0.78f, 0.06f), _metalMaterial);

            foreach (float x in new[] { -0.97f, 0.97f })
                Box("Side", desk, new Vector3(x, 0.4f, 0f), new Vector3(0.06f, 0.8f, 0.8f), _metalMaterial);

            foreach (float x in new[] { -0.55f, 0.55f })
            {
                Box("Stand", desk, new Vector3(x, 0.93f, 0.2f), new Vector3(0.06f, 0.22f, 0.06f), _darkMetalMaterial);
                Box("Monitor", desk, new Vector3(x, 1.2f, 0.2f), new Vector3(0.8f, 0.48f, 0.05f), _darkMetalMaterial);
                Box("Display", desk, new Vector3(x, 1.2f, 0.17f), new Vector3(0.72f, 0.4f, 0.01f), accent);
            }

            Box("Keyboard", desk, new Vector3(0f, 0.84f, -0.15f), new Vector3(0.6f, 0.02f, 0.2f), _rubberMaterial);
            Solid(desk, new Vector3(0f, 0.6f, 0f), new Vector3(2f, 1.2f, 0.8f));
        }

        private static void BuildLabTable(Transform parent, Vector3 position, float yaw, Material accent)
        {
            var table = Group("LabTable", parent, position, yaw);
            Box("Top", table, new Vector3(0f, 0.85f, 0f), new Vector3(2.2f, 0.08f, 0.9f), _whiteMaterial);
            Box("Cabinet", table, new Vector3(0f, 0.42f, 0f), new Vector3(2.1f, 0.8f, 0.8f), _metalMaterial);

            for (int i = 0; i < 5; i++)
            {
                float x = -0.85f + i * 0.3f;
                float height = 0.18f + 0.08f * (i % 3);
                Cylinder("Flask", table, new Vector3(x, 0.89f + height / 2f, -0.15f + 0.1f * (i % 2)), 0.12f, height, _glassMaterial);
                Cylinder("Fluid", table, new Vector3(x, 0.89f + height * 0.3f, -0.15f + 0.1f * (i % 2)), 0.1f, height * 0.55f, i % 2 == 0 ? accent : _liquidMaterial);
            }

            Box("Monitor", table, new Vector3(0.6f, 1.15f, 0.25f), new Vector3(0.7f, 0.45f, 0.05f), _darkMetalMaterial);
            Box("Display", table, new Vector3(0.6f, 1.15f, 0.22f), new Vector3(0.62f, 0.37f, 0.01f), accent);
            Box("Stand", table, new Vector3(0.6f, 0.95f, 0.25f), new Vector3(0.06f, 0.12f, 0.06f), _darkMetalMaterial);
            Solid(table, new Vector3(0f, 0.6f, 0f), new Vector3(2.2f, 1.2f, 0.9f));
        }

        // Колба для выращивания охранников: внутри модель врага в жидкости
        private static void BuildVat(Transform parent, Vector3 position, string model, Material accent)
        {
            var vat = Group("Vat", parent, position);
            Cylinder("Base", vat, new Vector3(0f, 0.2f, 0f), 1.7f, 0.4f, _darkMetalMaterial);
            Cylinder("BaseGlow", vat, new Vector3(0f, 0.42f, 0f), 1.5f, 0.05f, accent);
            BuildStaticMob("Specimen", vat, model, new Vector3(0f, 0.45f, 0f), new Vector3(0f, 180f, 0f), 0.85f, _specimenMaterial);
            Cylinder("Liquid", vat, new Vector3(0f, 1.5f, 0f), 1.3f, 2.1f, _liquidMaterial);
            Cylinder("Glass", vat, new Vector3(0f, 1.55f, 0f), 1.4f, 2.3f, _glassMaterial);
            Cylinder("Cap", vat, new Vector3(0f, 2.85f, 0f), 1.7f, 0.35f, _metalMaterial);
            Cylinder("CapGlow", vat, new Vector3(0f, 2.65f, 0f), 1.5f, 0.05f, accent);
            Rod("Pipe", vat, new Vector3(0.3f, 3f, 0f), new Vector3(0.3f, 4.5f, 0f), 0.15f, _darkMetalMaterial);
            Rod("Pipe", vat, new Vector3(-0.3f, 3f, 0.1f), new Vector3(-0.3f, 4.5f, 0.1f), 0.1f, _darkMetalMaterial);
            Solid(vat, new Vector3(0f, 1.5f, 0f), new Vector3(1.7f, 3f, 1.7f));
        }

        private static void BuildGenerator(Transform parent, Vector3 position, Material accent)
        {
            var generator = Group("Generator", parent, position);
            Box("Base", generator, new Vector3(0f, 0.15f, 0f), new Vector3(2.6f, 0.3f, 2.6f), _darkMetalMaterial);
            Cylinder("Body", generator, new Vector3(0f, 1.5f, 0f), 2.2f, 2.4f, _metalMaterial);

            foreach (float height in new[] { 0.5f, 1.5f, 2.5f })
                Cylinder("Band", generator, new Vector3(0f, height, 0f), 2.25f, 0.1f, _darkMetalMaterial);

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Box("CoreWindow", generator, direction * 1.1f + Vector3.up * 1.5f, new Vector3(0.14f, 1.6f, 0.06f), accent, new Vector3(0f, angle, 0f));
            }

            Sphere("Dome", generator, new Vector3(0f, 2.7f, 0f), new Vector3(2f, 0.7f, 2f), _darkMetalMaterial);
            Rod("Exhaust", generator, new Vector3(0.5f, 2.8f, 0.3f), new Vector3(0.5f, 4.6f, 0.3f), 0.3f, _darkMetalMaterial);
            Rod("Exhaust", generator, new Vector3(-0.4f, 2.8f, -0.4f), new Vector3(-0.4f, 4.3f, -0.4f), 0.22f, _darkMetalMaterial);
            Solid(generator, new Vector3(0f, 1.5f, 0f), new Vector3(2.6f, 3f, 2.6f));
        }

        private static void BuildTank(Transform parent, Vector3 position, float diameter, float height, Material paint)
        {
            var tank = Group("Tank", parent, position);
            Cylinder("Body", tank, new Vector3(0f, height / 2f, 0f), diameter, height, paint);
            Sphere("Dome", tank, new Vector3(0f, height, 0f), new Vector3(diameter, diameter * 0.4f, diameter), paint);
            Cylinder("Band", tank, new Vector3(0f, height * 0.25f, 0f), diameter + 0.04f, 0.08f, _darkMetalMaterial);
            Cylinder("Band", tank, new Vector3(0f, height * 0.75f, 0f), diameter + 0.04f, 0.08f, _darkMetalMaterial);
            Box("Gauge", tank, new Vector3(0f, height * 0.5f, diameter / 2f + 0.02f), new Vector3(0.25f, 0.25f, 0.05f), _whiteMaterial);
            Solid(tank, new Vector3(0f, height / 2f, 0f), new Vector3(diameter, height, diameter));
        }

        // Бетонное укрытие с жёлтой полосой
        private static void BuildBarrier(Transform parent, Vector3 position, float yaw)
        {
            var barrier = Group("Barrier", parent, position, yaw);
            Box("Base", barrier, new Vector3(0f, 0.175f, 0f), new Vector3(2.4f, 0.35f, 0.7f), _concreteMaterial);
            Box("Top", barrier, new Vector3(0f, 0.725f, 0f), new Vector3(2.4f, 0.75f, 0.35f), _concreteMaterial);

            for (int i = 0; i < 6; i++)
                Box("Stripe", barrier, new Vector3(-1f + i * 0.4f, 1.12f, 0f), new Vector3(0.4f, 0.06f, 0.36f), i % 2 == 0 ? _doorMaterial : _hazardMaterial);

            Solid(barrier, new Vector3(0f, 0.55f, 0f), new Vector3(2.4f, 1.1f, 0.7f));
        }

        // Конвейер вдоль локальной Z
        private static Transform BuildConveyor(Transform parent, Vector3 position, float yaw, float length)
        {
            var conveyor = Group("Conveyor", parent, position, yaw);
            Box("Belt", conveyor, new Vector3(0f, 0.82f, 0f), new Vector3(1f, 0.06f, length), _rubberMaterial);
            Box("Body", conveyor, new Vector3(0f, 0.65f, 0f), new Vector3(0.9f, 0.3f, length - 0.2f), _darkMetalMaterial);

            foreach (float x in new[] { -0.56f, 0.56f })
                Box("Rail", conveyor, new Vector3(x, 0.86f, 0f), new Vector3(0.1f, 0.16f, length), _doorMaterial);

            foreach (float z in new[] { -length / 2f, length / 2f })
                Cylinder("Roller", conveyor, new Vector3(0f, 0.75f, z), 0.22f, 1.1f, _metalMaterial, new Vector3(0f, 0f, 90f));

            for (float z = -length / 2f + 0.5f; z < length / 2f; z += 2f)
            foreach (float x in new[] { -0.45f, 0.45f })
                Box("Leg", conveyor, new Vector3(x, 0.25f, z), new Vector3(0.08f, 0.5f, 0.08f), _metalMaterial);

            Solid(conveyor, new Vector3(0f, 0.47f, 0f), new Vector3(1.2f, 0.95f, length));
            return conveyor;
        }

        // Рука-манипулятор тянется вперёд по +Z
        private static void BuildRobotArm(Transform parent, Vector3 position, float yaw, Material accent)
        {
            var arm = Group("RobotArm", parent, position, yaw);
            Cylinder("Base", arm, new Vector3(0f, 0.15f, 0f), 0.8f, 0.3f, _darkMetalMaterial);
            Cylinder("Turret", arm, new Vector3(0f, 0.5f, 0f), 0.5f, 0.4f, _doorMaterial);

            var shoulder = Group("Shoulder", arm, new Vector3(0f, 0.75f, 0f));
            shoulder.localRotation = Quaternion.Euler(25f, 0f, 0f);
            Sphere("Joint", shoulder, Vector3.zero, Vector3.one * 0.35f, _darkMetalMaterial);
            Box("UpperArm", shoulder, new Vector3(0f, 0.7f, 0f), new Vector3(0.24f, 1.4f, 0.24f), _doorMaterial);

            var elbow = Group("Elbow", shoulder, new Vector3(0f, 1.4f, 0f));
            elbow.localRotation = Quaternion.Euler(75f, 0f, 0f);
            Sphere("Joint", elbow, Vector3.zero, Vector3.one * 0.3f, _darkMetalMaterial);
            Box("Forearm", elbow, new Vector3(0f, 0.5f, 0f), new Vector3(0.18f, 1f, 0.18f), _doorMaterial);
            Box("Light", elbow, new Vector3(0f, 0.5f, 0.1f), new Vector3(0.06f, 0.6f, 0.01f), accent);

            var wrist = Group("Wrist", elbow, new Vector3(0f, 1f, 0f));
            wrist.localRotation = Quaternion.Euler(40f, 0f, 0f);
            Cylinder("Head", wrist, new Vector3(0f, 0.08f, 0f), 0.22f, 0.16f, _darkMetalMaterial);
            Box("Claw", wrist, new Vector3(-0.08f, 0.28f, 0f), new Vector3(0.04f, 0.28f, 0.1f), _metalMaterial, new Vector3(0f, 0f, -12f));
            Box("Claw", wrist, new Vector3(0.08f, 0.28f, 0f), new Vector3(0.04f, 0.28f, 0.1f), _metalMaterial, new Vector3(0f, 0f, 12f));

            Solid(arm, new Vector3(0f, 0.7f, 0f), new Vector3(0.8f, 1.4f, 0.8f));
        }

        // Недособранный охранник на стойке-креплении, лицом по +Z
        private static void BuildMobStand(Transform parent, Vector3 position, float yaw, string model, Material material)
        {
            var stand = Group("MobStand", parent, position, yaw);
            Box("Base", stand, new Vector3(0f, 0.08f, 0f), new Vector3(1f, 0.16f, 0.8f), _darkMetalMaterial);
            Box("Post", stand, new Vector3(0f, 1.3f, -0.36f), new Vector3(0.14f, 2.6f, 0.14f), _darkMetalMaterial);
            Box("Clamp", stand, new Vector3(0f, 2.55f, -0.15f), new Vector3(0.14f, 0.12f, 0.45f), _darkMetalMaterial);
            Box("Glow", stand, new Vector3(0f, 0.165f, 0.2f), new Vector3(0.8f, 0.01f, 0.2f), _signalMaterial);
            BuildStaticMob("Mob", stand, model, new Vector3(0f, 0.16f, 0f), Vector3.zero, 1f, material);
            Solid(stand, new Vector3(0f, 1.3f, 0f), new Vector3(1f, 2.6f, 0.8f));
        }

        // Ящик с запчастями охранников
        private static void BuildPartsBin(Transform parent, Vector3 position, float yaw, int seed)
        {
            var bin = Group("PartsBin", parent, position, yaw);
            var random = new System.Random(seed);
            Box("Bottom", bin, new Vector3(0f, 0.05f, 0f), new Vector3(1.6f, 0.1f, 1.1f), _darkMetalMaterial);

            foreach (float x in new[] { -0.77f, 0.77f })
                Box("Wall", bin, new Vector3(x, 0.4f, 0f), new Vector3(0.06f, 0.8f, 1.1f), _bluePaintMaterial);

            foreach (float z in new[] { -0.52f, 0.52f })
                Box("Wall", bin, new Vector3(0f, 0.4f, z), new Vector3(1.6f, 0.8f, 0.06f), _bluePaintMaterial);

            for (int i = 0; i < 9; i++)
            {
                var spot = new Vector3(((float)random.NextDouble() - 0.5f) * 1.2f, 0.55f + 0.2f * (float)random.NextDouble(), ((float)random.NextDouble() - 0.5f) * 0.7f);

                if (i % 3 == 0)
                    Sphere("Head", bin, spot, Vector3.one * 0.32f, _mobMeleeMaterial);
                else
                    Cylinder("Limb", bin, spot, 0.12f, 0.7f, _mobMeleeMaterial, new Vector3(90f, (float)random.NextDouble() * 180f, 0f));
            }

            Solid(bin, new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 1.1f));
        }

        // Камера наблюдения, объектив по +Z: корпус с козырьком, объектив, светящаяся линза и индикатор
        private static void BuildCameraModel(Transform parent, Material body, Material lens)
        {
            var facing = new Vector3(90f, 0f, 0f);
            Box("Body", parent, new Vector3(0f, 0f, -0.02f), new Vector3(0.34f, 0.32f, 0.5f), body);
            Box("Hood", parent, new Vector3(0f, 0.19f, 0.05f), new Vector3(0.42f, 0.05f, 0.6f), _darkMetalMaterial);
            Box("Back", parent, new Vector3(0f, 0f, -0.28f), new Vector3(0.3f, 0.28f, 0.06f), _darkMetalMaterial);
            Cylinder("Barrel", parent, new Vector3(0f, -0.01f, 0.27f), 0.24f, 0.12f, _darkMetalMaterial, facing);
            Cylinder("Lens", parent, new Vector3(0f, -0.01f, 0.33f), 0.17f, 0.02f, lens, facing);
            Sphere("Led", parent, new Vector3(0.12f, 0.1f, 0.23f), Vector3.one * 0.05f, lens);
        }

        private static void BuildPillar(Transform parent, Vector3 position, Material accent)
        {
            var pillar = Group("Pillar", parent, position);
            Box("Body", pillar, new Vector3(0f, CeilingHeight / 2f, 0f), new Vector3(1.2f, CeilingHeight, 1.2f), _wallMaterial);
            Box("Base", pillar, new Vector3(0f, 0.2f, 0f), new Vector3(1.5f, 0.4f, 1.5f), _trimMaterial);
            Box("Cap", pillar, new Vector3(0f, CeilingHeight - 0.15f, 0f), new Vector3(1.5f, 0.3f, 1.5f), _trimMaterial);

            for (int i = 0; i < 4; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward;
                Box("Strip", pillar, direction * 0.61f + Vector3.up * 2f, new Vector3(0.12f, 2.6f, 0.02f), accent, new Vector3(0f, i * 90f, 0f));
            }

            Solid(pillar, new Vector3(0f, CeilingHeight / 2f, 0f), new Vector3(1.5f, CeilingHeight, 1.5f));
        }

        // Огромный «глаз» системы наблюдения на стене
        private static void BuildEye(Transform wallAnchor, Material accent)
        {
            var eye = Group("Eye", wallAnchor, Vector3.zero);
            var facing = new Vector3(90f, 0f, 0f);
            Cylinder("Housing", eye, new Vector3(0f, 0f, 0.15f), 2.6f, 0.3f, _darkMetalMaterial, facing);
            Cylinder("Ring", eye, new Vector3(0f, 0f, 0.2f), 2.2f, 0.32f, accent, facing);
            Cylinder("Iris", eye, new Vector3(0f, 0f, 0.22f), 1.8f, 0.34f, _trimMaterial, facing);
            Cylinder("Lens", eye, new Vector3(0f, 0f, 0.24f), 1.1f, 0.36f, _screenMaterial, facing);
            Cylinder("Pupil", eye, new Vector3(0f, 0f, 0.26f), 0.45f, 0.38f, accent, facing);
            CreateLight(eye, new Vector3(0f, 0f, 1.2f), Color.red, 3f, 8f);
        }

        private static void BuildLocker(Transform parent, Vector3 position, float yaw)
        {
            var locker = Group("Locker", parent, position, yaw);
            Box("Body", locker, new Vector3(0f, 0.95f, 0f), new Vector3(0.6f, 1.9f, 0.5f), _bluePaintMaterial);
            Box("Seam", locker, new Vector3(0f, 0.95f, 0.251f), new Vector3(0.58f, 0.01f, 0.01f), _trimMaterial);
            Box("Handle", locker, new Vector3(0.2f, 1f, 0.26f), new Vector3(0.03f, 0.15f, 0.03f), _metalMaterial);

            for (int i = 0; i < 3; i++)
                Box("Vent", locker, new Vector3(0f, 1.55f + i * 0.06f, 0.255f), new Vector3(0.35f, 0.02f, 0.01f), _trimMaterial);

            Solid(locker, new Vector3(0f, 0.95f, 0f), new Vector3(0.6f, 1.9f, 0.5f));
        }

        private static void BuildCot(Transform parent, Vector3 position, float yaw)
        {
            var cot = Group("Cot", parent, position, yaw);
            Box("Frame", cot, new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.08f, 2f), _darkMetalMaterial);

            foreach (float x in new[] { -0.4f, 0.4f })
            foreach (float z in new[] { -0.9f, 0.9f })
                Box("Leg", cot, new Vector3(x, 0.12f, z), new Vector3(0.05f, 0.25f, 0.05f), _darkMetalMaterial);

            Box("Mattress", cot, new Vector3(0f, 0.35f, 0f), new Vector3(0.85f, 0.12f, 1.95f), _fabricMaterial);
            Box("Pillow", cot, new Vector3(0f, 0.45f, -0.75f), new Vector3(0.6f, 0.1f, 0.35f), _whiteMaterial);
            Box("Blanket", cot, new Vector3(0.02f, 0.42f, 0.35f), new Vector3(0.88f, 0.04f, 1.1f), _bluePaintMaterial, new Vector3(0f, 4f, 0f));
            Solid(cot, new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, 2f));
        }

        private static void BuildTable(Transform parent, Vector3 position, float yaw)
        {
            var table = Group("Table", parent, position, yaw);
            Box("Top", table, new Vector3(0f, 0.75f, 0f), new Vector3(1.4f, 0.05f, 0.8f), _woodMaterial);

            foreach (float x in new[] { -0.62f, 0.62f })
            foreach (float z in new[] { -0.32f, 0.32f })
                Box("Leg", table, new Vector3(x, 0.37f, z), new Vector3(0.05f, 0.74f, 0.05f), _darkMetalMaterial);

            Box("Paper", table, new Vector3(-0.3f, 0.78f, 0.1f), new Vector3(0.3f, 0.005f, 0.4f), _whiteMaterial, new Vector3(0f, 15f, 0f));
            Cylinder("Mug", table, new Vector3(0.35f, 0.83f, -0.1f), 0.09f, 0.11f, _redPaintMaterial);
            Solid(table, new Vector3(0f, 0.4f, 0f), new Vector3(1.4f, 0.8f, 0.8f));
        }

        private static void BuildChair(Transform parent, Vector3 position, Vector3 euler)
        {
            var chair = Group("Chair", parent, position);
            chair.localRotation = Quaternion.Euler(euler);
            Box("Seat", chair, new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.05f, 0.45f), _darkMetalMaterial);
            Box("Back", chair, new Vector3(0f, 0.75f, -0.2f), new Vector3(0.45f, 0.55f, 0.04f), _darkMetalMaterial);

            foreach (float x in new[] { -0.19f, 0.19f })
            foreach (float z in new[] { -0.19f, 0.19f })
                Box("Leg", chair, new Vector3(x, 0.22f, z), new Vector3(0.04f, 0.45f, 0.04f), _metalMaterial);
        }

        private static void BuildBench(Transform parent, Vector3 position, float yaw, float length)
        {
            var bench = Group("Bench", parent, position, yaw);
            Box("Seat", bench, new Vector3(0f, 0.45f, 0f), new Vector3(length, 0.06f, 0.45f), _woodMaterial);

            foreach (float x in new[] { -length / 2f + 0.15f, length / 2f - 0.15f })
                Box("Leg", bench, new Vector3(x, 0.22f, 0f), new Vector3(0.06f, 0.44f, 0.4f), _darkMetalMaterial);

            Solid(bench, new Vector3(0f, 0.24f, 0f), new Vector3(length, 0.48f, 0.45f));
        }

        // Экран с «аналитикой» боя: столбцы графика в цвете комнаты
        private static void BuildWallScreen(Transform room, Side side, float along, float y, float width, float height, Material accent, int seed, bool broken = false)
        {
            var screen = OnWall("WallScreen", room, side, along, y);
            Box("Frame", screen, new Vector3(0f, 0f, 0.04f), new Vector3(width + 0.12f, height + 0.12f, 0.08f), _darkMetalMaterial);
            Box("Display", screen, new Vector3(0f, 0f, 0.085f), new Vector3(width, height, 0.01f), broken ? _hazardMaterial : _screenMaterial);

            if (broken)
            {
                Box("Crack", screen, new Vector3(-0.1f, 0.05f, 0.095f), new Vector3(width * 0.7f, 0.015f, 0.005f), _whiteMaterial, new Vector3(0f, 0f, 25f));
                Box("Crack", screen, new Vector3(0.15f, -0.1f, 0.095f), new Vector3(width * 0.45f, 0.015f, 0.005f), _whiteMaterial, new Vector3(0f, 0f, -40f));
                return;
            }

            var random = new System.Random(seed);
            const int bars = 6;
            float barWidth = width / (bars * 1.6f);

            for (int i = 0; i < bars; i++)
            {
                float barHeight = height * (0.15f + 0.55f * (float)random.NextDouble());
                float x = -width / 2f + barWidth * (0.8f + i * 1.6f);
                Box($"Bar_{i}", screen, new Vector3(x, -height / 2f + 0.08f + barHeight / 2f, 0.095f), new Vector3(barWidth, barHeight, 0.01f), accent);
            }

            Box("Header", screen, new Vector3(-width * 0.2f, height / 2f - 0.1f, 0.095f), new Vector3(width * 0.5f, 0.04f, 0.01f), accent);
        }

        // Табличка: смотрит по +forward, текст читается спереди
        private static void BuildSign(Transform parent, Vector3 localPosition, float yaw, string content, float width, Material accent, Color textColor)
        {
            var sign = Group("Sign", parent, localPosition, yaw);
            Box("Plate", sign, Vector3.zero, new Vector3(width, 0.55f, 0.06f), _darkMetalMaterial);
            Box("Underline", sign, new Vector3(0f, -0.3f, 0f), new Vector3(width, 0.05f, 0.07f), accent);
            CreateText("Text", sign, new Vector3(0f, 0f, 0.035f), content, textColor, new Vector2(width - 0.2f, 0.45f));
        }

        private static void BuildPuddle(Transform parent, Vector3 position, float width, float length, float yaw)
        {
            var puddle = Cylinder("Puddle", parent, new Vector3(position.x, 0.013f, position.z), 1f, 0.004f, _puddleMaterial, new Vector3(0f, yaw, 0f));
            puddle.transform.localScale = new Vector3(width, 0.002f, length);
        }

        private static void BuildDebris(Transform parent, Vector3 center, int count, int seed)
        {
            var random = new System.Random(seed);

            for (int i = 0; i < count; i++)
            {
                float size = 0.08f + 0.25f * (float)random.NextDouble();
                var offset = new Vector3(((float)random.NextDouble() - 0.5f) * 1.6f, size * 0.35f, ((float)random.NextDouble() - 0.5f) * 1.6f);
                var euler = new Vector3((float)random.NextDouble() * 40f, (float)random.NextDouble() * 360f, (float)random.NextDouble() * 40f);
                Box("Debris", parent, center + offset, new Vector3(size, size * 0.6f, size * 1.3f), i % 3 == 0 ? _darkMetalMaterial : _concreteMaterial, euler);
            }
        }

        // Кабель по полу через точки
        private static void BuildCable(Transform parent, Material material, params Vector3[] points)
        {
            for (int i = 0; i < points.Length - 1; i++)
                Rod("Cable", parent, points[i] + Vector3.up * 0.03f, points[i + 1] + Vector3.up * 0.03f, 0.06f, material);
        }

        private static void BuildPipe(Transform parent, Vector3 from, Vector3 to, float diameter)
        {
            Rod("Pipe", parent, from, to, diameter, _metalMaterial);
            int flanges = Mathf.FloorToInt(Vector3.Distance(from, to) / 2.5f);

            for (int i = 1; i <= flanges; i++)
            {
                Vector3 point = Vector3.Lerp(from, to, i / (flanges + 1f));
                Rod("Flange", parent, point, point + (to - from).normalized * 0.08f, diameter * 1.4f, _darkMetalMaterial);
            }
        }

        // Жёлто-чёрная разметка на полу вдоль локальной X
        private static void BuildHazardStripe(Transform parent, Vector3 center, float length, float yaw)
        {
            var stripe = Group("HazardStripe", parent, new Vector3(center.x, 0f, center.z), yaw);
            int segments = Mathf.Max(1, Mathf.RoundToInt(length / 0.4f));
            float step = length / segments;

            for (int i = 0; i < segments; i++)
                Box("Segment", stripe, new Vector3(-length / 2f + step * (i + 0.5f), 0.012f, 0f), new Vector3(step, 0.01f, 0.25f), i % 2 == 0 ? _doorMaterial : _hazardMaterial);
        }
    }
}
