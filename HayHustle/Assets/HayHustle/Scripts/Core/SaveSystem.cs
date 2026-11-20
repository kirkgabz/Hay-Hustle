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
    }

    [System.Serializable]
    public class SaveData
    {
        public int version = 1;
        public string farmName;
        public int character;
        public int coins, water, waterMax, xp, warehouseCapacity;
        public int[] inventory;
        public float[] grass;
        public List<AnimalSave> animals = new List<AnimalSave>();
        public float timeOfDay;
        public int day;
        public int predatorsCaught, animalsLost;
        public string lastRewardDate = "";
        public int rewardStreak;
        public int wanted;
        public int mess;
        public int cats, dogs;
    }

    /// <summary>
    /// Local save file. Later this is mirrored to the Supabase backend (cloud save),
    /// see backend/supabase in the project root.
    /// </summary>
    public static class SaveSystem
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "hayhustle_save.json");

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
    }
}
