using UnityEngine;

public enum AuroraPurchaseCategory
{
    Skin,
    DataFile
}

[CreateAssetMenu(fileName = "AuroraPurchasableItem", menuName = "Projeto Aurora/Economy/Purchasable Item")]
public sealed class AuroraPurchasableItem : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private AuroraPurchaseCategory category;
    [SerializeField, Min(0)] private int price;
    [SerializeField] private bool unlockedByDefault;
    [SerializeField] private Sprite preview;
    [SerializeField, TextArea(2, 5)] private string description;

    public string Id => id;
    public string DisplayName => displayName;
    public AuroraPurchaseCategory Category => category;
    public int Price => price;
    public bool UnlockedByDefault => unlockedByDefault;
    public Sprite Preview => preview;
    public string Description => description;

    private void OnValidate()
    {
        id = id == null ? string.Empty : id.Trim();
        price = Mathf.Max(0, price);
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        string itemId,
        string itemDisplayName,
        AuroraPurchaseCategory itemCategory,
        int itemPrice,
        bool itemUnlockedByDefault,
        string itemDescription)
    {
        id = itemId == null ? string.Empty : itemId.Trim();
        displayName = itemDisplayName;
        category = itemCategory;
        price = Mathf.Max(0, itemPrice);
        unlockedByDefault = itemUnlockedByDefault;
        description = itemDescription;
    }
#endif
}
