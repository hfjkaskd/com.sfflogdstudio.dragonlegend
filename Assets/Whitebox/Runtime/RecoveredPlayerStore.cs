using System;
using DragonLegend.Whitebox.Recovered;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // SavePlayerData 0x236dbe8, Save<T> 0x2633e5c, GetGameData<T> 0x2633a4c.
    public sealed class RecoveredPlayerStore
    {
        public const string OriginalKey = "playerData.d";
        private readonly string key;
        public PlayerData Data { get; private set; }
        public RecoveredPlayerStore(string saveKey = OriginalKey)
        {
            key = saveKey ?? throw new ArgumentNullException(nameof(saveKey));
        }
        public void Load(Action<int> initialized)
        {
            int status;
            try
            {
                string json = PlayerPrefs.GetString(key, string.Empty);
                if (string.IsNullOrEmpty(json)) { Data = new PlayerData(); status = 0; }
                else { Data = JsonUtility.FromJson<PlayerData>(json); status = 1; }
            }
            catch (Exception) { Data = new PlayerData(); status = 2; } // native catches System.Exception
            initialized?.Invoke(status);
            Save(); // Original InitPlayeData invokes caller before its final save.
        }
        public void Save()
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }
    }
}
