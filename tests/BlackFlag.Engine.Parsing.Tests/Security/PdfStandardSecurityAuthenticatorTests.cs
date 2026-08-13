using System.Security.Cryptography;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.Security;

namespace BlackFlag.Engine.Parsing.Tests.Security;

public sealed class PdfStandardSecurityAuthenticatorTests
{
    [Fact]
    public void Revision2UserPasswordAuthenticatesAndDecryptsRc4Object()
    {
        byte[] id = "piratepdf-id"u8.ToArray();
        PdfDictionary dictionary = CreateRevision2Dictionary(
            userPassword: "crew",
            ownerPassword: "captain",
            id);

        PdfStandardSecurityAuthenticationResult result =
            PdfStandardSecurityAuthenticator.Authenticate(dictionary, id, "crew");

        Assert.True(result.IsAuthenticated);
        Assert.Equal(PdfPasswordAuthenticationKind.User, result.Kind);

        using PdfStandardSecuritySession session = result.Session!;
        byte[] encrypted = EncryptRc4Object(
            "BlackFlag"u8, dictionary, id, "crew", 17, 0);

        Assert.Equal(
            "BlackFlag"u8.ToArray(),
            session.DecryptString(encrypted, 17, 0));
    }

    [Fact]
    public void Revision2OwnerPasswordAuthenticates()
    {
        byte[] id = "owner-id"u8.ToArray();
        PdfDictionary dictionary = CreateRevision2Dictionary(
            userPassword: "reader",
            ownerPassword: "owner",
            id);

        PdfStandardSecurityAuthenticationResult result =
            PdfStandardSecurityAuthenticator.Authenticate(dictionary, id, "owner");

        Assert.Equal(PdfPasswordAuthenticationKind.Owner, result.Kind);
        Assert.True(result.Session!.Permissions.OwnerAuthenticated);
        result.Session.Dispose();
    }

    [Fact]
    public void WrongLegacyPasswordDoesNotExposeSession()
    {
        byte[] id = "wrong-id"u8.ToArray();
        PdfDictionary dictionary = CreateRevision2Dictionary("reader", "owner", id);

        PdfStandardSecurityAuthenticationResult result =
            PdfStandardSecurityAuthenticator.Authenticate(dictionary, id, "wrong");

        Assert.False(result.IsAuthenticated);
        Assert.Equal(PdfPasswordAuthenticationKind.Failed, result.Kind);
        Assert.Null(result.Session);
    }

    [Fact]
    public void Revision5UserPasswordAuthenticatesAndDecryptsAes256Stream()
    {
        const string password = "treasure";
        byte[] fileKey = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        PdfDictionary dictionary = CreateRevision5Dictionary(password, fileKey);

        PdfStandardSecurityAuthenticationResult result =
            PdfStandardSecurityAuthenticator.Authenticate(dictionary, ReadOnlyMemory<byte>.Empty, password);

        Assert.Equal(PdfPasswordAuthenticationKind.User, result.Kind);
        using PdfStandardSecuritySession session = result.Session!;

        byte[] iv = Enumerable.Range(32, 16).Select(i => (byte)i).ToArray();
        byte[] plaintext = "authenticated AES-256"u8.ToArray();
        byte[] encrypted = EncryptAesWithPrefixedIv(plaintext, fileKey, iv);

        Assert.Equal(
            plaintext,
            session.DecryptStream(encrypted, objectNumber: 1, generationNumber: 0));
    }

