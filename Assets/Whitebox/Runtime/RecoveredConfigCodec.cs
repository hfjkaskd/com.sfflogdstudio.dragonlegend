using System;
using System.Text;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    public static class RecoveredConfigCodec
    {
        // Recovered from ConfigManager.LoadConfig 0x2369c20 and XORGoldenDragon.Decode 0x236a0a0.
        // The key is a format constant, not a game design parameter.
        public static string Decode(string encoded)
        {
            byte[] source = Convert.FromBase64String(encoded.TrimStart('\ufeff'));
            byte[] key = Encoding.UTF8.GetBytes("GoldenDragon");
            for (int i = 0; i < source.Length; i++) source[i] ^= key[i % key.Length];
            return Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(source)));
        }

        // Recovered normal path: 0x236a380. The original uses cumulative >= random,
        // including the observable zero-weight / random-zero edge case.
        // Caller supplies UnityEngine.Random.Range(0,totalWeight) in the game.
        public static int SelectAt(IReadOnlyList<int> weights, int randomValue)
        {
            float cumulative = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i];
                if (cumulative >= (float)randomValue) return i;
            }
            return -1;
        }
    }
}
