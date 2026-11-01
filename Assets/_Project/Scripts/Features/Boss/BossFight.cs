using System;
using System.Collections;
using SibJam.Credits;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SibJam.Boss
{
    // Ход боя: старт, когда игрок заходит в триггер на входе. Игрок бьёт босса, пока тот не упадёт на колени —
    // тогда открываются целые генераторы. Снял генератору половину запаса (или вышло время) — генераторы закрываются, босс встаёт.
    // Сломанный генератор повышает фазу; все сломаны — босс падает, затем титры.
    // После смерти игрока система здоровья вызывает ResetFight и ставит игрока в RespawnPoint
    public class BossFight : MonoBehaviour
    {
        [SerializeField] private BossController _boss;
        [SerializeField] private BossGenerator[] _generators;
        [SerializeField] private GameObject _entranceBarrier;
        [SerializeField] private Transform _respawnPoint;
        [SerializeField] private string _playerTag = "Player";

        [Header("Оглушение")]
        [Tooltip("Сколько секунд босс стоит на коленях, если игрок не добрался до генератора")]
        [SerializeField, Min(1f)] private float _staggerWindow = 12f;

        [Header("Интерфейс")]
        [SerializeField] private CanvasGroup _hud;
        [SerializeField] private TMP_Text _counter;
        [SerializeField] private Image _staggerBar;
        [SerializeField] private TMP_Text _hint;
        [SerializeField] private CreditsView _credits;
        [SerializeField, Min(0f)] private float _creditsDelay = 4f;

        private Coroutine _window;
        private int _broken;
        private Music _music;

        public event Action Started;
        public event Action Won;

        public bool IsRunning { get; private set; }
        public bool IsWon { get; private set; }
        public Transform RespawnPoint => _respawnPoint;

        [Inject]
        public void Construct(Music music)
        {
            _music = music;
        }

        private void Awake()
        {
            _entranceBarrier.SetActive(false);
            _hud.alpha = 0f;
            SetHint(null);
        }

        private void OnEnable()
        {
            _boss.Staggered += OnBossStaggered;
            _boss.StaggerChanged += OnStaggerChanged;

            foreach (BossGenerator generator in _generators)
            {
                generator.HalfLost += OnGeneratorHalfLost;
                generator.Broken += OnGeneratorBroken;
            }
        }

        private void OnDisable()
        {
            _boss.Staggered -= OnBossStaggered;
            _boss.StaggerChanged -= OnStaggerChanged;

            foreach (BossGenerator generator in _generators)
            {
                generator.HalfLost -= OnGeneratorHalfLost;
                generator.Broken -= OnGeneratorBroken;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsRunning || IsWon || !other.CompareTag(_playerTag))
                return;

            StartFight();
        }

        public void StartFight()
        {
            IsRunning = true;
            _entranceBarrier.SetActive(true);
            _hud.alpha = 1f;
            RefreshCounter();
            OnStaggerChanged(0f);
            _boss.Activate();
            _music?.PlayBoss();
            Started?.Invoke();
        }

        // Вызывается системой здоровья после смерти игрока: всё как до входа на арену, бой начнётся снова у входа
        public void ResetFight()
        {
            IsRunning = false;
            StopWindow();
            _broken = 0;
            _boss.ResetBoss();

            foreach (BossGenerator generator in _generators)
                generator.ResetGenerator();

            _entranceBarrier.SetActive(false);
            _hud.alpha = 0f;
            SetHint(null);
            _music?.PlayGame();
        }

        private void OnBossStaggered()
        {
            if (!IsRunning)
                return;

            foreach (BossGenerator generator in _generators)
                generator.Open();

            StopWindow();
            _window = StartCoroutine(Window());
        }

        private IEnumerator Window()
        {
            SetHint("БОСС ОГЛУШЁН — ЛОМАЙ ГЕНЕРАТОР");
            yield return new WaitForSeconds(_staggerWindow);
            _window = null;
            EndWindow();
        }

        private void OnGeneratorHalfLost(BossGenerator generator) => EndWindow();

        private void OnGeneratorBroken(BossGenerator generator)
        {
            _broken++;
            RefreshCounter();

            if (_broken >= _generators.Length)
            {
                StopWindow();
                StartCoroutine(Win());
                return;
            }

            _boss.SetPhase(_broken);
            EndWindow();
        }

        // Генераторы закрываются, босс встаёт с колен
        private void EndWindow()
        {
            if (!IsRunning)
                return;

            StopWindow();
            SetHint(null);

            foreach (BossGenerator generator in _generators)
                generator.Close();

            _boss.Recover();
        }

        private void StopWindow()
        {
            if (_window != null)
                StopCoroutine(_window);

            _window = null;
        }

        private IEnumerator Win()
        {
            IsRunning = false;
            IsWon = true;
            SetHint(null);
            _boss.Die();
            _entranceBarrier.SetActive(false);
            Won?.Invoke();
            yield return new WaitForSeconds(_creditsDelay);
            _hud.alpha = 0f;
            _credits.Show();
        }

        private void OnStaggerChanged(float value)
        {
            if (_staggerBar != null)
                _staggerBar.fillAmount = value;
        }

        private void SetHint(string text)
        {
            if (_hint == null)
                return;

            _hint.text = text ?? string.Empty;
            _hint.enabled = !string.IsNullOrEmpty(text);
        }

        private void RefreshCounter() => _counter.text = $"ГЕНЕРАТОРЫ: {_broken} / {_generators.Length}";
    }
}
