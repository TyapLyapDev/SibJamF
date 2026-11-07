using System;
using UnityEngine;

namespace SibJam.Locations
{
    // Зона прохода. Entered шлётся не только на вход, но и каждый физ.кадр, пока игрок внутри:
    // так зону нельзя «проскочить», если игрок появился уже в ней (возрождение) или условия
    // у слушателя выполнились, пока он тут стоял. Слушатели сами защищены от повторов
    public class PassageZone : MonoBehaviour
    {
        [SerializeField] private string _playerTag = "Player";

        public event Action Entered;

        private void OnTriggerEnter(Collider other) => Notify(other);

        private void OnTriggerStay(Collider other) => Notify(other);

        private void Notify(Collider other)
        {
            if (other.CompareTag(_playerTag))
                Entered?.Invoke();
        }
    }
}
