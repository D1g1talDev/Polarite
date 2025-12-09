using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Polarite.Patches;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

using Random = UnityEngine.Random;

namespace Polarite.Multiplayer
{
    public static class SceneObjectCache
    {
        private static readonly Dictionary<string, GameObject> pathToObject = new Dictionary<string, GameObject>();
        private static readonly Dictionary<int, string> idToPath = new Dictionary<int, string>();
        private static readonly object sync = new object();

        private static bool initialized;
        private static bool needsRebuild;
        private static bool scheduledRebuild;

        public static void Initialize()
        {
            if (initialized)
                Clear();

            CoroutineRunner.EnsureExists();

            CoroutineRunner.forceInvokeAction += () =>
            {
                scheduledRebuild = false;

                if (needsRebuild)
                {
                    needsRebuild = false;
                    Rebuild();
                }
            };

            SceneManager.sceneLoaded += (_, __) => FlagRebuild();
            SceneManager.sceneUnloaded += _ => FlagRebuild();
            SceneManager.activeSceneChanged += (_, __) => FlagRebuild();

            initialized = true;
            FlagRebuild();
        }

        private static void FlagRebuild()
        {
            needsRebuild = true;

            if (scheduledRebuild) return;
            scheduledRebuild = true;

            CoroutineRunner.InvokeNextFrame(() =>
            {
                scheduledRebuild = false;

                if (needsRebuild)
                {
                    needsRebuild = false;
                    Rebuild();
                }
            });
        }

        public static void Rebuild()
        {
            var scenes = GetLoadedScenes().ToArray();

            lock (sync)
            {
                pathToObject.Clear();
                idToPath.Clear();

                foreach (var scene in scenes)
                {
                    if (!scene.IsValid() || !scene.isLoaded)
                        continue;

                    foreach (var root in scene.GetRootGameObjects())
                        AddRecursive(root, scene);
                }
            }
        }

        private static void AddRecursive(GameObject obj, Scene scene)
        {
            if (obj == null) return;

            string path = BuildPathForObject(obj);

            lock (sync)
            {
                pathToObject[path] = obj;
                idToPath[obj.GetInstanceID()] = path;
            }

            foreach (Transform child in obj.transform)
                AddRecursive(child.gameObject, scene);
        }

        public static EnemyIdentifier TrySpawnEnemy(string path, EnemyType fallback, Vector3 pos, Quaternion rot)
        {
            try
            {
                GameObject obj = Find(path);
                if (obj == null)
                {
                    EnemyIdentifier newE = EntityStorage.Spawn(fallback, pos, rot, NetworkManager.Sandbox);
                    if (newE != null && !Contains(newE.gameObject))
                    {
                        Add(newE.gameObject);
                    }
                    return newE;
                }
                else
                {
                    obj.SetActive(true);
                    obj.transform.position = pos;
                    obj.transform.rotation = rot;
                    return obj.GetComponent<EnemyIdentifier>();
                }
            }
            catch
            {
                EnemyIdentifier newE = EntityStorage.Spawn(fallback, pos, rot, NetworkManager.Sandbox);
                if (newE != null && !Contains(newE.gameObject))
                {
                    Add(newE.gameObject);
                }
                return newE;
            }
        }

        private static string BuildUniquePath(GameObject obj)
        {
            Transform t = obj.transform;
            List<string> parts = new List<string>();

            while (t != null)
            {
                int index = t.GetSiblingIndex();
                parts.Add($"{EscapeSegment(t.name)}[{index}]");
                t = t.parent;
            }

            parts.Reverse();

            string sceneName = obj.scene.name;

            return $"U:{sceneName}/{string.Join("/", parts)}";
        }


        private static string BuildScenePath(GameObject obj, Scene scene)
        {
            StringBuilder sb = new StringBuilder();
            Transform t = obj.transform;

            while (t != null)
            {
                sb.Insert(0, "/" + EscapeSegment(t.name));
                t = t.parent;
            }

            return $"{scene.name}{sb}";
        }

        private static string EscapeSegment(string segment)
        {
            return segment.Replace("/", "%2F");
        }

        private static string UnescapeSegment(string segment)
        {
            return segment.Replace("%2F", "/");
        }

        private static bool ShouldUseUniquePath(GameObject obj)
        {
            if (obj == null) return false;

            if (obj.GetComponent<EnemyIdentifier>() != null)
                return true;

            return false;
        }

