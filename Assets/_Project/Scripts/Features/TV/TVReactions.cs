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
    // Всё, что говорит телик, в одном месте: базовое лицо и список реакций «событие → фразы с лицами».
    // Зона входа/выхода — коллайдер-триггер с TVZone (обычно дочерний объект Zone)
    [RequireComponent(typeof(TVScreen))]
    public class TVReactions : MonoBehaviour
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

        [SerializeField] private TVScreen _screen;
        [SerializeField] private BossFight _bossFight;
        [SerializeField] private List<Reaction> _reactions = new List<Reaction>();

        private VoiceLinePlayer _voiceLines;
        private Coroutine _talking;

        [Inject]
        public void Construct(VoiceLinePlayer voiceLines) => _voiceLines = voiceLines;

        public TVFace IdleFace
        {
            get => _screen.IdleFace;
            set => _screen.IdleFace = value;
        }

        private void Reset() => _screen = GetComponent<TVScreen>();

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
            _screen.BeginTalk(phrase.Face);
            _voiceLines.Play(phrase.Line, _screen.Voice);
            yield return new WaitForSeconds(phrase.Line.Duration);
            _screen.EndTalk();
            _talking = null;
        }
    }
}
