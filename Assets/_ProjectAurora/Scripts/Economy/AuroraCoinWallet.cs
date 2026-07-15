using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class AuroraCoinWallet : MonoBehaviour
{
    public const int MaxBalance = 999;

    private static AuroraCoinWallet instance;

    [SerializeField, Range(0.05f, 1f)] private float saveDebounceSeconds = 0.2f;

    private AuroraProgressSaveService saveService;
    private AuroraProgressSaveData progress;
    private Coroutine pendingSave;
    private int balance;

    public static AuroraCoinWallet Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AuroraCoinWallet>(FindObjectsInactive.Include);
            }

            return instance;
        }
    }
    public int Balance => balance;
    public string SelectedSkinId
    {
        get
        {
            EnsureProgressLoaded();
            return progress.selectedSkinId;
        }
    }

    public event Action<int> OnBalanceChanged;
    public event Action<int, int> OnCoinsAdded;
    public event Action<int, int> OnCoinsSpent;
    public event Action OnBalanceLimitReached;
    public event Action<string, AuroraPurchaseCategory> OnItemUnlocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<AuroraCoinWallet>(FindObjectsInactive.Include);
        }

        if (instance != null)
        {
            return;
        }

        var root = new GameObject("[AuroraCoinWallet]");
        instance = root.AddComponent<AuroraCoinWallet>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        Load();
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        instance = null;
    }

    public bool TryAddCoins(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (balance >= MaxBalance)
        {
            OnBalanceLimitReached?.Invoke();
            return false;
        }

        int added = Mathf.Min(amount, MaxBalance - balance);
        balance += added;
        SyncBalanceToProgress();
        RequestSave();
        OnCoinsAdded?.Invoke(added, balance);
        OnBalanceChanged?.Invoke(balance);

        if (balance >= MaxBalance && added < amount)
        {
            OnBalanceLimitReached?.Invoke();
        }

        return added > 0;
    }

    public bool CanAfford(int cost)
    {
        return cost >= 0 && balance >= cost;
    }

    public bool TrySpendCoins(int cost)
    {
        if (!CanAfford(cost))
        {
            return false;
        }

        if (cost == 0)
        {
            return true;
        }

        balance -= cost;
        SyncBalanceToProgress();
        RequestSave();
        OnCoinsSpent?.Invoke(cost, balance);
        OnBalanceChanged?.Invoke(balance);
        return true;
    }

    public void Load()
    {
        saveService = saveService ?? new AuroraProgressSaveService();
        progress = saveService.Load();
        balance = Mathf.Clamp(progress.auroraCoins, 0, MaxBalance);
        SyncBalanceToProgress();
        OnBalanceChanged?.Invoke(balance);
    }

    public void Save()
    {
        if (pendingSave != null && Application.isPlaying)
        {
            StopCoroutine(pendingSave);
            pendingSave = null;
        }

        saveService = saveService ?? new AuroraProgressSaveService();
        progress = progress ?? saveService.Load();
        SyncBalanceToProgress();
        saveService.Save(progress);
    }

    public bool IsUnlocked(string itemId, AuroraPurchaseCategory category)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        EnsureProgressLoaded();
        List<string> ids = GetUnlockList(category);
        return ids.Contains(itemId);
    }

    public bool TrySetSelectedSkinId(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId))
        {
            return false;
        }

        EnsureProgressLoaded();
        string normalized = skinId.Trim();
        if (string.Equals(progress.selectedSkinId, normalized, StringComparison.Ordinal))
        {
            return true;
        }

        progress.selectedSkinId = normalized;
        Save();
        return true;
    }

    internal bool TrySpendAndUnlock(string itemId, AuroraPurchaseCategory category, int cost)
    {
        if (string.IsNullOrWhiteSpace(itemId) || !CanAfford(cost) || IsUnlocked(itemId, category))
        {
            return false;
        }

        balance -= cost;
        GetUnlockList(category).Add(itemId);
        progress.Sanitize();
        SyncBalanceToProgress();
        Save();

        if (cost > 0)
        {
            OnCoinsSpent?.Invoke(cost, balance);
            OnBalanceChanged?.Invoke(balance);
        }

        OnItemUnlocked?.Invoke(itemId, category);

        return true;
    }

    internal bool TryUnlockItem(string itemId, AuroraPurchaseCategory category)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        EnsureProgressLoaded();
        string normalized = itemId.Trim();
        List<string> ids = GetUnlockList(category);
        if (ids.Contains(normalized))
        {
            return false;
        }

        ids.Add(normalized);
        progress.Sanitize();
        Save();
        OnItemUnlocked?.Invoke(normalized, category);
        return true;
    }

    internal bool SynchronizeUnlocks(
        AuroraPurchaseCategory category,
        IEnumerable<string> validIds,
        IEnumerable<string> requiredIds,
        string managedPrefix)
    {
        EnsureProgressLoaded();
        var valid = new HashSet<string>(validIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        var required = new HashSet<string>(requiredIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        List<string> ids = GetUnlockList(category);
        bool changed = false;

        for (int i = ids.Count - 1; i >= 0; i--)
        {
            bool isManaged = string.IsNullOrEmpty(managedPrefix) ||
                             ids[i].StartsWith(managedPrefix, StringComparison.Ordinal);
            if (isManaged && !valid.Contains(ids[i]))
            {
                ids.RemoveAt(i);
                changed = true;
            }
        }

        foreach (string requiredId in required)
        {
            if (valid.Contains(requiredId) && !ids.Contains(requiredId))
            {
                ids.Add(requiredId);
                changed = true;
            }
        }

        if (changed)
        {
            progress.Sanitize();
            Save();
        }

        return changed;
    }

    private void RequestSave()
    {
        if (!Application.isPlaying)
        {
            Save();
            return;
        }

        if (pendingSave != null)
        {
            StopCoroutine(pendingSave);
        }

        pendingSave = StartCoroutine(SaveAfterDelay());
    }

    private IEnumerator SaveAfterDelay()
    {
        yield return new WaitForSecondsRealtime(saveDebounceSeconds);
        pendingSave = null;
        Save();
    }

    private void EnsureProgressLoaded()
    {
        if (progress == null)
        {
            Load();
        }
    }

    private List<string> GetUnlockList(AuroraPurchaseCategory category)
    {
        return category == AuroraPurchaseCategory.Skin
            ? progress.unlockedSkins
            : progress.unlockedDataFiles;
    }

    private void SyncBalanceToProgress()
    {
        if (progress != null)
        {
            progress.auroraCoins = Mathf.Clamp(balance, 0, MaxBalance);
        }
    }

    private void HandleActiveSceneChanged(Scene previous, Scene current)
    {
        Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            Save();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Save();
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }

#if UNITY_EDITOR
    public void ConfigureForTests(AuroraProgressSaveService testSaveService)
    {
        instance = this;
        saveService = testSaveService;
        progress = saveService.Load();
        balance = progress.auroraCoins;
    }

    public void SetBalanceForTests(int value)
    {
        EnsureProgressLoaded();
        balance = Mathf.Clamp(value, 0, MaxBalance);
        SyncBalanceToProgress();
        Save();
        OnBalanceChanged?.Invoke(balance);
    }

    public static void ReleaseTestInstance(AuroraCoinWallet wallet)
    {
        if (instance == wallet)
        {
            instance = null;
        }
    }
#endif
}
