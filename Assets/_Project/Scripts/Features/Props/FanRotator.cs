using UnityEngine;

namespace SibJam.Props
{
    // Крутит лопасти вентилятора вокруг локальной оси, со случайным стартовым углом, чтобы соседние вентиляторы не крутились синхронно
    public class FanRotator : MonoBehaviour
    {
        [SerializeField] private Vector3 _axis = Vector3.right;
        [SerializeField] private float _speed = 360f;
        [SerializeField, Range(0f, 0.5f)] private float _speedSpread = 0.15f;
        [SerializeField] private bool _randomStartAngle = true;

        private float _currentSpeed;

        private void Start()
        {
            _currentSpeed = _speed * (1f + Random.Range(-_speedSpread, _speedSpread));

            if (_randomStartAngle)
                transform.Rotate(_axis, Random.Range(0f, 360f), Space.Self);
        }

        private void Update()
        {
            transform.Rotate(_axis, _currentSpeed * Time.deltaTime, Space.Self);
        }
    }
}