        private static string BuildPathForObject(GameObject obj)
        {
            if (obj == null) return "";

            if (ShouldUseUniquePath(obj))
                return BuildUniquePath(obj);

            return BuildPureHierarchyPath(obj);
        }

        public static string GetScenePath(GameObject obj)
        {
            if (obj == null)
                return "";

            int id = obj.GetInstanceID();

            lock (sync)
            {
                if (idToPath.TryGetValue(id, out string cached))
                {
                    bool cachedIsUnique = cached != null && cached.StartsWith("U:");
                    bool wantUnique = ShouldUseUniquePath(obj);

                    if (cachedIsUnique != wantUnique)
                    {
                        pathToObject.Remove(cached);
                        idToPath.Remove(id);
                    }
                    else
                    {
                        return cached;
                    }
                }
            }
            string newPath = BuildPathForObject(obj);

            lock (sync)
            {
                pathToObject[newPath] = obj;
                idToPath[id] = newPath;
            }
            return newPath;
        }

        private static string BuildPureHierarchyPath(GameObject obj)
        {
            StringBuilder sb = new StringBuilder();
            Transform t = obj.transform;

            while (t != null)
            {
                sb.Insert(0, "/" + EscapeSegment(t.name));
                t = t.parent;
            }

            return $"{obj.scene.name}{sb}";
        }

        public static string GetOrCreatePath(GameObject obj)
        {
            if (obj == null) return "";

            return GetScenePath(obj);
        }
        public static GameObject Find(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (needsRebuild)
                Rebuild();

            CleanupDestroyed();

            lock (sync)
            {
                if (pathToObject.TryGetValue(path, out var obj) && obj != null)
                    return obj;
            }

            return null;
        }
        public static void Add(GameObject obj)
        {
            if (obj == null) return;

            string path = GetScenePath(obj);

            lock (sync)
            {
                pathToObject[path] = obj;
                idToPath[obj.GetInstanceID()] = path;
            }
        }

        public static void Add(string path, GameObject obj)
        {
            if (obj == null || string.IsNullOrEmpty(path)) return;

            lock (sync)
            {
                pathToObject[path] = obj;
                idToPath[obj.GetInstanceID()] = path;
            }
        }

        public static void Remove(GameObject obj)
        {
            if (obj == null) return;

            int id = obj.GetInstanceID();

            lock (sync)
            {
                if (idToPath.TryGetValue(id, out string path))
                {
                    idToPath.Remove(id);
                    pathToObject.Remove(path);
                }
            }
        }

        public static bool Contains(GameObject obj)
        {
            if (obj == null) return false;

            lock (sync)
                return idToPath.ContainsKey(obj.GetInstanceID());
        }

        public static bool Contains(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            CleanupDestroyed();

            lock (sync)
                return pathToObject.ContainsKey(path);
        }
        public static void Clear()
        {
            lock (sync)
            {
                pathToObject.Clear();
                idToPath.Clear();
                initialized = false;
                needsRebuild = false;
                scheduledRebuild = false;
            }
        }
        private static void CleanupDestroyed()
        {
            lock (sync)
            {
                foreach (var key in pathToObject.Where(k => k.Value == null).Select(k => k.Key).ToList())
                    pathToObject.Remove(key);

                foreach (var kv in idToPath.ToList())
                {
                    if (!pathToObject.TryGetValue(kv.Value, out var obj) || obj == null)
                        idToPath.Remove(kv.Key);
                }
            }
        }
        private static IEnumerable<Scene> GetLoadedScenes()
        {
            int count = SceneManager.sceneCount;

            for (int i = 0; i < count; i++)
                yield return SceneManager.GetSceneAt(i);
        }
        public class CoroutineRunner : MonoBehaviour
        {
            private static CoroutineRunner instance;

            public static Action forceInvokeAction;

            public static void EnsureExists()
            {
                if (instance != null) return;

                GameObject runner = new GameObject("SceneObjectCache_Runner");
                DontDestroyOnLoad(runner);
                instance = runner.AddComponent<CoroutineRunner>();
                runner.hideFlags = HideFlags.HideAndDontSave;
            }

            public static void InvokeNextFrame(Action action)
            {
                EnsureExists();
                instance.StartCoroutine(instance.InvokeAfterFrame(action));
            }
            public static void ForceInvokeFrame()
            {
                InvokeNextFrame(forceInvokeAction);
            }

            private System.Collections.IEnumerator InvokeAfterFrame(Action action)
            {
                yield return null;

                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SceneObjectCache CoroutineRunner action threw: {ex}");
                }
            }
        }
    }
}
