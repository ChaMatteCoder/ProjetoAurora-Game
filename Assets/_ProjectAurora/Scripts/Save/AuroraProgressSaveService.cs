using System;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class AuroraProgressSaveService
{
    public const string DefaultFileName = "aurora_progress.json";

    private readonly string savePath;
    private readonly string backupPath;
    private readonly bool devSession;

    public AuroraProgressSaveData Data { get; private set; }
    public string SavePath => savePath;
    public string BackupPath => backupPath;
    public string CorruptedBackupPath { get; private set; }
    public bool LastLoadRecoveredFromBackup { get; private set; }
    public bool LastLoadUsedDefaults { get; private set; }

    /// SESSAO DE DESENVOLVIMENTO (Editor + caminho padrao):
    /// O Editor e a build compartilham o mesmo persistentDataPath no Windows, entao
    /// testar no Editor ate 999 moedas / todos os DataFiles contaminava o save que a
    /// build depois lia — a build abria maximizada e sem nenhum DataFile visivel.
    ///
    /// Nesta sessao o servico passa a:
    ///   - zerar moedas e DataFiles a cada Play (corrida sempre "limpa" para teste);
    ///   - NUNCA escrever no arquivo (a build mantem o progresso dela intacto).
    ///
    /// O gate exige caminho PADRAO de proposito: os testes de Editor instanciam o
    /// servico com caminho customizado e dependem de Load/Save reais — eles seguem
    /// com o comportamento normal.
    public AuroraProgressSaveService(string customSavePath = null)
    {
        bool usingDefaultPath = string.IsNullOrWhiteSpace(customSavePath);
        savePath = usingDefaultPath
            ? Path.Combine(Application.persistentDataPath, DefaultFileName)
            : customSavePath;
        backupPath = savePath + ".bak";
        devSession = usingDefaultPath && Application.isEditor;
    }

    public AuroraProgressSaveData Load()
    {
        LastLoadRecoveredFromBackup = false;
        LastLoadUsedDefaults = false;
        CorruptedBackupPath = null;

        if (!File.Exists(savePath))
        {
            Data = CreateDefault();
            LastLoadUsedDefaults = true;
            return ApplyDevSessionReset(Data);
        }

        try
        {
            Data = ReadData(savePath);
            return ApplyDevSessionReset(Data);
        }
        catch (Exception exception)
        {
            PreserveCorruptedSave();
            Debug.LogWarning("[AuroraSave] Save principal invalido. Tentando backup. " + exception.Message);
        }

        if (File.Exists(backupPath))
        {
            try
            {
                Data = ReadData(backupPath);
                LastLoadRecoveredFromBackup = true;
                if (!devSession)
                {
                    WriteAtomic(Data, false);
                }
                return ApplyDevSessionReset(Data);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AuroraSave] Backup invalido. Usando progresso padrao. " + exception.Message);
            }
        }

        Data = CreateDefault();
        LastLoadUsedDefaults = true;
        if (!devSession)
        {
            WriteAtomic(Data, false);
        }
        return ApplyDevSessionReset(Data);
    }

    /// No Editor (caminho padrao), zera moedas e DataFiles em memoria a cada Load.
    /// Nao toca em skins/selecao — o pedido foi especificamente moedas e DataFiles.
    private AuroraProgressSaveData ApplyDevSessionReset(AuroraProgressSaveData data)
    {
        if (!devSession || data == null)
        {
            return data;
        }

        data.auroraCoins = 0;
        data.unlockedDataFiles?.Clear();
        data.Sanitize();
        return data;
    }

    public bool Save(AuroraProgressSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("[AuroraSave] Tentativa de salvar dados nulos.");
            return false;
        }

        data.Sanitize();
        Data = data;

        if (devSession)
        {
            // Dev: mantem o progresso apenas em memoria durante o Play. Escrever aqui
            // contaminaria o save que a build le (era a causa da build abrir com 999
            // moedas e sem DataFiles).
            return true;
        }

        return WriteAtomic(data, true);
    }

    /// True quando rodando no Editor com o save padrao: progresso nao persiste e
    /// moedas/DataFiles sao zerados a cada Play. Falso em build e nos testes
    /// (que usam caminho customizado).
    public bool IsDevSession => devSession;

    public bool ResetTestEconomyData()
    {
        AuroraProgressSaveData data = Data ?? Load();
        data.auroraCoins = 0;
        data.unlockedSkins.Remove("Skin_Test_01");
        data.unlockedDataFiles.Remove("DataFile_Test_01");
        return Save(data);
    }

    private static AuroraProgressSaveData CreateDefault()
    {
        var data = new AuroraProgressSaveData();
        data.Sanitize();
        return data;
    }

    private static AuroraProgressSaveData ReadData(string path)
    {
        string json = File.ReadAllText(path, Encoding.UTF8);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("Arquivo vazio.");
        }

        AuroraProgressSaveData data = JsonUtility.FromJson<AuroraProgressSaveData>(json);
        if (data == null)
        {
            throw new InvalidDataException("JSON sem dados de progresso.");
        }

        data.Sanitize();
        return data;
    }

    private bool WriteAtomic(AuroraProgressSaveData data, bool rotateBackup)
    {
        string directory = Path.GetDirectoryName(savePath);
        string tempPath = savePath + ".tmp";

        try
        {
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
            if (!File.Exists(savePath))
            {
                File.Move(tempPath, savePath);
                return true;
            }

            if (!rotateBackup)
            {
                File.Copy(tempPath, savePath, true);
                File.Delete(tempPath);
                return true;
            }

            try
            {
                File.Replace(tempPath, savePath, backupPath, true);
            }
            catch (Exception replaceException) when (
                replaceException is IOException ||
                replaceException is PlatformNotSupportedException ||
                replaceException is UnauthorizedAccessException)
            {
                File.Copy(savePath, backupPath, true);
                File.Copy(tempPath, savePath, true);
                File.Delete(tempPath);
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("[AuroraSave] Falha ao salvar progresso: " + exception.Message);
            TryDeleteTemp(tempPath);
            return false;
        }
    }

    private void PreserveCorruptedSave()
    {
        try
        {
            string directory = Path.GetDirectoryName(savePath) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(savePath);
            string extension = Path.GetExtension(savePath);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
            CorruptedBackupPath = Path.Combine(directory, name + ".corrupt-" + stamp + extension);
            File.Copy(savePath, CorruptedBackupPath, true);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[AuroraSave] Nao foi possivel preservar o save corrompido: " + exception.Message);
        }
    }

    private static void TryDeleteTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch
        {
            // Best effort cleanup only.
        }
    }
}
