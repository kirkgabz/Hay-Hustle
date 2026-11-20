using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace HayHustle
{
    /// <summary>
    /// Cloud save in Supabase (table farm_saves, one row per player, protected by Row Level Security).
    /// - After signing in, the newest farm (this device or the cloud) wins, so the same farm plays on phone and PC.
    /// - While playing, every save is also uploaded (at most every 15 seconds, and right away when you leave).
    /// The save on the device is always kept too, so the game also works offline.
    /// </summary>
    public static class CloudSave
    {
        public static bool Syncing { get; private set; }
        public static string Status { get; private set; } = "";
        /// <summary>The account whose cloud farm was already checked this session.</summary>
        public static string SyncedUser { get; private set; } = "";
        /// <summary>The account we already tried to sync this session (so it is tried once automatically).</summary>
        public static string TriedUser { get; private set; } = "";
        public static System.DateTime? LastUpload { get; private set; }

        const float UploadEvery = 15f;
        static string pendingJson;
        static bool uploading;
        static float nextUpload;

        public static void Reset()
        {
            pendingJson = null;
            uploading = false;
            Syncing = false;
            Status = "";
            SyncedUser = "";
            TriedUser = "";
            LastUpload = null;
        }

        static void Run(IEnumerator routine)
        {
            if (GameManager.I) GameManager.I.StartCoroutine(routine);
        }

        // ================= Upload =================

        /// <summary>Remember the latest save; it is uploaded on the next Tick (or Flush).</summary>
        public static void Queue(string json)
        {
            if (!Backend.SignedIn || SyncedUser != Backend.UserId) return;   // never upload before the cloud farm was checked
            pendingJson = json;
        }

        public static void Tick()
        {
            if (pendingJson != null && !uploading && Time.realtimeSinceStartup >= nextUpload) Run(Upload());
        }

        /// <summary>Upload now (leaving the game, going to the title, switching apps).</summary>
        public static void Flush()
        {
            if (pendingJson != null && !uploading) Run(Upload());
        }

        static IEnumerator Upload()
        {
            uploading = true;
            string json = pendingJson;
            pendingJson = null;
            yield return Backend.EnsureToken();
            string body = "{\"player_id\":\"" + Backend.UserId + "\",\"data\":" + json +
                          ",\"updated_at\":\"" + System.DateTime.UtcNow.ToString("o") + "\"}";
            using (var req = Backend.Rest("POST", "farm_saves?on_conflict=player_id", body))
            {
                req.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=minimal");
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    LastUpload = System.DateTime.Now;
                    Status = "Saved to the cloud";
                }
                else
                {
                    if (pendingJson == null) pendingJson = json;   // try again later
                    Status = "Cloud save failed - it will try again";
                    Debug.LogWarning($"Hay Hustle: cloud save failed ({req.responseCode}) {req.error} {req.downloadHandler?.text}");
                }
            }
            nextUpload = Time.realtimeSinceStartup + UploadEvery;
            uploading = false;
        }

        // ================= Download after sign-in =================

        public static void Sync(System.Action done = null)
        {
            if (Syncing || !Backend.SignedIn) return;
            Run(SyncRoutine(done));
        }

        static IEnumerator SyncRoutine(System.Action done)
        {
            Syncing = true;
            TriedUser = Backend.UserId;
            Status = "Loading your farm from the cloud...";
            yield return Backend.EnsureToken();
            string uid = Backend.UserId;

            using (var req = Backend.Rest("GET", "farm_saves?select=data&player_id=eq." + uid))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Status = "Could not reach the cloud - playing the farm saved on this device.";
                    Debug.LogWarning($"Hay Hustle: cloud load failed ({req.responseCode}) {req.error}");
                    Syncing = false;
                    done?.Invoke();
                    yield break;   // SyncedUser stays empty: nothing is uploaded, so the cloud farm is never overwritten by mistake
                }

                string cloudJson = ExtractValue(req.downloadHandler.text, "data");
                SaveData cloud = null;
                if (!string.IsNullOrEmpty(cloudJson))
                {
                    try { cloud = JsonUtility.FromJson<SaveData>(cloudJson); } catch (System.Exception e) { Debug.LogWarning("Hay Hustle: cloud save unreadable - " + e.Message); }
                }
                var local = SaveSystem.Load();
                bool localMine = local != null && (string.IsNullOrEmpty(local.ownerId) || local.ownerId == uid);
                SyncedUser = uid;

                if (cloud != null)
                {
                    if (local == null || !localMine || cloud.savedAt > local.savedAt)
                    {
                        SaveSystem.Backup();
                        SaveSystem.WriteRaw(cloudJson);
                        Status = "Your farm was loaded from the cloud.";
                    }
                    else if (local.savedAt > cloud.savedAt)
                    {
                        local.ownerId = uid;
                        Queue(JsonUtility.ToJson(local));
                        Flush();
                        Status = "The farm on this device is newer - it was saved to the cloud.";
                    }
                    else Status = "Your farm is up to date with the cloud.";
                }
                else if (local != null && localMine)
                {
                    local.ownerId = uid;
                    SaveSystem.Save(local);
                    Queue(JsonUtility.ToJson(local));
                    Flush();
                    Status = "Your farm is now saved in the cloud.";
                }
                else if (local != null)
                {
                    SaveSystem.Backup();   // another account's farm: kept in Restore backup
                    SaveSystem.Delete();
                    Status = "No farm on this account yet - start a new farm!";
                }
                else Status = "No farm on this account yet - start a new farm!";
            }
            Syncing = false;
            done?.Invoke();
        }

        /// <summary>Gets the raw JSON object/array/value of "key" from a JSON text (the first match).</summary>
        static string ExtractValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int k = json.IndexOf("\"" + key + "\"", System.StringComparison.Ordinal);
            if (k < 0) return null;
            int i = json.IndexOf(':', k) + 1;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length) return null;
            char open = json[i];
            if (open != '{' && open != '[') return null;
            char close = open == '{' ? '}' : ']';
            int depth = 0;
            bool inString = false;
            for (int j = i; j < json.Length; j++)
            {
                char c = json[j];
                if (inString)
                {
                    if (c == '\\') j++;
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') inString = true;
                else if (c == '{' || c == '[') depth++;
                else if (c == '}' || c == ']')
                {
                    depth--;
                    if (depth == 0) return c == close ? json.Substring(i, j - i + 1) : null;
                }
            }
            return null;
        }
    }
}
