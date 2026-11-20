using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HayHustle
{
    [System.Serializable]
    public class AnimalSave
    {
        public int kind, stage;
        public float age, hunger, wellFed;
        public bool rare;
        public float x, z;
        public int field;
    }

    [System.Serializable]
    public class FieldSave
    {
        public int index;
        public bool owned;
        public float[] grass;
    }

    [System.Serializable]
    public class SurvivorSave
    {
        public int id, skill, job, field;
        public string name;
        public float health, morale;
    }

    [System.Serializable]
    public class CropSave
    {
        public int crop = -1;
        public float growth;
        public bool watered;
    }

    [System.Serializable]
    public class SaveData
    {
        public int version = 2;
        public string farmName;
        public int character;
        public int coins, water, waterMax, xp, warehouseCapacity;
        public int[] inventory;
        public float[] grass;   // version 1 only (home pasture)
        public List<FieldSave> fields = new List<FieldSave>();
        public List<AnimalSave> animals = new List<AnimalSave>();
        public List<int> workers = new List<int>();
        public List<int> workerFields = new List<int>();   // -1 = all fields (rotation)
        public float timeOfDay;
        public int day;
        public int predatorsCaught, animalsLost;
        public string lastRewardDate = "";
        public int rewardStreak;
        public int wanted;
        public int mess;
        public int cats, dogs;
        public int fenceLevel, traps, lamps;
        public List<float> lampXZ;   // x, z of each lamp post (newer saves)
        public bool tutorialDone;
        public List<SurvivorSave> survivors = new List<SurvivorSave>();
        public List<CropSave> crops = new List<CropSave>();
        public int nextSurvivorId, bunkLevel, barnLevel, farmerCrop;
        public float farmGateDamage;   // 0 = the farm gate is whole
        public int[] seeds;
        public float wood = -1f;   // -1 = older save
        public int guardWeapon;
    }

    /// <summary>
    /// Local save file. Later this is mirrored to the Supabase backend (cloud save),
    /// see backend/supabase in the project root.
    /// </summary>
    public static class SaveSystem
    {
        public const int BackupCount = 3;

        static string FilePath => Path.Combine(Application.persistentDataPath, "hayhustle_save.json");
        static string BackupPath(int i) => Path.Combine(Application.persistentDataPath, $"hayhustle_save.backup{i}.json");
        static string BeforeRestorePath => Path.Combine(Application.persistentDataPath, "hayhustle_save.before_restore.json");

        public static bool Exists => File.Exists(FilePath);

        public static void Save(SaveData data)
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(data, true)); }
            catch (IOException e) { Debug.LogError("Hay Hustle: could not save - " + e.Message); }
        }

        public static SaveData Load()
        {
            if (!Exists) return null;
            try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
            catch (System.Exception e) { Debug.LogError("Hay Hustle: could not load save - " + e.Message); return null; }
        }

        // ================= Backups =================

        public class BackupInfo
        {
            public int Index;
            public System.DateTime Time;
            public string Summary;
        }

        /// <summary>Copies the current save to backup 1 (older backups move to 2 and 3; the oldest is dropped).</summary>
        public static void Backup()
        {
            if (!Exists) return;
            try
            {
                for (int i = BackupCount; i > 1; i--)
                    if (File.Exists(BackupPath(i - 1))) File.Copy(BackupPath(i - 1), BackupPath(i), true);
                File.Copy(FilePath, BackupPath(1), true);
            }
            catch (System.Exception e) { Debug.LogError("Hay Hustle: could not make a backup - " + e.Message); }
        }

        public static List<BackupInfo> ListBackups()
        {
            var list = new List<BackupInfo>();
            for (int i = 1; i <= BackupCount; i++)
            {
                string p = BackupPath(i);
                if (!File.Exists(p)) continue;
                string summary;
                try
                {
                    var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(p));
                    summary = $"{d.farmName}  -  Day {Mathf.Max(1, d.day)}  -  Rank {GameData.RankFromXp(d.xp)}  -  {d.coins} coins  -  {d.animals.Count} animals";
                }
                catch (System.Exception) { summary = "(this backup could not be read)"; }
                list.Add(new BackupInfo { Index = i, Time = File.GetLastWriteTime(p), Summary = summary });
            }
            return list;
        }

        /// <summary>Puts a backup back as the main save. The current save is kept aside first, so nothing is lost.</summary>
        public static bool Restore(int index)
        {
            string p = BackupPath(index);
            if (!File.Exists(p)) return false;
            try
            {
                if (Exists) File.Copy(FilePath, BeforeRestorePath, true);
                File.Copy(p, FilePath, true);
                return true;
            }
            catch (System.Exception e) { Debug.LogError("Hay Hustle: could not restore backup - " + e.Message); return false; }
        }
    }
}
