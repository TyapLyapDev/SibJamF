using SibJam.Boss;
using SibJam.SecurityCameras;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Запускает катсцену по событию в игре: сломали камеру наблюдения или генератор, начался или выигран бой с боссом, игрок вошёл в зону (коллайдер-триггер на этом объекте)
    // или кто-то вызвал Fire. Для реплик теликов: у такой катсцены выключены «играть при старте» и пропуск, игрок не замирает
    public class CutsceneTrigger : MonoBehaviour
    {
        public enum BossMoment
        {
            None,
            FightStarted,
            FightWon,
        }

        [SerializeField] private Cutscene _cutscene;
        [Tooltip("Сработать только один раз")]
        [SerializeField] private bool _once = true;
        [Tooltip("Запустить, когда сломают любую из этих камер")]
        [SerializeField] private SecurityCamera[] _cameras;
        [Tooltip("Запустить, когда игрок войдёт в коллайдер-триггер на этом объекте")]
        [SerializeField] private bool _onPlayerEnter;
        [SerializeField] private string _playerTag = "Player";
        [Tooltip("Запустить на начало или победу в бою с боссом")]
        [SerializeField] private BossFight _bossFight;
        [SerializeField] private BossMoment _bossMoment;
        [Tooltip("Запустить, когда сломают любой из этих генераторов")]
        [SerializeField] private BossGenerator[] _generators;

        private bool _fired;

        private void OnEnable()
        {
            foreach (SecurityCamera securityCamera in _cameras)
            {
                if (securityCamera != null)
                    securityCamera.Broken += OnCameraBroken;
            }

            foreach (BossGenerator generator in _generators)
            {
                if (generator != null)
                    generator.Broken += OnGeneratorBroken;
            }

            if (_bossFight == null)
                return;

            if (_bossMoment == BossMoment.FightStarted)
                _bossFight.Started += Fire;
            else if (_bossMoment == BossMoment.FightWon)
                _bossFight.Won += Fire;
        }

        private void OnDisable()
        {
            foreach (SecurityCamera securityCamera in _cameras)
            {
                if (securityCamera != null)
                    securityCamera.Broken -= OnCameraBroken;
            }

            foreach (BossGenerator generator in _generators)
            {
                if (generator != null)
                    generator.Broken -= OnGeneratorBroken;
            }

            if (_bossFight == null)
                return;

            _bossFight.Started -= Fire;
            _bossFight.Won -= Fire;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_onPlayerEnter && other.CompareTag(_playerTag))
                Fire();
        }

        public void Fire()
        {
            if ((_once && _fired) || _cutscene.IsPlaying)
                return;

            _fired = true;
            _cutscene.Play();
        }

        private void OnCameraBroken(SecurityCamera securityCamera) => Fire();

        private void OnGeneratorBroken(BossGenerator generator) => Fire();
    }
}
