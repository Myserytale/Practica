using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("Item Information")]
    public string itemName = "New Item";
    [TextArea(3, 5)]
    public string description = "Item Description";
    public Sprite icon = null;
    public int maxStackSize = 64;
}