using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("Setari Inventar")]
    public int maxSlots = 3;

    [Header("Iteme Echipabile")]
    public GameObject uvObject;
    public GameObject emfObject;

    private List<ItemType> items = new();

    private int currentSlot = -1;

    void Start()
    {
        DisableAllItems();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EquipSlot(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            EquipSlot(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            EquipSlot(2);
    }

    public bool AddItem(ItemType item)
    {
        if (items.Count >= maxSlots)
        {
            Debug.Log("[INVENTAR] Inventar plin!");
            return false;
        }

        items.Add(item);

        Debug.Log("[INVENTAR] Ai ridicat: " + item);

        if (currentSlot == -1)
            EquipSlot(0);

        return true;
    }

    public void EquipSlot(int slot)
    {
        if (slot < 0 || slot >= items.Count)
            return;

        currentSlot = slot;

        DisableAllItems();

        switch (items[slot])
        {
            case ItemType.UVFlashlight:
                if (uvObject != null)
                    uvObject.SetActive(true);
                break;

            case ItemType.EMFReader:
                if (emfObject != null)
                    emfObject.SetActive(true);
                break;
        }

        Debug.Log("[INVENTAR] Echipat: " + items[slot]);
    }

    void DisableAllItems()
    {
        if (uvObject != null)
            uvObject.SetActive(false);

        if (emfObject != null)
            emfObject.SetActive(false);
    }
}