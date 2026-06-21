using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public TextMeshProUGUI slot1text;
    public TextMeshProUGUI slot2text;
    public TextMeshProUGUI slot3text;

    public void UpdateUI(PlayerInventory inventory)
    {
        slot1text.text = "";
        slot2text.text = "";
        slot3text.text = "";

        if (inventory.ItemCount > 0)
            slot1text.text = inventory.GetItemName(0);

        if (inventory.ItemCount > 1)
            slot2text.text = inventory.GetItemName(1);

        if (inventory.ItemCount > 2)
            slot3text   .text = inventory.GetItemName(2);
    }
}