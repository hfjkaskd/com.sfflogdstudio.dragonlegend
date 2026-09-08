using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using DragonLegend.Whitebox.Recovered;

namespace DragonLegend.Whitebox
{
    // New native Unity loader; selected snapshot path is caller/Inspector configuration.
    // All platforms use the same StreamingAssets -> UnityWebRequest -> JsonUtility pipeline.
    public sealed class ConfigSnapshotLoader
    {
        public GoldenDragonAutoGenConfig Value { get; private set; }
        public IEnumerator Load(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || relativePath.Contains(".."))
                throw new ArgumentException("A relative snapshot path is required.");
            string root=Application.streamingAssetsPath.TrimEnd('/');
            string url=root.Contains("://") ? root+"/"+relativePath : new Uri(Path.Combine(root,relativePath)).AbsoluteUri;
            using (var request=UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result!=UnityWebRequest.Result.Success) throw new IOException(request.error);
                var value=JsonUtility.FromJson<GoldenDragonAutoGenConfig>(request.downloadHandler.text);
                if (value==null || value.Rogk==null || value.Gimrol==null || value.Rgpggm==null || value.Rrggiomg==null || value.Qonrii==null || value.Qollgqr==null || value.Ronig==null)
                    throw new FormatException("The snapshot is missing required configuration sections.");
                Value=value;
            }
        }
    }
}
