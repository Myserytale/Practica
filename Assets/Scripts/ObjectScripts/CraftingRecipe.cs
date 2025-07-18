using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Ingredient
{
    public Item item;
    public int quantity;
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Inventory/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public List<Ingredient> ingredients;
    public Item outputItem;
    public int outputQuantity = 1;
}