    [Fact]
    public void Revision5RejectsTamperedPermissionsBlock()
    {
        byte[] fileKey = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        PdfDictionary dictionary = CreateRevision5Dictionary("reader", fileKey);
        byte[] badPerms = ((PdfHexString)dictionary[new PdfName("Perms")]).Value.ToArray();
        badPerms[0] ^= 0x40;
        dictionary = Replace(dictionary, "Perms", new PdfHexString(badPerms));

        PdfStandardSecurityAuthenticationResult result =
            PdfStandardSecurityAuthenticator.Authenticate(
                dictionary, ReadOnlyMemory<byte>.Empty, "reader");

        Assert.False(result.IsAuthenticated);
        Assert.Contains("permissions", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DisposedSessionRejectsDecryption()
    {
        byte[] id = "dispose-id"u8.ToArray();
        PdfDictionary dictionary = CreateRevision2Dictionary("reader", "owner", id);
        PdfStandardSecurityAuthenticationResult result =
            PdfStandardSecurityAuthenticator.Authenticate(dictionary, id, "reader");
        PdfStandardSecuritySession session = result.Session!;
        session.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => session.DecryptString([1, 2, 3], 1, 0));
    }

    private static PdfDictionary CreateRevision2Dictionary(
        string userPassword,
        string ownerPassword,
        byte[] id)
    {
        byte[] padding =
        [
            0x28, 0xbf, 0x4e, 0x5e, 0x4e, 0x75, 0x8a, 0x41,
            0x64, 0x00, 0x4e, 0x56, 0xff, 0xfa, 0x01, 0x08,
            0x2e, 0x2e, 0x00, 0xb6, 0xd0, 0x68, 0x3e, 0x80,
            0x2f, 0x0c, 0xa9, 0xfe, 0x64, 0x53, 0x69, 0x7a
        ];

        byte[] userPadded = Pad(userPassword, padding);
        byte[] ownerPadded = Pad(ownerPassword, padding);
        byte[] ownerDigest = MD5.HashData(ownerPadded);
        byte[] ownerKey = ownerDigest[..5];
        byte[] owner = Rc4(userPadded, ownerKey);
        const int permissions = -4;

        byte[] input = new byte[32 + 32 + 4 + id.Length];
        userPadded.CopyTo(input, 0);
        owner.CopyTo(input, 32);
        BitConverter.GetBytes(permissions).CopyTo(input, 64);
        id.CopyTo(input, 68);
        byte[] fileKey = MD5.HashData(input)[..5];
        byte[] user = Rc4(padding, fileKey);

        return Dictionary(
            ("Filter", new PdfName("Standard")),
            ("V", new PdfInteger(1)),
            ("R", new PdfInteger(2)),
            ("O", new PdfHexString(owner)),
            ("U", new PdfHexString(user)),
            ("P", new PdfInteger(permissions)));
    }

    private static byte[] EncryptRc4Object(
        ReadOnlySpan<byte> plaintext,
        PdfDictionary dictionary,
        byte[] id,
        string password,
        int objectNumber,
        int generationNumber)
    {
        byte[] padding =
        [
            0x28, 0xbf, 0x4e, 0x5e, 0x4e, 0x75, 0x8a, 0x41,
            0x64, 0x00, 0x4e, 0x56, 0xff, 0xfa, 0x01, 0x08,
            0x2e, 0x2e, 0x00, 0xb6, 0xd0, 0x68, 0x3e, 0x80,
            0x2f, 0x0c, 0xa9, 0xfe, 0x64, 0x53, 0x69, 0x7a
        ];
        byte[] padded = Pad(password, padding);
        byte[] owner = ((PdfHexString)dictionary[new PdfName("O")]).Value.ToArray();
        byte[] input = new byte[32 + 32 + 4 + id.Length];
        padded.CopyTo(input, 0);
        owner.CopyTo(input, 32);
        BitConverter.GetBytes(-4).CopyTo(input, 64);
        id.CopyTo(input, 68);
        byte[] fileKey = MD5.HashData(input)[..5];

        byte[] objectSeed =
        [
            .. fileKey,
            (byte)(objectNumber & 0xff),
            (byte)((objectNumber >> 8) & 0xff),
            (byte)((objectNumber >> 16) & 0xff),
            (byte)(generationNumber & 0xff),
            (byte)((generationNumber >> 8) & 0xff)
        ];
        int objectKeyLength = Math.Min(fileKey.Length + 5, 16);
        byte[] objectKey = MD5.HashData(objectSeed)[..objectKeyLength];
        return Rc4(plaintext, objectKey);
    }

    private static PdfDictionary CreateRevision5Dictionary(
        string password,
        byte[] fileKey)
    {
        byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
        byte[] validationSalt = [1, 2, 3, 4, 5, 6, 7, 8];
        byte[] keySalt = [9, 10, 11, 12, 13, 14, 15, 16];

        byte[] validationInput = [.. passwordBytes, .. validationSalt];
        byte[] userHash = SHA256.HashData(validationInput);
        byte[] user = [.. userHash, .. validationSalt, .. keySalt];

        byte[] keyInput = [.. passwordBytes, .. keySalt];
        byte[] keyHash = SHA256.HashData(keyInput);
        byte[] ue = EncryptAesNoPadding(fileKey, keyHash, new byte[16]);

        const int permissions = -4;
        byte[] permissionsPlain =
        [
            .. BitConverter.GetBytes(permissions),
            0xff, 0xff, 0xff, 0xff,
            (byte)'T',
            (byte)'a', (byte)'d', (byte)'b',
            1, 2, 3, 4
        ];
        byte[] perms = EncryptAesEcbNoPadding(permissionsPlain, fileKey);

        byte[] owner = new byte[48];
        RandomNumberGenerator.Fill(owner);

        PdfDictionary standardCryptFilter = Dictionary(
            ("CFM", new PdfName("AESV3")));

        PdfDictionary cryptFilters = Dictionary(
            ("StdCF", standardCryptFilter));

        return Dictionary(
            ("Filter", new PdfName("Standard")),
            ("V", new PdfInteger(5)),
            ("R", new PdfInteger(5)),
            ("Length", new PdfInteger(256)),
            ("O", new PdfHexString(owner)),
            ("U", new PdfHexString(user)),
            ("OE", new PdfHexString(new byte[32])),
            ("UE", new PdfHexString(ue)),
            ("Perms", new PdfHexString(perms)),
            ("P", new PdfInteger(permissions)),
            ("CF", cryptFilters),
            ("StmF", new PdfName("StdCF")),
            ("StrF", new PdfName("StdCF")));
    }

    private static byte[] EncryptAesWithPrefixedIv(
        byte[] plaintext,
        byte[] key,
        byte[] iv)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] body = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        return [.. iv, .. body];
    }

    private static byte[] EncryptAesNoPadding(
        byte[] plaintext,
        byte[] key,
        byte[] iv)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    private static byte[] EncryptAesEcbNoPadding(byte[] plaintext, byte[] key)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    private static byte[] Pad(string password, byte[] padding)
    {
        byte[] source = System.Text.Encoding.Latin1.GetBytes(password);
        byte[] result = new byte[32];
        int count = Math.Min(source.Length, 32);
        source.AsSpan(0, count).CopyTo(result);
        padding.AsSpan(0, 32 - count).CopyTo(result.AsSpan(count));
        return result;
    }

    private static byte[] Rc4(ReadOnlySpan<byte> input, ReadOnlySpan<byte> key)
    {
        byte[] state = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        int j = 0;
        for (int i = 0; i < 256; i++)
        {
            j = (j + state[i] + key[i % key.Length]) & 255;
            (state[i], state[j]) = (state[j], state[i]);
        }

        byte[] output = new byte[input.Length];
        int x = 0;
        j = 0;
        for (int offset = 0; offset < input.Length; offset++)
        {
            x = (x + 1) & 255;
            j = (j + state[x]) & 255;
            (state[x], state[j]) = (state[j], state[x]);
            output[offset] = (byte)(input[offset] ^ state[(state[x] + state[j]) & 255]);
        }

        return output;
    }

    private static PdfDictionary Replace(
        PdfDictionary dictionary,
        string key,
        PdfObject value) =>
        new(dictionary.Select(pair =>
            pair.Key == new PdfName(key)
                ? new KeyValuePair<PdfName, PdfObject>(pair.Key, value)
                : pair));

    private static PdfDictionary Dictionary(
        params (string Key, PdfObject Value)[] entries) =>
        new(entries.Select(item =>
            new KeyValuePair<PdfName, PdfObject>(
                new PdfName(item.Key), item.Value)));
}