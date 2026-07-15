using System;
using UnityEngine;

public sealed class AuroraPurchaseService
{
    private static AuroraPurchaseService instance;

    private readonly AuroraCoinWallet wallet;

    public static AuroraPurchaseService Instance
    {
        get
        {
            if (instance == null && AuroraCoinWallet.Instance != null)
            {
                instance = new AuroraPurchaseService(AuroraCoinWallet.Instance);
            }

            return instance;
        }
    }

    public event Action<AuroraPurchasableItem> OnItemPurchased;
    public event Action<string, AuroraPurchaseCategory> OnUnlockPurchased;

    public AuroraPurchaseService(AuroraCoinWallet targetWallet)
    {
        wallet = targetWallet;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null && AuroraCoinWallet.Instance != null)
        {
            instance = new AuroraPurchaseService(AuroraCoinWallet.Instance);
        }
    }

    public bool CanPurchase(AuroraPurchasableItem item)
    {
        return item != null && CanPurchase(
            item.Id,
            item.Category,
            item.Price,
            item.UnlockedByDefault);
    }

    public bool TryPurchase(AuroraPurchasableItem item)
    {
        if (item == null || !TryPurchase(
                item.Id,
                item.Category,
                item.Price,
                item.UnlockedByDefault))
        {
            return false;
        }

        OnItemPurchased?.Invoke(item);
        return true;
    }

    public bool CanPurchase(
        string itemId,
        AuroraPurchaseCategory category,
        int price,
        bool unlockedByDefault = false)
    {
        return wallet != null &&
               !string.IsNullOrWhiteSpace(itemId) &&
               price >= 0 &&
               !unlockedByDefault &&
               !IsUnlocked(itemId, category) &&
               wallet.CanAfford(price);
    }

    public bool TryPurchase(
        string itemId,
        AuroraPurchaseCategory category,
        int price,
        bool unlockedByDefault = false)
    {
        if (!CanPurchase(itemId, category, price, unlockedByDefault) ||
            !wallet.TrySpendAndUnlock(itemId, category, price))
        {
            return false;
        }

        OnUnlockPurchased?.Invoke(itemId, category);
        return true;
    }

    public bool IsUnlocked(AuroraPurchasableItem item)
    {
        return item != null && (item.UnlockedByDefault || IsUnlocked(item.Id, item.Category));
    }

    public bool IsUnlocked(string itemId, AuroraPurchaseCategory category)
    {
        return wallet != null && wallet.IsUnlocked(itemId, category);
    }
}
