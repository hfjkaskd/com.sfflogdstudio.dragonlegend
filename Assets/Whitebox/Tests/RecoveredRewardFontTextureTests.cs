using System;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;

public sealed class RecoveredRewardFontTextureTests
{
    [Test]
    public void ImportedRewardAtlasMatchesOriginalApkAlphaPayloadAndSampling()
    {
        var texture=Resources.Load<Texture2D>("RecoveredArt/Res/Font/QuorumStd-Black_zitidi.com Atlas");
        Assert.IsNotNull(texture);
        Assert.AreEqual(1024,texture.width);Assert.AreEqual(1024,texture.height);
        Assert.AreEqual(TextureFormat.Alpha8,texture.format);Assert.AreEqual(1,texture.mipmapCount);
        Assert.IsTrue(texture.isReadable);Assert.IsFalse(texture.streamingMipmaps);
        Assert.AreEqual(FilterMode.Bilinear,texture.filterMode);Assert.AreEqual(1,texture.anisoLevel);
        Assert.AreEqual(0,texture.mipMapBias);
        Assert.AreEqual(TextureWrapMode.Repeat,texture.wrapModeU);
        Assert.AreEqual(TextureWrapMode.Repeat,texture.wrapModeV);
        Assert.AreEqual(TextureWrapMode.Repeat,texture.wrapModeW);
        var bytes=texture.GetRawTextureData<byte>().ToArray();Assert.AreEqual(1048576,bytes.Length);
        // Original font bundle Texture2D path -2850944667414898811, decoded
        // image data bytes. Oracle provenance is reward-atlas-native-audit.json.
        using(var sha=SHA256.Create())
            Assert.AreEqual("2a4211a1f2d98d05cf095cdaddcac7a1ea1abb78fe40bb0ff887634585b89464",
                BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant());
    }
}
