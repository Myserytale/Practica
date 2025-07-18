// filepath: c:\Users\Reea\New folder\Practica\Assets\Scripts\CraftingButton.cs
using UnityEngine;

public class CraftingButton : MonoBehaviour
{
    public CraftingRecipe recipeToCraft;

    public void OnCraftButtonClick()
    {
        if (recipeToCraft != null)
        {
            InventoryManager.Instance.CraftItem(recipeToCraft);
        }
    }
}