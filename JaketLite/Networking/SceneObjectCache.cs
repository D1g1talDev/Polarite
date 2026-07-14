using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Polarite.Debugging;
using Polarite.Networking;
using Polarite.Networking.Extensions;
using Polarite.Patches;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Polarite.Multiplayer
{
    public static class SceneObjectCache
    {
        public static readonly Dictionary<string, GameObject> Paths = new Dictionary<string, GameObject>();
        public static readonly Dictionary<int, string> Ids = new Dictionary<int, string>();
        public static readonly Dictionary<int, GameObject> NetObjectIndex = new Dictionary<int, GameObject>();
        private static readonly object sync = new object();

        public static bool initialized;
        public static bool needsRebuild;
        public static bool scheduledRebuild;

        public static void Init()
        {
            if (initialized) Clear();

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
            if (NetworkManager.SceneLoading) return;

            Scene[] scenes = GetLoadedScenes().ToArray();
            lock (sync)
            {
                Paths.Clear();
                Ids.Clear();
                NetObjectIndex.Clear();

                foreach (var scene in scenes)
                {
                    if (!scene.IsValid() || !scene.isLoaded) continue;
                    foreach (var root in scene.GetRootGameObjects())
                        AddRecursive(root);
                }
            }
        }

        private static void AddRecursive(GameObject obj)
        {
            if (obj == null) return;

            string path = BuildPathForObject(obj);
            lock (sync)
            {
                bool isNet = obj.IsNetwork();
                if (isNet)
                {
                    int index = GrabIndex(obj);
                    NetObjectIndex[index] = obj;
                }
                Paths[path] = obj;
                Ids[obj.GetInstanceID()] = path;
            }

            foreach (Transform child in obj.transform)
                AddRecursive(child.gameObject);
        }

        private static string GrabSimpleId(GameObject obj)
        {
            if (obj == null) return "Unknown";

            if (obj.TryGetComponent<INetworkObject>(out var netObj) && !string.IsNullOrEmpty(netObj.SimpleID))
                return netObj.SimpleID;

            return "Unknown";
        }
        private static int GrabIndex(GameObject obj)
        {
            if(obj.TryGetComponent<INetworkObject>(out var objNet))
            {
                return objNet.Index;
            }
            return -1;
        }

        private static bool ShouldUseUniquePath(GameObject obj)
        {
            if (obj == null) return false;
            return obj.IsNetwork();
        }

        private static string BuildUniquePathBody(GameObject obj)
        {
            List<string> parts = new List<string>();
            Transform t = obj.transform;

            while (t != null)
            {
                parts.Add(EscapeSegment(t.name));
                t = t.parent;
            }
            string simpleId = GrabSimpleId(obj);
            int index = GrabIndex(obj);
            parts.Reverse();
            Logs.Debug($"Built unique path for {obj.name}: {obj.scene.name}/{index}/{simpleId}/{string.Join("/", parts)}", name: "SceneObjectCache");
            return $"{obj.scene.name}/{index}/{simpleId}/{string.Join("/", parts)}";
        }

        private static string BuildHierarchyPathBody(GameObject obj)
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

        private static string BuildPathForObject(GameObject obj)
        {
            if (obj == null) return "";

            string body = ShouldUseUniquePath(obj) ? BuildUniquePathBody(obj) : BuildHierarchyPathBody(obj);
            return body;
        }

        public static string GetScenePath(GameObject obj)
        {
            if (obj == null) return "";

            int id = obj.GetInstanceID();      
            lock (sync)
            {
                if (Ids.TryGetValue(id, out string cached))
                {
                    return cached;
                }
            }

            string newPath = BuildPathForObject(obj);
            lock (sync)
            {
                Paths[newPath] = obj;
                Ids[id] = newPath;
            }

            return newPath;
        }
        public static string RemakePath(GameObject obj)
        {
            Remove(obj);
            return GetScenePath(obj);
        }
        private static bool EnemyValid(GameObject obj)
        {
            if(obj == null)
            {
                return false;
            }
            EnemyIdentifier eid = obj.GetComponentInChildren<EnemyIdentifier>(true);
            if (eid == null)
            {
                return false;
            }
            return !eid.dead && eid.health > 0;
        }

        public static EnemyIdentifier TrySpawnEnemy(string path, EnemyType fallback, Vector3 pos, Quaternion rot, ulong owner)
        {
            try
            {
                Logs.Debug("spawnin " + path);
                GameObject obj = Find(path);
                if (!EnemyValid(obj))
                {
                    Logs.Debug("notvalid " + path);
                    EnemyIdentifier newE = EntityStorage.Spawn(fallback, pos, rot, owner, path);
                    INetworkObject netObj = newE.gameObject.NetObject();
                    if (newE != null && !ContainsIndex(newE.gameObject))
                    {
                        Add(path, newE.gameObject);
                    }
                    netObj.Base.owner = owner;
                    return newE;
                }
                obj.SetActive(true);
                obj.transform.SetPositionAndRotation(pos, rot);
                INetworkObject newNet = EntityStorage.AddNetEnemy(obj, owner, path);
                return obj.GetComponentInChildren<EnemyIdentifier>(true);
            }
            catch (Exception)
            {
                Logs.Debug("catched " + path);
                EnemyIdentifier newE = EntityStorage.Spawn(fallback, pos, rot, owner, path);
                INetworkObject netObj = newE.GetComponent<INetworkObject>();
                if (newE != null && !ContainsIndex(newE.gameObject))
                {
                    Add(path, newE.gameObject);
                }
                netObj.Base.owner = owner;
                return newE;
            }
        }

        public static GameObject Find(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            CleanupDestroyed();

            if(int.TryParse(path.Split('/').Length > 1 ? path.Split('/')[1] : "-1", out int result))
            {
                if (result != -1 && NetObjectIndex.TryGetValue(result, out var obj2) && obj2 != null)
                    return obj2;
            }

            lock (sync)
            {
                if (Paths.TryGetValue(path, out var obj1) && obj1 != null)
                    return obj1;
            }

            return null;
        }

        public static void Add(GameObject obj)
        {
            if (obj == null) return;

            string path = GetScenePath(obj);
            lock (sync)
            {
                Paths[path] = obj;
                Ids[obj.GetInstanceID()] = path;
                if(ShouldUseUniquePath(obj))
                {
                    int index = GrabIndex(obj);
                    NetObjectIndex[index] = obj;
                }
            }
        }

        public static void Add(string path, GameObject obj)
        {
            if (obj == null || string.IsNullOrEmpty(path)) return;

            lock (sync)
            {
                Paths[path] = obj;
                Ids[obj.GetInstanceID()] = path;
                if(ShouldUseUniquePath(obj))
                {
                    int index = GrabIndex(obj);
                    NetObjectIndex[index] = obj;
                }
            }
        }

        public static void Remove(GameObject obj)
        {
            int id = obj.GetInstanceID();
            int index = GrabIndex(obj);

            lock (sync)
            {
                if (Ids.TryGetValue(id, out string path))
                {
                    Ids.Remove(id);
                    Paths.Remove(path);
                }
                if(NetObjectIndex.ContainsKey(index))
                {
                    NetObjectIndex.Remove(index);
                }
            }
        }

        public static bool Contains(GameObject obj)
        {
            if (obj == null) return false;
            lock (sync) return Ids.ContainsKey(obj.GetInstanceID());
        }

        public static bool Contains(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            CleanupDestroyed();
            lock (sync) return Paths.ContainsKey(path);
        }
        public static bool ContainsIndex(GameObject obj)
        {
            lock (sync)
            {
                int index = GrabIndex(obj);
                return NetObjectIndex.ContainsKey(index);
            }
        }
        public static bool ContainsIndex(int index)
        {
            lock (sync)
            {
                return NetObjectIndex.ContainsKey(index);
            }
        }

        public static void Clear()
        {
            lock (sync)
            {
                Paths.Clear();
                Ids.Clear();
                NetObjectIndex.Clear();
                initialized = false;
                needsRebuild = false;
                scheduledRebuild = false;
            }
        }

        private static void CleanupDestroyed()
        {
            if (Paths.Count < Ids.Count + 8) return;

            lock (sync)
            {
                foreach (var key in Paths.Where(k => k.Value == null).Select(k => k.Key).ToList())
                    Paths.Remove(key);
                foreach (var key2 in NetObjectIndex.Where(k => k.Value == null).Select(k => k.Key).ToList())
                    NetObjectIndex.Remove(key2);

                foreach (var kv in Ids.ToList())
                {
                    if (!Paths.TryGetValue(kv.Value, out var obj) || obj == null)
                        Ids.Remove(kv.Key);
                }
            }
        }

        private static IEnumerable<Scene> GetLoadedScenes()
        {
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++) yield return SceneManager.GetSceneAt(i);
        }

        private static string EscapeSegment(string segment)
        {
            return segment.Replace("/", "%2F");
        }

        public class CoroutineRunner : MonoBehaviour
        {
            private static CoroutineRunner Instance;
            public static Action forceInvokeAction;

            public static void EnsureExists()
            {
                if (Instance != null) return;

                GameObject runner = new GameObject("SceneObjectCache");
                DontDestroyOnLoad(runner);
                Instance = runner.AddComponent<CoroutineRunner>();
                runner.hideFlags = HideFlags.HideAndDontSave;
            }

            public static void InvokeNextFrame(Action action)
            {
                EnsureExists();
                Instance.StartCoroutine(Instance.InvokeAfterFrame(action));
            }

            public static void ForceInvokeFrame()
            {
                InvokeNextFrame(forceInvokeAction);
            }

            private System.Collections.IEnumerator InvokeAfterFrame(Action action)
            {
                yield return null;
                action?.Invoke();
            }
        }
    }
}
