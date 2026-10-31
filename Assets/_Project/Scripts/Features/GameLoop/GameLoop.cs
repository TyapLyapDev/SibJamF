using System;
using SibJam.Locations;
using UnityEngine;

namespace SibJam
{
    public class GameLoop : MonoBehaviour
    {
        [SerializeField] private LocationLoop[] _locations;

        public event Action<LocationLoop> LocationChanged;
        public event Action Completed;

        public int CurrentLocationIndex { get; private set; }

        public LocationLoop CurrentLocationLoop => _locations[CurrentLocationIndex];

        private bool HasNext => CurrentLocationIndex + 1 < _locations.Length;

        private void OnEnable()
        {
            foreach (LocationLoop location in _locations)
                location.Completed += OnLocationCompleted;
        }

        private void OnDisable()
        {
            foreach (LocationLoop location in _locations)
                location.Completed -= OnLocationCompleted;
        }

        public void LaunchLocation(LocationLoop location)
        {
            if (location != CurrentLocationLoop) return;

            location.Launch();
        }

        private void OnLocationCompleted(LocationLoop location)
        {
            if (location != CurrentLocationLoop) return;

            if (!HasNext)
            {
                Completed?.Invoke();
                return;
            }

            CurrentLocationIndex++;
            LocationChanged?.Invoke(CurrentLocationLoop);
        }
    }
}
