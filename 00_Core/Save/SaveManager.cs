using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveManager
{
    private static string SavesDir =>
        Path.Combine(Application.persistentDataPath, "saves");

    private static string SavePath(string saveId)
        => Path.Combine(SavesDir, $"{saveId}.json");

    private static string BackupPath(string saveId)
        => Path.Combine(SavesDir, $"{saveId}.bak.json");

    private static void EnsureDir()
    {
        if (!Directory.Exists(SavesDir))
            Directory.CreateDirectory(SavesDir);
    }

    // Секреты используются для формирования ключей шифрования и подписи.
    // При изменении строк старые сохранения перестанут открываться.
    private const string AesSecret = "FarmMania_Save_AES_Secret_ChangeMe_2026";
    private const string HmacSecret = "FarmMania_Save_HMAC_Secret_ChangeMe_2026";

    private const int KeySizeBytes = 32;
    private const int IvSizeBytes = 16;

    // Уменьшено для устранения фризов в Editor
    private const int Pbkdf2Iterations = 30000;

    // Кеш ключей, чтобы PBKDF2 не выполнялся при каждом сохранении
    private static byte[] cachedAesKey;
    private static byte[] cachedHmacKey;

    // Фиксированная соль для derivation ключей
    private static readonly byte[] fixedSalt = Encoding.UTF8.GetBytes("FarmMania_FixedSalt_v1");

    [Serializable]
    private class ProtectedSaveFile
    {
        public int v;
        public string iv;
        public string data;
        public string hmac;
    }

    public static void SaveGame(string saveId, GameSaveData data)
    {
        string content = BuildProtectedFileContent(data);
        WriteAllTextAtomic(SavePath(saveId), content);
        Debug.Log($"[SaveManager] Saved (protected): {saveId}");
    }

    public static void SaveGameBackup(string saveId, GameSaveData data)
    {
        string content = BuildProtectedFileContent(data);
        WriteAllTextAtomic(BackupPath(saveId), content);
        Debug.Log($"[SaveManager] Backup saved (protected): {saveId}");
    }

    public static GameSaveData LoadGame(string saveId)
    {
        string path = SavePath(saveId);
        if (!File.Exists(path))
            return null;

        return ReadAndParse(path);
    }

    public static GameSaveData LoadBackup(string saveId)
    {
        string path = BackupPath(saveId);
        if (!File.Exists(path))
            return null;

        return ReadAndParse(path);
    }

    public static GameSaveData LoadGameOrBackup(string saveId)
    {
        try
        {
            var main = LoadGame(saveId);
            if (main != null)
                return main;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Ошибка чтения основного сохранения: {saveId}. {e.Message}");
        }

        try
        {
            var bak = LoadBackup(saveId);
            if (bak != null)
            {
                Debug.LogWarning($"[SaveManager] Загрузка из резервной копии: {saveId}");
                return bak;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Ошибка чтения резервной копии: {saveId}. {e.Message}");
        }

        return null;
    }

    public static void DeleteSave(string saveId)
    {
        string path = SavePath(saveId);
        if (File.Exists(path))
            File.Delete(path);

        string bak = BackupPath(saveId);
        if (File.Exists(bak))
            File.Delete(bak);
    }

    // Запись через временный файл для снижения риска повреждения при прерывании операции
    private static void WriteAllTextAtomic(string path, string content)
    {
        EnsureDir();

        string tmp = path + ".tmp";
        File.WriteAllText(tmp, content);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(tmp, path);
    }

    // Чтение файла с учетом совместимости со старым форматом без шифрования
    private static GameSaveData ReadAndParse(string path)
    {
        string text = File.ReadAllText(path);

        if (LooksLikeJson(text))
        {
            var maybeProtected = TryParseProtected(text);
            if (maybeProtected != null)
                return DecryptAndVerify(maybeProtected);

            return JsonConvert.DeserializeObject<GameSaveData>(text);
        }

        var protectedFile = JsonConvert.DeserializeObject<ProtectedSaveFile>(text);
        return DecryptAndVerify(protectedFile);
    }

    private static bool LooksLikeJson(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c))
                continue;
            return c == '{' || c == '[';
        }

        return false;
    }

    private static ProtectedSaveFile TryParseProtected(string text)
    {
        try
        {
            var psf = JsonConvert.DeserializeObject<ProtectedSaveFile>(text);
            if (psf == null)
                return null;

            if (psf.v <= 0)
                return null;

            if (string.IsNullOrEmpty(psf.data) ||
                string.IsNullOrEmpty(psf.iv) ||
                string.IsNullOrEmpty(psf.hmac))
                return null;

            return psf;
        }
        catch
        {
            return null;
        }
    }

    // Формирование защищенного файла: шифрование и расчет подписи
    private static string BuildProtectedFileContent(GameSaveData data)
    {
        EnsureKeys();

        string json = JsonConvert.SerializeObject(data, Formatting.None);

        byte[] iv = new byte[IvSizeBytes];
        RandomNumberGenerator.Fill(iv);

        byte[] plainBytes = Encoding.UTF8.GetBytes(json);
        byte[] cipherBytes = EncryptAesCbcPkcs7(plainBytes, cachedAesKey, iv);

        string ivB64 = Convert.ToBase64String(iv);
        string dataB64 = Convert.ToBase64String(cipherBytes);

        int version = 1;
        string signInput = $"{version}|{ivB64}|{dataB64}";
        string hmacB64 = ComputeHmacBase64(signInput, cachedHmacKey);

        var file = new ProtectedSaveFile
        {
            v = version,
            iv = ivB64,
            data = dataB64,
            hmac = hmacB64
        };

        return JsonConvert.SerializeObject(file, Formatting.Indented);
    }

    // Проверка подписи и расшифровка содержимого
    private static GameSaveData DecryptAndVerify(ProtectedSaveFile file)
    {
        if (file == null)
            return null;

        if (file.v != 1)
            throw new Exception("Неподдерживаемая версия сохранения");

        EnsureKeys();

        string signInput = $"{file.v}|{file.iv}|{file.data}";
        string expectedHmac = ComputeHmacBase64(signInput, cachedHmacKey);

        if (!FixedTimeEqualsBase64(expectedHmac, file.hmac))
            throw new Exception("Файл сохранения изменен или поврежден");

        byte[] iv = Convert.FromBase64String(file.iv);
        byte[] cipherBytes = Convert.FromBase64String(file.data);

        byte[] plainBytes = DecryptAesCbcPkcs7(cipherBytes, cachedAesKey, iv);
        string json = Encoding.UTF8.GetString(plainBytes);

        return JsonConvert.DeserializeObject<GameSaveData>(json);
    }

    // Инициализация ключей один раз на запуск игры
    private static void EnsureKeys()
    {
        if (cachedAesKey != null && cachedHmacKey != null)
            return;

        cachedAesKey = DeriveKey(AesSecret, fixedSalt);
        cachedHmacKey = DeriveKey(HmacSecret, fixedSalt);
    }

    // Получение ключа на основе секрета и соли
    private static byte[] DeriveKey(string secret, byte[] salt)
    {
        using var kdf = new Rfc2898DeriveBytes(secret, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        return kdf.GetBytes(KeySizeBytes);
    }

    private static byte[] EncryptAesCbcPkcs7(byte[] plain, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var enc = aes.CreateEncryptor();
        return enc.TransformFinalBlock(plain, 0, plain.Length);
    }

    private static byte[] DecryptAesCbcPkcs7(byte[] cipher, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(cipher, 0, cipher.Length);
    }

    private static string ComputeHmacBase64(string input, byte[] key)
    {
        using var h = new HMACSHA256(key);
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(h.ComputeHash(bytes));
    }

    // Сравнение подписи в постоянное время
    private static bool FixedTimeEqualsBase64(string aB64, string bB64)
    {
        if (string.IsNullOrEmpty(aB64) || string.IsNullOrEmpty(bB64))
            return false;

        byte[] a;
        byte[] b;

        try
        {
            a = Convert.FromBase64String(aB64);
            b = Convert.FromBase64String(bB64);
        }
        catch
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
