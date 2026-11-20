using System;
using UnityEngine;

namespace SlotGame.Audio
{
    /// <summary>
    /// Audio manager for slot sound effects and audio feedback.
    /// Handles one-shot sound effects, spin loops, and volume mute toggling.
    /// </summary>
    public class SlotAudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _loopSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _buttonClickClip;
        [SerializeField] private AudioClip _reelSpinLoopClip;
        [SerializeField] private AudioClip _reelStopClip;
        [SerializeField] private AudioClip _winChimeClip;
        [SerializeField] private AudioClip _lossSoundClip;
        [SerializeField] private AudioClip _jackpotFanfareClip;

        private const string MUTE_PREF_KEY = "SlotGame_IsMuted";
        private bool _isMuted;

        public event Action<bool> OnMuteStateChanged;
        public bool IsMuted
        {
            get { return _isMuted; }
        }

        private void Awake()
        {
            _isMuted = PlayerPrefs.GetInt(MUTE_PREF_KEY, 0) == 1;
            ApplyMuteState();
        }

        public void Initialize(AudioSource sfxSource, AudioSource loopSource,
            AudioClip click, AudioClip loop, AudioClip stop, AudioClip win, AudioClip loss, AudioClip jackpot)
        {
            _sfxSource = sfxSource;
            _loopSource = loopSource;
            _buttonClickClip = click;
            _reelSpinLoopClip = loop;
            _reelStopClip = stop;
            _winChimeClip = win;
            _lossSoundClip = loss;
            _jackpotFanfareClip = jackpot;
            ApplyMuteState();
        }

        public void PlayButtonClick()
        {
            PlayOneShot(_buttonClickClip, 0.8f);
        }

        public void PlayReelSpinLoop()
        {
            if (_loopSource == null || _reelSpinLoopClip == null) return;
            _loopSource.clip = _reelSpinLoopClip;
            _loopSource.loop = true;
            _loopSource.mute = _isMuted;
            if (!_loopSource.isPlaying)
            {
                _loopSource.Play();
            }
        }

        public void StopReelSpinLoop()
        {
            if (_loopSource != null && _loopSource.isPlaying)
            {
                _loopSource.Stop();
            }
        }

        public void PlayReelStop()
        {
            PlayOneShot(_reelStopClip, 0.9f);
        }

        public void PlayWinChime()
        {
            PlayOneShot(_winChimeClip, 1.0f);
        }

        public void PlayLossSound()
        {
            PlayOneShot(_lossSoundClip, 0.7f);
        }

        public void PlayJackpotFanfare()
        {
            PlayOneShot(_jackpotFanfareClip, 1.0f);
        }

        public void ToggleMute()
        {
            SetMuted(!_isMuted);
        }

        public void SetMuted(bool mute)
        {
            _isMuted = mute;
            PlayerPrefs.SetInt(MUTE_PREF_KEY, _isMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMuteState();
            if (OnMuteStateChanged != null)
            {
                OnMuteStateChanged(_isMuted);
            }
        }

        private void ApplyMuteState()
        {
            if (_sfxSource != null) _sfxSource.mute = _isMuted;
            if (_loopSource != null) _loopSource.mute = _isMuted;
        }

        private void PlayOneShot(AudioClip clip, float volumeScale)
        {
            if (_isMuted || clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip, volumeScale);
        }
    }
}
