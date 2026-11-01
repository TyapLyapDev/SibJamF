using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SibJam
{
    // Голос героя на сервисном объекте: 2D-источник для реплик без своего источника. Играть через сервис VoiceLinePlayer
    public class VoiceSource : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("_voiceSource")] private AudioSource _audioSource;

        public AudioSource AudioSource => _audioSource;

        private void Awake()
        {
            if (_audioSource == null)
                throw new InvalidOperationException(
                    $"[VoiceSource] AudioSource не назначен на '{name}'. Назначьте ссылку в инспекторе.");
        }

        private void OnValidate()
        {
            if (_audioSource == null)
                Debug.LogError($"[VoiceSource] AudioSource не назначен на '{name}'.", this);
        }
    }
}
