using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace SibJam.Editor
{
    // Оформление: общая отделка комнат, тематический декор, коридоры, двери, атмосфера.
    // Свободными остаются: проходы у дверей, точки спавна, места перед камерами наблюдения
    public static partial class GameplayFlowBuilder
    {
        private static readonly Color LampColor = new(1f, 0.92f, 0.8f);

        // Низ потолка — на уровне верха балок, выше стен (их верх закрывает фриз)
        private const float CeilingHeight = 4.5f;

        // Плитка пола, кольцо арены, плинтусы, пилястры, световые полосы цвета комнаты, балки с лампами
        private static void DecorateShell(Transform room, Material accent, bool entryDoor, bool exitDoor)
        {
            var shell = Group("Shell", room, Vector3.zero);

            for (int x = 0; x < 6; x++)
            for (int z = 0; z < 6; z++)
            {
                if ((x + z) % 2 == 0)
                    Box("FloorPanel", shell, new Vector3(-7.5f + x * 3f, 0.005f, -7.5f + z * 3f), new Vector3(2.9f, 0.01f, 2.9f), _floorPanelMaterial);
            }

            const int ringSegments = 28;

            for (int i = 0; i < ringSegments; i++)
            {
                float angle = i * 360f / ringSegments;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Box("Ring", shell, direction * 4.5f + Vector3.up * 0.012f, new Vector3(0.7f, 0.01f, 0.1f), accent, new Vector3(0f, angle, 0f));
            }

            foreach (Side side in new[] { Side.Left, Side.Right, Side.Entry, Side.Exit })
            {
                bool hasGap = side == Side.Entry ? entryDoor : side == Side.Exit && exitDoor;
                var wall = OnWall($"Wall_{side}", shell, side, 0f);
                var spans = hasGap ? new[] { (-Half, -GapWidth / 2f), (GapWidth / 2f, Half) } : new[] { (-Half, Half) };

                foreach (var (from, to) in spans)
                {
                    float center = (from + to) / 2f;
                    float length = to - from;
                    Box("Baseboard", wall, new Vector3(center, 0.15f, 0.06f), new Vector3(length, 0.3f, 0.12f), _trimMaterial);
                    Box("Cap", wall, new Vector3(center, WallHeight + 0.05f, -WallThickness / 2f), new Vector3(length, 0.1f, WallThickness + 0.3f), _trimMaterial);
                    Box("Frieze", wall, new Vector3(center, (WallHeight + CeilingHeight) / 2f, -WallThickness / 2f), new Vector3(length, CeilingHeight - WallHeight, WallThickness), _wallMaterial);
                    Box("LightStrip", wall, new Vector3(center, 3.3f, 0.03f), new Vector3(length, 0.06f, 0.06f), accent);
                }

                foreach (float along in new[] { -6f, 6f })
                {
                    Box("Pilaster", wall, new Vector3(along, CeilingHeight / 2f, 0.2f), new Vector3(0.6f, CeilingHeight, 0.4f), _trimMaterial);
                    Box("PilasterStrip", wall, new Vector3(along, 2.6f, 0.41f), new Vector3(0.08f, 1.6f, 0.02f), accent);
                }
            }

            foreach (float z in new[] { -4.5f, 4.5f })
            {
                var girder = Group("Girder", shell, new Vector3(0f, 0f, z));
                Box("Top", girder, new Vector3(0f, 4.46f, 0f), new Vector3(RoomSize + 0.6f, 0.08f, 0.45f), _darkMetalMaterial);
                Box("Web", girder, new Vector3(0f, 4.2f, 0f), new Vector3(RoomSize + 0.6f, 0.45f, 0.08f), _darkMetalMaterial);
                Box("Bottom", girder, new Vector3(0f, 3.96f, 0f), new Vector3(RoomSize + 0.6f, 0.08f, 0.45f), _darkMetalMaterial);
                BuildHangingLamp(girder, new Vector3(0f, 3.92f, 0f), LampColor, 5f, 16f);
            }

            BuildRoomCeiling(shell);
        }

        // Потолок над комнатой: плита на высоте верха балок, рёбра и светящиеся панели. Над проёмами дверей — короба, куда уезжает дверь
        private static void BuildRoomCeiling(Transform shell)
        {
            var ceiling = Group("Ceiling", shell, Vector3.zero);
            float inner = RoomSize - WallThickness;
            Box("Slab", ceiling, new Vector3(0f, CeilingHeight + 0.15f, 0f), new Vector3(RoomSize, 0.3f, inner), _trimMaterial);

            for (float z = -7.5f; z <= 7.5f; z += 3f)
                Box("Rib", ceiling, new Vector3(0f, CeilingHeight - 0.05f, z), new Vector3(inner, 0.1f, 0.15f), _darkMetalMaterial);

            foreach (float x in new[] { -6f, 6f })
                Box("Rib", ceiling, new Vector3(x, CeilingHeight - 0.05f, 0f), new Vector3(0.15f, 0.1f, inner), _darkMetalMaterial);

            foreach (float x in new[] { -4.5f, 4.5f })
            foreach (float z in new[] { -6f, 0f, 6f })
                Box("Panel", ceiling, new Vector3(x, CeilingHeight - 0.02f, z), new Vector3(1.8f, 0.04f, 0.5f), _lampMaterial);

            foreach (float z in new[] { -Half, Half })
                Box("DoorHousing", ceiling, new Vector3(0f, WallHeight + 2.5f, z), new Vector3(GapWidth, 5f, WallThickness), _wallMaterial);
        }

        private static void BuildHangingLamp(Transform parent, Vector3 top, Color color, float intensity, float range)
        {
            var lamp = Group("Lamp", parent, top);
            Cylinder("Cable", lamp, new Vector3(0f, -0.25f, 0f), 0.03f, 0.5f, _darkMetalMaterial);
            Box("Housing", lamp, new Vector3(0f, -0.56f, 0f), new Vector3(1.4f, 0.12f, 0.4f), _darkMetalMaterial);
            Box("Glow", lamp, new Vector3(0f, -0.63f, 0f), new Vector3(1.3f, 0.03f, 0.3f), _lampMaterial);
            CreateLight(lamp, new Vector3(0f, -0.9f, 0f), color, intensity, range);
        }

        // Детали двери — дочерние, чтобы ехали вместе с ней. Дверь — масштабированный куб, поэтому детали ставим в мировых координатах
        private static void DecorateDoor(GameObject door)
        {
            Transform room = door.transform.parent;
            Vector3 center = door.transform.localPosition;
            float depth = WallThickness * 0.8f + 0.04f;

            var parts = new List<GameObject>
            {
                Box("Band", room, center + new Vector3(0f, -1.4f, 0f), new Vector3(GapWidth, 0.5f, depth), _hazardMaterial),
                Box("Band", room, center + new Vector3(0f, 1.65f, 0f), new Vector3(GapWidth, 0.2f, depth), _hazardMaterial),
                Box("Seam", room, center, new Vector3(0.06f, WallHeight, depth), _hazardMaterial),
                Box("Window", room, center + new Vector3(0f, 0.6f, 0f), new Vector3(1.6f, 0.1f, depth + 0.01f), _lampMaterial),
            };

            for (int i = 0; i < 5; i++)
                parts.Add(Box("Stripe", room, center + new Vector3(-1.6f + i * 0.8f, -1.4f, 0f), new Vector3(0.33f, 0.33f, depth + 0.01f), _doorMaterial, new Vector3(0f, 0f, 45f)));

            foreach (GameObject part in parts)
                part.transform.SetParent(door.transform, true);
        }

        // Жёлто-чёрные косяки по краям проёма и сигнальные лампы сверху
        private static void BuildDoorFrame(Transform room, float z)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                // Внутренняя грань чуть заходит в проём: иначе совпадает с гранью стены коридора и мерцает
                var post = Group("DoorFrame", room, new Vector3(side * (GapWidth / 2f + 0.13f), 0f, z));

                for (int i = 0; i < 6; i++)
                    Box("Segment", post, new Vector3(0f, 0.35f + i * 0.7f, 0f), new Vector3(0.34f, 0.7f, WallThickness + 0.3f), i % 2 == 0 ? _doorMaterial : _hazardMaterial);

                foreach (float face in new[] { -1f, 1f })
                    Sphere("Signal", post, new Vector3(0f, 3.85f, face * (WallThickness / 2f + 0.2f)), Vector3.one * 0.22f, _signalMaterial);
            }
        }

        // ---------- Темы комнат ----------

        private static void DecorateWarehouse(Transform decor, Material accent)
        {
            BuildPallet(decor, new Vector3(-7.6f, 0f, -7.6f), 5f);
            BuildCrate(decor, new Vector3(-7.6f, 0.15f, -7.6f), 5f);
            BuildCrate(decor, new Vector3(-7.55f, 1.15f, -7.65f), 20f, 0.9f);
            BuildCrate(decor, new Vector3(-6.3f, 0f, -7.9f), -10f, 0.8f);

            BuildShelf(OnWall("Anchor", decor, Side.Entry, 3.8f), new Vector3(0f, 0f, 0.4f), 0f, 3f, 11);
            BuildShelf(OnWall("Anchor", decor, Side.Right, -4f), new Vector3(0f, 0f, 0.4f), 0f, 3f, 12);
            BuildPallet(decor, new Vector3(7.7f, 0f, -7.6f), 0f);
            BuildCrate(decor, new Vector3(7.7f, 0.15f, -7.6f), -8f);

            BuildPallet(decor, new Vector3(-7.7f, 0f, -4.8f), 90f);
            BuildCrate(decor, new Vector3(-7.7f, 0.15f, -4.8f), 85f, 0.95f);
            BuildPallet(decor, new Vector3(-7.7f, 0f, 3.6f), 90f);
            BuildCrate(decor, new Vector3(-7.75f, 0.15f, 3.3f), 95f, 0.8f);
            BuildCrate(decor, new Vector3(-7.6f, 0.15f, 4.2f), 80f, 0.7f);
            BuildCrate(decor, new Vector3(-7.7f, 0f, 7.7f), 12f, 1.1f);
            BuildCrate(decor, new Vector3(-7.6f, 1.1f, 7.6f), -20f, 0.7f);

            BuildBarrelCluster(decor, new Vector3(7.2f, 0f, 7.3f), _redPaintMaterial, _bluePaintMaterial, _redPaintMaterial, _doorMaterial);
            BuildBarrel(decor, new Vector3(5.9f, 0.3f, 8.1f), _bluePaintMaterial, new Vector3(90f, 270f, 0f));

            // Укрытия
            BuildCrate(decor, new Vector3(-3.6f, 0f, -3f), 10f, 1.2f);
            BuildCrate(decor, new Vector3(-2.6f, 0f, -3.3f), -5f, 0.8f);
            BuildCrate(decor, new Vector3(3.5f, 0f, -2.8f), -12f, 1.2f);
            BuildBarrel(decor, new Vector3(4.6f, 0f, -3.2f), _bluePaintMaterial);

            BuildHazardStripe(decor, new Vector3(3.8f, 0f, -7.7f), 3.2f, 0f);
            BuildPuddle(decor, new Vector3(1.8f, 0f, -0.5f), 1.4f, 0.9f, 30f);
            BuildDebris(decor, new Vector3(-3f, 0f, -1.8f), 6, 21);
            BuildWallScreen(decor, Side.Exit, 4.5f, 2.6f, 2f, 1.1f, accent, 31);
        }

        private static void DecorateServers(Transform decor, Material accent)
        {
            int seed = 100;

            foreach (float z in new[] { 1.2f, 2.05f, 2.9f, 3.75f, 7f, 7.85f })
                BuildServerRack(decor, new Vector3(-8.3f, 0f, z), 90f, accent, seed++);

            foreach (float z in new[] { -5f, -4.15f, -3.3f, -2.45f, -1.6f })
                BuildServerRack(decor, new Vector3(8.3f, 0f, z), -90f, accent, seed++);

            foreach (float x in new[] { -5f, -4.15f, -3.3f })
                BuildServerRack(decor, new Vector3(x, 0f, -8.3f), 0f, accent, seed++);

            foreach (float x in new[] { 3.3f, 4.15f, 5f, 7.4f })
                BuildServerRack(decor, new Vector3(x, 0f, 8.3f), 180f, accent, seed++);

            BuildCoolingUnit(decor, new Vector3(-7.5f, 0f, -7.5f), 45f);
            BuildCoolingUnit(decor, new Vector3(7.5f, 0f, -7.5f), -45f);

            BuildConsole(decor, new Vector3(-3.5f, 0f, -3f), 0f, accent);
            BuildConsole(decor, new Vector3(3.5f, 0f, -3f), 0f, accent);
            BuildChair(decor, new Vector3(-3.3f, 0f, -3.8f), new Vector3(0f, 170f, 0f));
            BuildChair(decor, new Vector3(3.8f, 0f, -3.9f), new Vector3(0f, 200f, 0f));

            BuildCable(decor, _rubberMaterial, new Vector3(-3.2f, 0f, -2.6f), new Vector3(-3.2f, 0f, 0.6f), new Vector3(-7.8f, 0f, 1.2f));
            BuildCable(decor, _rubberMaterial, new Vector3(3.3f, 0f, -2.6f), new Vector3(3.3f, 0f, -2f), new Vector3(7.8f, 0f, -2f));
            BuildCable(decor, accent, new Vector3(-3.8f, 0f, -2.6f), new Vector3(-3.8f, 0f, -7.8f));

            foreach (float x in new[] { -7.4f, 7.4f })
                Box("CableTray", decor, new Vector3(x, 3.75f, 0f), new Vector3(0.5f, 0.1f, RoomSize - 1f), _darkMetalMaterial);

            BuildWallScreen(decor, Side.Left, -4.8f, 2.6f, 1.6f, 1f, accent, 41);
            BuildWallScreen(decor, Side.Entry, 4.2f, 2.6f, 2.4f, 1.2f, accent, 42);
            BuildWallScreen(decor, Side.Exit, -7.6f, 2.8f, 1.2f, 0.8f, accent, 43);
        }

        private static void DecorateLab(Transform decor, Material accent)
        {
            var vats = new[]
            {
                new Vector3(-7.4f, 0f, -7.4f), new Vector3(7.4f, 0f, -7.4f), new Vector3(-7.4f, 0f, 7.4f),
                new Vector3(7.4f, 0f, 7.4f), new Vector3(7.5f, 0f, -4.3f), new Vector3(-7.5f, 0f, 3.4f),
            };

            for (int i = 0; i < vats.Length; i++)
                BuildVat(decor, vats[i], i % 3 == 2 ? GunnerModel : MeleeModel, accent);

            BuildLabTable(decor, new Vector3(-3.5f, 0f, -3f), 0f, accent);
            BuildLabTable(decor, new Vector3(3.5f, 0f, -3f), 0f, accent);
            BuildLocker(decor, new Vector3(-8.45f, 0f, -5.3f), 90f);
            BuildLocker(decor, new Vector3(-8.45f, 0f, -4.65f), 90f);

            BuildCable(decor, accent, new Vector3(-6.6f, 0f, -7.4f), new Vector3(-3.5f, 0f, -3.5f));
            BuildCable(decor, accent, new Vector3(6.6f, 0f, -7.4f), new Vector3(3.5f, 0f, -3.5f));
            BuildPuddle(decor, new Vector3(6.4f, 0f, -5.5f), 0.9f, 1.4f, -20f);

            BuildWallScreen(decor, Side.Entry, -4.5f, 2.6f, 2f, 1.1f, accent, 51);
            BuildWallScreen(decor, Side.Entry, 4.5f, 2.6f, 2f, 1.1f, accent, 52);
            BuildWallScreen(decor, Side.Left, -4.9f, 2.7f, 1.4f, 0.9f, accent, 53);
            BuildWallScreen(decor, Side.Exit, 4.5f, 2.6f, 2f, 1.1f, accent, 54);
            CreateLight(decor, new Vector3(0f, 3f, 0f), new Color(0.4f, 1f, 0.55f), 1.2f, 12f);
        }

        private static void DecorateGenerator(Transform decor, Material accent)
        {
            BuildGenerator(decor, new Vector3(7f, 0f, 7.1f), accent);
            BuildHazardStripe(decor, new Vector3(7.05f, 0f, 5.45f), 3.4f, 0f);
            BuildHazardStripe(decor, new Vector3(5.35f, 0f, 7.1f), 3.3f, 90f);
            CreateLight(decor, new Vector3(5.6f, 2.2f, 5.6f), new Color(1f, 0.55f, 0.2f), 2f, 7f);

            float wallX = Half - WallThickness / 2f - 0.25f;
            BuildPipe(decor, new Vector3(-wallX, 3.3f, -Half), new Vector3(-wallX, 3.3f, Half), 0.3f);
            BuildPipe(decor, new Vector3(-wallX, 2.85f, -Half), new Vector3(-wallX, 2.85f, Half), 0.18f);
            BuildPipe(decor, new Vector3(wallX, 0.35f, -Half), new Vector3(wallX, 0.35f, 6.5f), 0.3f);

            BuildTank(decor, new Vector3(-7.5f, 0f, -7.4f), 1.5f, 2.8f, _metalMaterial);
            BuildTank(decor, new Vector3(-5.9f, 0f, -8f), 1f, 1.8f, _bluePaintMaterial);
            BuildTank(decor, new Vector3(7.5f, 0f, -7.5f), 1.3f, 2.4f, _metalMaterial);

            BuildBarrier(decor, new Vector3(-3.5f, 0f, -3f), 0f);
            BuildBarrier(decor, new Vector3(3.5f, 0f, -3f), 0f);

            BuildBarrelCluster(decor, new Vector3(-7.6f, 0f, 3.2f), _redPaintMaterial, _doorMaterial);
            BuildBarrelCluster(decor, new Vector3(-7.7f, 0f, 7.2f), _doorMaterial, _redPaintMaterial);
            BuildBarrel(decor, new Vector3(-5.6f, 0.3f, 6.4f), _redPaintMaterial, new Vector3(90f, 140f, 0f));

            BuildPuddle(decor, new Vector3(2f, 0f, -1f), 1.6f, 1f, 15f);
            BuildPuddle(decor, new Vector3(-4.2f, 0f, 5.6f), 1f, 1.5f, -30f);
            BuildDebris(decor, new Vector3(3.5f, 0f, -1.7f), 7, 61);
            BuildDebris(decor, new Vector3(-1.5f, 0f, 3.5f), 5, 62);

            BuildWallScreen(decor, Side.Entry, 4.5f, 2.6f, 2f, 1.1f, accent, 63);
            BuildWallScreen(decor, Side.Left, -4.9f, 1.9f, 1.4f, 0.9f, accent, 64);
        }

        private static void DecorateAssembly(Transform decor, Material accent)
        {
            BuildConveyor(decor, new Vector3(7.6f, 0f, -4.4f), 0f, 8f);

            // Лежащие на ленте заготовки охранников: ногами к выходу, головой ко входу
            foreach (float z in new[] { -6f, -3.4f, -0.8f })
                BuildStaticMob("Blank", decor, MeleeModel, new Vector3(7.6f, 1.07f, z), new Vector3(-90f, 0f, 0f), 0.95f, _metalMaterial);

            var scanner = Group("Scanner", decor, new Vector3(7.6f, 0f, -3.1f));
            foreach (float x in new[] { -0.75f, 0.75f })
                Box("Post", scanner, new Vector3(x, 1.2f, 0f), new Vector3(0.15f, 2.4f, 0.25f), _darkMetalMaterial);
            Box("Top", scanner, new Vector3(0f, 2.4f, 0f), new Vector3(1.65f, 0.2f, 0.3f), _darkMetalMaterial);
            Box("Beam", scanner, new Vector3(0f, 2.28f, 0f), new Vector3(1.3f, 0.03f, 0.05f), accent);

            BuildRobotArm(decor, new Vector3(5.9f, 0f, -5.7f), 90f, accent);
            BuildRobotArm(decor, new Vector3(5.9f, 0f, -1.9f), 90f, accent);
            CreateLight(decor, new Vector3(6f, 2.6f, -4.4f), new Color(0.9f, 0.4f, 1f), 1.6f, 7f);

            var stands = new[] { -7.6f, -4.6f, 2.2f, 3.6f, 7.6f };
            for (int i = 0; i < stands.Length; i++)
                BuildMobStand(decor, new Vector3(-8.25f, 0f, stands[i]), 90f, i % 2 == 0 ? GunnerModel : MeleeModel, i % 2 == 0 ? _metalMaterial : _specimenMaterial);

            BuildShelf(OnWall("Anchor", decor, Side.Exit, 4.2f), new Vector3(0f, 0f, 0.4f), 0f, 3f, 71);
            BuildPartsBin(decor, new Vector3(7.4f, 0f, 7.6f), 10f, 72);
            BuildPartsBin(decor, new Vector3(-3.5f, 0f, -3f), -5f, 73);
            BuildCrate(decor, new Vector3(3.5f, 0f, -3f), 8f, 1.1f, _bluePaintMaterial);
            BuildCrate(decor, new Vector3(3.6f, 1.1f, -3.05f), -15f, 0.7f, _bluePaintMaterial);
            BuildCrate(decor, new Vector3(-5f, 0f, -8f), 5f, 0.9f);
            BuildCrate(decor, new Vector3(-4f, 0f, -7.9f), -10f, 0.7f);

            BuildHazardStripe(decor, new Vector3(6.2f, 0f, -4.4f), 8f, 90f);
            BuildDebris(decor, new Vector3(4.5f, 0f, -1.5f), 5, 74);

            BuildWallScreen(decor, Side.Entry, -4.5f, 2.6f, 2f, 1.1f, accent, 75);
            BuildWallScreen(decor, Side.Entry, 4.5f, 2.6f, 2f, 1.1f, accent, 76);
            BuildWallScreen(decor, Side.Exit, -7.6f, 2.8f, 1.2f, 0.8f, accent, 77);
        }

        private static void DecorateCore(Transform decor, Material accent)
        {
            var pillars = new[] { new Vector3(-6.8f, 0f, -4.6f), new Vector3(6.8f, 0f, -4.6f), new Vector3(-7.4f, 0f, 7.4f), new Vector3(7.4f, 0f, 7.4f) };

            foreach (Vector3 pillar in pillars)
            {
                BuildPillar(decor, pillar, accent);
                BuildCable(decor, accent, pillar * 0.88f, pillar * 0.25f);
            }

            BuildEye(OnWall("EyeAnchor", decor, Side.Exit, 5.2f, 2.2f), accent);

            // Внутреннее кольцо и «ядро» в центре — только разметка на полу
            for (int i = 0; i < 16; i++)
            {
                float angle = i * 360f / 16f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Box("InnerRing", decor, direction * 2f + Vector3.up * 0.012f, new Vector3(0.5f, 0.01f, 0.08f), accent, new Vector3(0f, angle, 0f));
            }

            Cylinder("CoreMark", decor, new Vector3(0f, 0.012f, 0f), 1.2f, 0.01f, _hazardMaterial);
            Cylinder("CoreGlow", decor, new Vector3(0f, 0.016f, 0f), 0.5f, 0.01f, accent);

            BuildWallScreen(decor, Side.Left, 3.5f, 2.6f, 1.8f, 1.1f, accent, 81);
            BuildWallScreen(decor, Side.Right, -1.2f, 2.6f, 1.6f, 1f, accent, 82);
            BuildWallScreen(decor, Side.Entry, -4.5f, 2.6f, 2f, 1.1f, accent, 83);
            BuildWallScreen(decor, Side.Entry, 4.5f, 2.6f, 2f, 1.1f, accent, 84);

            BuildPuddle(decor, new Vector3(-2.5f, 0f, -1f), 1.8f, 1.2f, 40f);
            BuildPuddle(decor, new Vector3(3f, 0f, 2.5f), 1.2f, 1.6f, -10f);
            BuildDebris(decor, new Vector3(-3f, 0f, 3f), 6, 85);
            BuildDebris(decor, new Vector3(3.5f, 0f, -2f), 6, 86);
        }

        // Сейф-комната: койка, шкафчики, стол, следы боя; крепления от сорванных камер над их обломками
        private static void DecorateSafeRoom(Transform decor, Material accent)
        {
            BuildCot(decor, new Vector3(-7.9f, 0f, -7f), 0f);
            BuildChair(decor, new Vector3(-6.8f, 0f, -7.9f), new Vector3(0f, 20f, 0f));

            foreach (float z in new[] { 2.4f, 3.05f, 3.7f })
                BuildLocker(decor, new Vector3(-8.45f, 0f, z), 90f);

            BuildTable(decor, new Vector3(6.6f, 0f, -6.6f), 10f);
            BuildChair(decor, new Vector3(6.6f, 0f, -5.7f), new Vector3(0f, 190f, 0f));
            BuildChair(decor, new Vector3(5.4f, 0.22f, -6.4f), new Vector3(90f, 60f, 0f));

            BuildCrate(decor, new Vector3(7.6f, 0f, 7.6f), 15f);
            BuildCrate(decor, new Vector3(6.4f, 0f, 7.9f), -5f, 0.8f);
            BuildCrate(decor, new Vector3(7.65f, 1f, 7.55f), 40f, 0.6f);

            foreach (var (side, along) in new[] { (Side.Left, -1f), (Side.Right, 4f), (Side.Exit, -4f) })
            {
                var mount = OnWall("CameraMount", decor, side, along, 2f);
                Box("Plate", mount, new Vector3(0f, 0f, 0.04f), new Vector3(0.35f, 0.35f, 0.08f), _darkMetalMaterial);
                Box("Arm", mount, new Vector3(0f, 0f, 0.25f), new Vector3(0.08f, 0.08f, 0.4f), _darkMetalMaterial);
                Rod("Wire", mount, new Vector3(0f, 0f, 0.45f), new Vector3(0.05f, -0.8f, 0.5f), 0.03f, _rubberMaterial);
                Rod("Wire", mount, new Vector3(0.02f, 0f, 0.45f), new Vector3(-0.1f, -0.6f, 0.55f), 0.025f, _redPaintMaterial);
            }

            BuildDebris(decor, new Vector3(-7.4f, 0f, -1f), 5, 91);
            BuildDebris(decor, new Vector3(7.4f, 0f, 4f), 5, 92);
            BuildDebris(decor, new Vector3(-4f, 0f, 7.6f), 5, 93);
            BuildPuddle(decor, new Vector3(-4f, 0f, 3f), 1.8f, 1.1f, 20f);
            BuildPuddle(decor, new Vector3(4.6f, 0f, -1.6f), 1.2f, 1.6f, -35f);

            // Пьедестал под мечом
            Cylinder("Pedestal", decor, new Vector3(0f, 0.17f, -Half + 7f), 0.9f, 0.34f, _darkMetalMaterial);
            Cylinder("PedestalGlow", decor, new Vector3(0f, 0.35f, -Half + 7f), 0.8f, 0.02f, _weaponMaterial);
            CreateLight(decor, new Vector3(0f, 1.6f, -Half + 7f), new Color(0.4f, 0.9f, 1f), 1.5f, 4f);

            BuildWallScreen(decor, Side.Right, -4f, 2.6f, 1.6f, 1f, accent, 94, true);
            BuildSign(decor, new Vector3(0f, 3.4f, Half - 0.8f), 180f, "ВЫХОД", 2.2f, _safeMaterial, new Color(0.6f, 1f, 0.7f));

            foreach (float x in new[] { -0.9f, 0.9f })
                Rod("SignRod", decor, new Vector3(x, 3.6f, Half - 0.8f), new Vector3(x, 4.5f, Half - 0.8f), 0.03f, _darkMetalMaterial);
        }

        // Коридор — безопасная зона: зелёная разметка, стрелки к следующей комнате, лавка, аптечка, табличка с названием комнаты
        private static void DecorateCorridor(Transform corridor, string title, Material accent)
        {
            var decor = Group("Decor", corridor, Vector3.zero);
            float wallX = GapWidth / 2f;
            float end = CorridorLength / 2f;

            foreach (float x in new[] { -wallX + 0.2f, wallX - 0.2f })
                Box("EdgeStripe", decor, new Vector3(x, 0.006f, 0f), new Vector3(0.12f, 0.012f, CorridorLength), _doorMaterial);

            for (float z = -4.5f; z <= 4.5f; z += 3f)
            {
                Box("Chevron", decor, new Vector3(-0.25f, 0.008f, z), new Vector3(0.75f, 0.012f, 0.14f), _safeMaterial, new Vector3(0f, -45f, 0f));
                Box("Chevron", decor, new Vector3(0.25f, 0.008f, z), new Vector3(0.75f, 0.012f, 0.14f), _safeMaterial, new Vector3(0f, 45f, 0f));
            }

            foreach (float side in new[] { -1f, 1f })
            {
                Box("LightStrip", decor, new Vector3(side * (wallX - 0.03f), 3.3f, 0f), new Vector3(0.06f, 0.06f, CorridorLength), _safeMaterial);
                Box("Baseboard", decor, new Vector3(side * (wallX - 0.06f), 0.15f, 0f), new Vector3(0.12f, 0.3f, CorridorLength), _trimMaterial);
            }

            BuildPipe(decor, new Vector3(-wallX + 0.15f, 3.55f, -end), new Vector3(-wallX + 0.15f, 3.55f, end), 0.2f);
            BuildPipe(decor, new Vector3(-wallX + 0.12f, 3.05f, -end), new Vector3(-wallX + 0.12f, 3.05f, end), 0.12f);
            BuildPipe(decor, new Vector3(wallX - 0.15f, 0.45f, -end), new Vector3(wallX - 0.15f, 0.45f, end), 0.2f);

            foreach (float z in new[] { -end + 0.6f, 0f, end - 0.7f })
                Box("Beam", decor, new Vector3(0f, WallHeight + 0.1f, z), new Vector3(GapWidth + 0.6f, 0.2f, 0.25f), _darkMetalMaterial);

            BuildHangingLamp(decor, new Vector3(0f, WallHeight, 0f), new Color(0.85f, 1f, 0.9f), 3.5f, 11f);

            // Потолок: между стенами комнат, чтобы не мешать дверям уезжать в короба над проёмами
            var ceiling = Group("Ceiling", decor, Vector3.zero);
            float length = CorridorLength - WallThickness;
            Box("Slab", ceiling, new Vector3(0f, CeilingHeight + 0.15f, 0f), new Vector3(GapWidth + WallThickness * 2f, 0.3f, length), _trimMaterial);

            foreach (float side in new[] { -1f, 1f })
                Box("Frieze", ceiling, new Vector3(side * (wallX + WallThickness / 2f), (WallHeight + CeilingHeight) / 2f, 0f), new Vector3(WallThickness, CeilingHeight - WallHeight, CorridorLength), _wallMaterial);

            foreach (float z in new[] { -3f, 3f })
                Box("Panel", ceiling, new Vector3(0f, CeilingHeight - 0.02f, z), new Vector3(0.5f, 0.04f, 1.6f), _lampMaterial);

            BuildBench(decor, new Vector3(-wallX + 0.3f, 0f, -1.8f), 90f, 1.8f);
            var aid = Group("FirstAid", decor, new Vector3(-wallX, 1.5f, 1f), 90f);
            Box("Box", aid, new Vector3(0f, 0f, 0.07f), new Vector3(0.5f, 0.4f, 0.14f), _whiteMaterial);
            Box("Cross", aid, new Vector3(0f, 0f, 0.145f), new Vector3(0.24f, 0.07f, 0.01f), _medicalMaterial);
            Box("Cross", aid, new Vector3(0f, 0f, 0.145f), new Vector3(0.07f, 0.24f, 0.01f), _medicalMaterial);

            BuildSign(decor, new Vector3(wallX - 0.04f, 2.4f, -1.5f), -90f, "БЕЗОПАСНАЯ ЗОНА", 2.6f, _safeMaterial, new Color(0.6f, 1f, 0.7f));
            BuildSign(decor, new Vector3(0f, 3.45f, end - 0.7f), 180f, title, 3.4f, accent, Color.white);

            foreach (float x in new[] { -1.4f, 1.4f })
                Rod("SignRod", decor, new Vector3(x, 3.65f, end - 0.7f), new Vector3(x, WallHeight, end - 0.7f), 0.03f, _darkMetalMaterial);
        }

        // Сумерки: низкое тёплое солнце, туман, трёхцветный ambient — световые полосы и лампы читаются ярче
        private static void SetupAtmosphere()
        {
            var sun = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);

            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(28f, -35f, 0f);
                sun.color = new Color(1f, 0.7f, 0.5f);
                sun.intensity = 0.9f;
                RenderSettings.sun = sun;
            }

            RenderSettings.skybox = _skyMaterial;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.3f, 0.31f, 0.4f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.19f, 0.21f);
            RenderSettings.ambientGroundColor = new Color(0.09f, 0.09f, 0.1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.27f, 0.25f, 0.31f);
            RenderSettings.fogStartDistance = 20f;
            RenderSettings.fogEndDistance = 80f;
        }
    }
}
