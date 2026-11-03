using UnityEngine;

namespace SibJam.Locations
{
    // Открыть дверь вызовом без параметров — например «Действием» Open на таймлайне катсцены
    [RequireComponent(typeof(LocationDoor))]
    public class DoorOpener : MonoBehaviour
    {
        public void Open()
        {
            LocationDoor door = GetComponent<LocationDoor>();

            if (!door.IsOpen)
                door.Open();
        }
    }
}
