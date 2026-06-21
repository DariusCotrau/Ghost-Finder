using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("Setari Inventar")]
    public int maxSlots = 3;
    public float throwForce = 4f; 
    public Transform dropPoint;   

    [Header("Setari Interactiune (Ridicat de jos)")]
    public float pickupRange = 3f; // Distanta maxima de la care poti ridica un item
    public LayerMask DefaultLayer; // Pune pe Default sau layer-ul pe care se afla obiectul aruncat

    [Header("Iteme Echipabile (In Mana Jucatorului)")]
    public GameObject uvObject;
    public GameObject emfObject;
    public GameObject motionSensorObject; 
    public GameObject crucifixObject;    

    [Header("Doar Prefab-ul pentru Motion Sensor")]
    public GameObject motionSensorPrefab; 

    private List<ItemType> items = new();
    private int currentSlot = -1;

    void Start()
    {
        DisableAllItems();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(2);

        // Cand apesi pe G, arunca senzorul
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (currentSlot != -1 && items.Count > 0 && currentSlot < items.Count)
            {
                if (items[currentSlot] == ItemType.MotionSensor)
                {
                    DropMotionSensor();
                }
            }
        }

        // Cand apesi pe E, incearca sa ridici ce ai in fata
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickupItem();
        }
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
                if (uvObject != null) uvObject.SetActive(true);
                break;
            case ItemType.EMFReader:
                if (emfObject != null) emfObject.SetActive(true);
                break;
            case ItemType.MotionSensor:
                if (motionSensorObject != null) motionSensorObject.SetActive(true);
                break;
            case ItemType.Crucifix:
                if (crucifixObject != null) crucifixObject.SetActive(true);
                break;
        }

        // ADAUGAT DIN NOU: Mesaj de debug in consola
        Debug.Log("[INVENTAR] Echipat: " + items[slot]);
    }

    void DropMotionSensor()
    {
        if (motionSensorPrefab != null)
        {
            // Tragem o rază din cameră în diagonală spre podea (în direcția privirii)
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward + (Vector3.down * 0.5f));
            RaycastHit hit;
            
            Vector3 targetPosition;
            Quaternion spawnRotation = Quaternion.identity;

            // Căutăm o podea la maximum 3 metri în fața noastră
            if (Physics.Raycast(ray, out hit, 3f))
            {
                targetPosition = hit.point;
                spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                // Dacă nu întâlnește nicio podea aproape, îl pune la nivelul picioarelor
                targetPosition = transform.position + (transform.forward * 1.2f) + (Vector3.up * 0.1f);
            }

            GameObject droppedItem = Instantiate(motionSensorPrefab, targetPosition, spawnRotation);

            Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; 
            }
        }

        items.RemoveAt(currentSlot);
        DisableAllItems();

        if (items.Count > 0)
        {
            EquipSlot(Mathf.Clamp(currentSlot - 1, 0, items.Count - 1));
        }
        else
        {
            currentSlot = -1;
        }
    }

    void TryPickupItem()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            MotionSensor sensor = hit.collider.GetComponent<MotionSensor>();
            
            if (sensor != null)
            {
                if (AddItem(ItemType.MotionSensor))
                {
                    Destroy(hit.collider.gameObject);
                    Debug.Log("[INVENTAR] Ai luat senzorul inapoi de pe jos!");
                }
            }
        }
    }

    void DisableAllItems()
    {
        if (uvObject != null) uvObject.SetActive(false);
        if (emfObject != null) emfObject.SetActive(false);
        if (motionSensorObject != null) motionSensorObject.SetActive(false);
        if (crucifixObject != null) crucifixObject.SetActive(false);
    }
}