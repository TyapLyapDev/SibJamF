using System;
using System.Collections;
using System.Collections.Generic;
using SibJam.Boss;
using SibJam.SecurityCameras;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace SibJam.TV
{
    // Комната с теликами: все телики — дочерние объекты, все говорят одно и то же одновременно.
    // Здесь базовое лицо комнаты и реакции «событие → фразы с лицами». Зона входа/выхода — дочерний Zone с TVZone.
    // Голос звучит из первого телика комнаты, колонки качаются у всех
    public class TVRoom : MonoBehaviour
    {
        public enum Trigger
        {
            PlayerEnter,
            PlayerExit,
            CameraBroken,
            GeneratorBroken,
            BossFightStarted,
            BossFightWon,
        }

        public enum Order
        {
            InTurn,
            Random,
        }

        [Serializable]
        public class Phrase
        {
            public VoiceLine Line;
            public TVFace Face = TVFace.Evil;
        }

        [Serializable]
        public class Reaction
        {
            [Tooltip("Для себя: что это за реакция")]
            public string Name;
            public Trigger When;
            [Tooltip("Какие камеры (для «Сломали камеру»)")]
            public SecurityCamera[] Cameras = Array.Empty<SecurityCamera>();
            [Tooltip("Какие генераторы (для «Сломали генератор»)")]
            public BossGenerator[] Generators = Array.Empty<BossGenerator>();
            [Tooltip("Каждое срабатывание — следующая фраза из списка (или случайная)")]
            public List<Phrase> Phrases = new List<Phrase>();
            public Order Order;
            [Tooltip("Фразы кончились — начать список заново. Иначе реакция замолкает")]
            public bool Loop;

            [NonSerialized] public int Played;
        }

        [SerializeField] private TVFace _idleFace = TVFace.Sleepy;
        [SerializeField] private BossFight _bossFight;
        [SerializeField] private List<Reaction> _reactions = new List<Reaction>();

        private VoiceLinePlayer _voiceLines;
        private TVScreen[] _screens = Array.Empty<TVScreen>();
        private Coroutine _talking;

        [Inject]
        public void Construct(VoiceLinePlayer voiceLines) => _voiceLines = voiceLines;

        public TVFace IdleFace => _idleFace;

        public IReadOnlyList<TVScreen> Screens => _screens;

        // Откуда звучит голос комнаты: первый телик
        public AudioSource Voice => _screens.Length > 0 ? _screens[0].Voice : null;

        private void Awake()
        {
            _screens = GetComponentsInChildren<TVScreen>(true);
            ApplyIdleFace();
        }

        private void OnEnable()
        {
            foreach (Reaction reaction in _reactions)
            {
                foreach (SecurityCamera securityCamera in reaction.Cameras)
                {
                    if (securityCamera != null)
                        securityCamera.Broken += OnCameraBroken;
                }

                foreach (BossGenerator generator in reaction.Generators)
                {
                    if (generator != null)
                        generator.Broken += OnGeneratorBroken;
                }
            }

            if (_bossFight != null)
            {
                _bossFight.Started += OnFightStarted;
                _bossFight.Won += OnFightWon;
            }
        }

        private void OnDisable()
        {
            foreach (Reaction reaction in _reactions)
            {
                foreach (SecurityCamera securityCamera in reaction.Cameras)
                {
                    if (securityCamera != null)
                        securityCamera.Broken -= OnCameraBroken;
                }

                foreach (BossGenerator generator in reaction.Generators)
                {
                    if (generator != null)
                        generator.Broken -= OnGeneratorBroken;
                }
            }

            if (_bossFight != null)
            {
                _bossFight.Started -= OnFightStarted;
                _bossFight.Won -= OnFightWon;
            }
        }

        // Базовое лицо раздаётся всем теликам комнаты (зовёт и редактор, чтобы в сцене было видно)
        public void ApplyIdleFace()
        {
            foreach (TVScreen screen in GetComponentsInChildren<TVScreen>(true))
                screen.IdleFace = _idleFace;
        }

        public void BeginTalk(TVFace face)
        {
            foreach (TVScreen screen in _screens)
                screen.BeginTalk(face);
        }

        public void ShowFace(TVFace face)
        {
            foreach (TVScreen screen in _screens)
                screen.Show(face);
        }

        public void EndTalk()
        {
            foreach (TVScreen screen in _screens)
                screen.EndTalk();
        }

        // Зовёт TVZone
        public void OnPlayerEntered() => Fire(Trigger.PlayerEnter, null);

        public void OnPlayerExited() => Fire(Trigger.PlayerExit, null);

        private void OnCameraBroken(SecurityCamera securityCamera) => Fire(Trigger.CameraBroken, securityCamera);

        private void OnGeneratorBroken(BossGenerator generator) => Fire(Trigger.GeneratorBroken, generator);

        private void OnFightStarted() => Fire(Trigger.BossFightStarted, null);

        private void OnFightWon() => Fire(Trigger.BossFightWon, null);

        private void Fire(Trigger trigger, UnityEngine.Object source)
        {
            foreach (Reaction reaction in _reactions)
            {
                if (reaction.When != trigger || !Matches(reaction, source))
                    continue;

                Phrase phrase = Next(reaction);

                if (phrase != null)
                    Say(phrase);

                return;
            }
        }

        private static bool Matches(Reaction reaction, UnityEngine.Object source)
        {
            if (source is SecurityCamera securityCamera)
                return Array.IndexOf(reaction.Cameras, securityCamera) >= 0;

            if (source is BossGenerator generator)
                return Array.IndexOf(reaction.Generators, generator) >= 0;

            return true;
        }

        private static Phrase Next(Reaction reaction)
        {
            int count = reaction.Phrases.Count;

            if (count == 0 || (!reaction.Loop && reaction.Played >= count))
                return null;

            int index = reaction.Order == Order.Random ? Random.Range(0, count) : reaction.Played % count;
            reaction.Played++;
            return reaction.Phrases[index];
        }

        public void Say(Phrase phrase)
        {
            if (phrase.Line == null || _voiceLines == null)
                return;

            if (_talking != null)
                StopCoroutine(_talking);

            _talking = StartCoroutine(Talk(phrase));
        }

        private IEnumerator Talk(Phrase phrase)
        {
            BeginTalk(phrase.Face);
            _voiceLines.Play(phrase.Line, Voice);
            yield return new WaitForSeconds(phrase.Line.Duration);
            EndTalk();
            _talking = null;
        }
    }
}
