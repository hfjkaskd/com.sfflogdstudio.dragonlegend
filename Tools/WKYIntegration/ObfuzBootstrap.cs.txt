using Obfuz;
using Obfuz.EncryptionVM;
using UnityEngine;

public static class ObfuzBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize()
    {
        var key = Resources.Load<TextAsset>("Obfuz/defaultStaticSecretKey");
        if (key == null) throw new System.InvalidOperationException("Obfuz static key is missing.");
        EncryptionService<DefaultStaticEncryptionScope>.Encryptor =
            new GeneratedEncryptionVirtualMachine(key.bytes);
        Resources.UnloadAsset(key);
    }
}
