using UnityEngine;

public class DoorController : MonoBehaviour
{
    public bool isOpen = false;
    public float openRotation = 90f; 
    public float smooth = 2f;        

    private Quaternion closedRot;
    private Quaternion openRot;

    void Start()
    {
        closedRot = transform.localRotation;
        openRot = Quaternion.Euler(0, openRotation, 0) * closedRot;
    }

    void Update()
    {
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