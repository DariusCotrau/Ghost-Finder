using UnityEngine;

public class DoorController : MonoBehaviour
{
    public bool isOpen = false;
    public float openRotation = 90f; // Unghiul de deschidere
    public float smooth = 2f;        // Viteza animației

    private Quaternion closedRot;
    private Quaternion openRot;

    void Start()
    {
        // Memorăm rotația inițială (închisă)
        closedRot = transform.localRotation;
        // Calculăm rotația pentru deschis
        openRot = Quaternion.Euler(0, openRotation, 0) * closedRot;
    }

    void Update()
    {
        // Animăm rotația ușii către starea dorită
        if (isOpen)
            transform.localRotation = Quaternion.Slerp(transform.localRotation, openRot, Time.deltaTime * smooth);
        else
            transform.localRotation = Quaternion.Slerp(transform.localRotation, closedRot, Time.deltaTime * smooth);
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
    }
}