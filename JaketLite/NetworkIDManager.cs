using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Patches;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

namespace Polarite.Multiplayer
{
    public static class SceneObjectCache
    {
        private static readonly Dictionary<string, GameObject> pathToObject = new Dictionary<string, GameObject>();
        private static readonly Dictionary<int, string> idToPath = new Dictionary<int, string>();
        private static bool initialized;
        private static bool needsRebuild;

        public static void Initialize()
        {
            if (initialized)
                Clear();

            initialized = true;
            SceneManager.sceneLoaded += (_, _1) => FlagRebuild();
            SceneManager.sceneUnloaded += _ => FlagRebuild();
            SceneManager.activeSceneChanged += (_, _1) => FlagRebuild();

            CoroutineRunner.InvokeNextFrame(Rebuild);
        }

        private static void FlagRebuild()
        {
            needsRebuild = true;
            CoroutineRunner.InvokeNextFrame(() =>
            {
                if (needsRebuild)
                {
                    needsRebuild = false;
                    Rebuild();
                }
            });
        }

        public static void Rebuild()
        {
            pathToObject.Clear();
            idToPath.Clear();

            foreach (var scene in GetLoadedScenes())
            {
                if (!scene.isLoaded || !scene.IsValid())
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                    AddRecursive(root, scene);
            }
        }

        private static void AddRecursive(GameObject obj, Scene scene)
        {
            if (obj == null)
                return;

            string path = BuildScenePath(obj, scene);
            if (!pathToObject.ContainsKey(path))
                pathToObject[path] = obj;

            idToPath[obj.GetInstanceID()] = path;

            foreach (Transform child in obj.transform)
                AddRecursive(child.gameObject, scene);
        }

        private static string BuildScenePath(GameObject obj, Scene scene)
        {
            StringBuilder sb = new StringBuilder();
            Transform t = obj.transform;
            while (t != null)
            {
                sb.Insert(0, "/" + t.name);
                t = t.parent;
            }

            return $"{scene.path}{sb}";
        }

        public static string GetScenePath(GameObject obj)
        {
            if (obj == null) return "";

            int id = obj.GetInstanceID();
            if (idToPath.TryGetValue(id, out string cached))
                return cached;

            string path = BuildScenePath(obj, obj.scene);
            idToPath[id] = path;
            pathToObject[path] = obj;
            return path;
        }

        public static GameObject Find(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (needsRebuild)
                Rebuild();

            CleanupDestroyed();

            if (pathToObject.TryGetValue(path, out var obj) && obj != null)
                return obj;

            GameObject found = FindManually(path);
            if (found != null)
                Add(found);

            return found;
        }

        public static EnemyIdentifier TrySpawnEnemy(string path, EnemyType fallback, Vector3 pos, Quaternion rot)
        {
            try
            {
                GameObject obj = Find(path);
                if (obj == null || NetworkManager.Sandbox || CyberSync.Active)
                {
                    EnemyIdentifier newE = EntityStorage.Spawn(fallback, pos, rot, NetworkManager.Sandbox);
                    if(!Contains(newE.gameObject))
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
                if (!Contains(newE.gameObject))
                {
                    Add(newE.gameObject);
                }
                return newE;
            }
        }

        public static void Add(GameObject obj)
        {
            if (obj == null) return;
            string path = GetScenePath(obj);
            pathToObject[path] = obj;
            idToPath[obj.GetInstanceID()] = path;
        }

        public static void Remove(GameObject obj)
        {
            if (obj == null) return;

            int id = obj.GetInstanceID();
            if (idToPath.TryGetValue(id, out string path))
            {
                idToPath.Remove(id);
                pathToObject.Remove(path);
            }
        }

        public static bool Contains(GameObject obj)
        {
            if (obj == null) return false;
            return idToPath.ContainsKey(obj.GetInstanceID());
        }

        public static bool Contains(string path)
        {
            CleanupDestroyed();
            return pathToObject.ContainsKey(path);
        }

        public static void Clear()
        {
            pathToObject.Clear();
            idToPath.Clear();
            initialized = false;
            needsRebuild = false;
        }

        private static void CleanupDestroyed()
        {
            List<string> toRemove = new List<string>();
            foreach (var kvp in pathToObject)
            {
                if (kvp.Value == null)
                    toRemove.Add(kvp.Key);
            }

            foreach (var key in toRemove)
                pathToObject.Remove(key);

            foreach (var kvp in idToPath.Where(kvp => !pathToObject.ContainsKey(kvp.Value)).ToList())
                idToPath.Remove(kvp.Key);
        }

        private static GameObject FindManually(string path)
        {
            string[] split = path.Split('/');
            if (split.Length < 2)
                return null;

            string scenePath = split[0];
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            GameObject current = null;

            for (int i = 1; i < split.Length; i++)
            {
                string segment = split[i];
                if (i == 1)
                {
                    current = roots.FirstOrDefault(r => r.name == segment);
                    if (current == null) return null;
                }
                else
                {
                    Transform child = current.transform.Find(segment);
                    if (child == null) return null;
                    current = child.gameObject;
                }
            }
            return current;
        }

        private static IEnumerable<Scene> GetLoadedScenes()
        {
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
                yield return SceneManager.GetSceneAt(i);
        }

        private class CoroutineRunner : MonoBehaviour
        {
            private static CoroutineRunner instance;

            public static void EnsureExists()
            {
                if (instance != null) return;
                GameObject runner = new GameObject("SceneObjectCache_Runner");
                DontDestroyOnLoad(runner);
                instance = runner.AddComponent<CoroutineRunner>();
            }

            public static void InvokeNextFrame(Action action)
            {
                EnsureExists();
                instance.StartCoroutine(instance.InvokeAfterFrame(action));
            }

            private System.Collections.IEnumerator InvokeAfterFrame(Action action)
            {
                yield return null;
                action?.Invoke();
            }
        }
    }
}
