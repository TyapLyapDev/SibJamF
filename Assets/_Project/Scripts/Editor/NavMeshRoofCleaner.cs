using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace SibJam.Editor
{
    // Меню SibJam/NavMesh: навмеш печётся со всей геометрии и вылезает на крыши стеллажей, стен и потолки.
    // Запекаем, оставляем только острова, где есть точки спавна/респауна/арена, остальные закрываем Not Walkable объёмами и перепекаем
    public static class NavMeshRoofCleaner
    {
        private const string Menu = "SibJam/NavMesh/";
        private const string VolumesRootName = "_RoofBlockers";
        private const int NotWalkableArea = 1;
        private const float SeedSampleRadius = 3f;
        private const float LevelBucket = 1f;
        private const float VerticalPadding = 0.5f;
        private const float HorizontalPadding = 0.2f;

        [MenuItem(Menu + "Bake Without Roofs")]
        private static void BakeWithoutRoofs()
        {
            NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                Debug.LogError("[NavMeshRoofCleaner] No NavMeshSurface in scene");
                return;
            }

            ClearBlockers(surface);
            surface.BuildNavMesh();

            List<Vector3> seeds = CollectSeeds();
            List<Bounds> blockers = FindUnseededLevels(seeds, out int islands, out int kept);

            var root = new GameObject(VolumesRootName).transform;
            root.SetParent(surface.transform, false);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Bake Without Roofs");

            for (int i = 0; i < blockers.Count; i++)
            {
                var go = new GameObject($"Blocker_{i}");
                go.transform.SetParent(root, false);
                go.transform.position = blockers[i].center;

                var volume = go.AddComponent<NavMeshModifierVolume>();
                volume.center = Vector3.zero;
                volume.size = blockers[i].size;
                volume.area = NotWalkableArea;
            }

            surface.BuildNavMesh();
            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);

            Debug.Log($"[NavMeshRoofCleaner] Islands: {islands}, kept: {kept}, blockers: {blockers.Count}, seeds: {seeds.Count}");
        }

        [MenuItem(Menu + "Clear Roof Blockers")]
        private static void ClearBlockersMenu()
        {
            NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface == null) return;

            ClearBlockers(surface);
            surface.BuildNavMesh();
            EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
        }

        private static void ClearBlockers(NavMeshSurface surface)
        {
            Transform old = surface.transform.Find(VolumesRootName);
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);
        }

        // Точки, где точно должна быть ходьба: спавны волн, респауны локаций, центр арены босса
        private static List<Vector3> CollectSeeds()
        {
            var seeds = new List<Vector3>();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name.StartsWith("SpawnPoint") || t.name == "RespawnPoint" || t.name == "ArenaCenter")
                    seeds.Add(t.position);
            }
            return seeds;
        }

        private static List<Bounds> FindUnseededLevels(List<Vector3> seeds, out int islandCount, out int keptCount)
        {
            NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
            Vector3[] verts = tri.vertices;
            int[] indices = tri.indices;

            // Вершины соседних тайлов дублируются — сшиваем по позиции
            var weldMap = new Dictionary<Vector3Int, int>();
            var weld = new int[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3Int key = Vector3Int.RoundToInt(verts[i] * 20f);
                if (!weldMap.TryGetValue(key, out int id))
                {
                    id = weldMap.Count;
                    weldMap[key] = id;
                }
                weld[i] = id;
            }

            var parent = new int[weldMap.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x)
                {
                    parent[x] = parent[parent[x]];
                    x = parent[x];
                }
                return x;
            }

            for (int t = 0; t < indices.Length; t += 3)
            {
                int a = Find(weld[indices[t]]);
                parent[Find(weld[indices[t + 1]])] = a;
                parent[Find(weld[indices[t + 2]])] = a;
            }

            var kept = new HashSet<int>();
            foreach (Vector3 seed in seeds)
            {
                if (!NavMesh.SamplePosition(seed, out NavMeshHit hit, SeedSampleRadius, NavMesh.AllAreas)) continue;

                int triangle = TriangleUnder(verts, indices, hit.position);
                if (triangle >= 0)
                    kept.Add(Find(weld[indices[triangle]]));
            }

            // Блокеры делим по уровням высоты, чтобы рамка наклонного острова не накрыла пол под ним
            var levels = new Dictionary<(int island, int level), Bounds>();
            var islands = new HashSet<int>();
            for (int t = 0; t < indices.Length; t += 3)
            {
                int island = Find(weld[indices[t]]);
                islands.Add(island);
                if (kept.Contains(island)) continue;

                Vector3 p0 = verts[indices[t]], p1 = verts[indices[t + 1]], p2 = verts[indices[t + 2]];
                int level = Mathf.FloorToInt((p0.y + p1.y + p2.y) / 3f / LevelBucket);
                var key = (island, level);

                if (levels.TryGetValue(key, out Bounds b))
                {
                    b.Encapsulate(p0);
                    b.Encapsulate(p1);
                    b.Encapsulate(p2);
                }
                else
                {
                    b = new Bounds(p0, Vector3.zero);
                    b.Encapsulate(p1);
                    b.Encapsulate(p2);
                }
                levels[key] = b;
            }

            islandCount = islands.Count;
            keptCount = kept.Count;

            var result = new List<Bounds>(levels.Count);
            foreach (Bounds b in levels.Values)
            {
                Bounds padded = b;
                padded.Expand(new Vector3(HorizontalPadding * 2f, VerticalPadding * 2f, HorizontalPadding * 2f));
                result.Add(padded);
            }
            return result;
        }

        // Треугольник, в чью проекцию на XZ попадает точка и который ближе всех к ней по высоте
        private static int TriangleUnder(Vector3[] verts, int[] indices, Vector3 point)
        {
            int best = -1;
            float bestDy = float.MaxValue;
            var p = new Vector2(point.x, point.z);

            for (int t = 0; t < indices.Length; t += 3)
            {
                Vector3 a = verts[indices[t]], b = verts[indices[t + 1]], c = verts[indices[t + 2]];
                if (!TryBarycentric(p, new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(c.x, c.z), out Vector3 w))
                    continue;

                float dy = Mathf.Abs(a.y * w.x + b.y * w.y + c.y * w.z - point.y);
                if (dy < bestDy)
                {
                    bestDy = dy;
                    best = t;
                }
            }
            return best;
        }

        private static bool TryBarycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out Vector3 weights)
        {
            weights = default;
            Vector2 v0 = b - a, v1 = c - a, v2 = p - a;
            float den = v0.x * v1.y - v1.x * v0.y;
            if (Mathf.Abs(den) < 1e-8f) return false;

            float v = (v2.x * v1.y - v1.x * v2.y) / den;
            float w = (v0.x * v2.y - v2.x * v0.y) / den;
            float u = 1f - v - w;
            const float eps = -0.001f;
            if (u < eps || v < eps || w < eps) return false;

            weights = new Vector3(u, v, w);
            return true;
        }
    }
}
