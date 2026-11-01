using System;
using UnityEngine;

namespace SibJam
{
    // Сервис реплик: играет озвучку и показывает субтитр. Новая реплика обрывает предыдущую
    public class VoiceLinePlayer
    {
        private readonly VoiceSource _voice;
        private readonly Subtitles _subtitles;
        private AudioSource _currentSource;

        public VoiceLinePlayer(VoiceSource voice, Subtitles subtitles)
        {
            _voice = voice ?? throw new ArgumentNullException(nameof(voice));
            _subtitles = subtitles ?? throw new ArgumentNullException(nameof(subtitles));
        }

        // source — откуда звучит реплика (например, 3D-источник у врага). Пусто — голос героя
        public void Play(VoiceLine line, AudioSource source = null)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            StopVoice();

            if (line.Clip != null)
            {
                _currentSource = source != null ? source : _voice.AudioSource;
                _currentSource.PlayOneShot(line.Clip);
            }

            if (string.IsNullOrEmpty(line.Text))
                _subtitles.Hide();
            else
                _subtitles.Show(line.Speaker, line.Text, line.Duration, line.Animation);
        }

        public void Stop()
        {
            StopVoice();
            _subtitles.Hide();
        }

        private void StopVoice()
        {
            if (_currentSource != null)
                _currentSource.Stop();

            _currentSource = null;
        }
    }
}
