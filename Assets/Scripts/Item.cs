using UnityEngine;

public enum ItemType { Resource, Tool, Placeable }

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("Item Information")]
    public ItemType itemType = ItemType.Resource;
    public string itemName = "New Item";
    [TextArea(3, 5)]
    public string description = "Item Description";
    public Sprite icon = null;
    public int maxStackSize = 64;

    [Header("Placeable Settings")]
    public GameObject objectPrefab; // The prefab to instantiate when placed
}