using System;
using UnityEngine;

namespace _dev
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private VcaBusHelper sfxBus;
        [SerializeField] private VcaBusHelper musicBus;
        private static SoundManager _instance;
        internal static float _sfxVolume;
        private static float _musicVolume;

        public static float SfxVolume => _sfxVolume;
        public static float MusicVolume => _musicVolume;

        public static void SetVcaBus(VcaBusType type, VcaBusHelper vcaBus)
        {
            switch (type)
            {
                case VcaBusType.Sfx:
                    _instance.sfxBus = vcaBus;
                    break;
                case VcaBusType.Music:
                    _instance.musicBus = vcaBus;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static void SetVcaBusVolume(VcaBusType type, float value)
        {
            switch (type)
            {
                case VcaBusType.Sfx:
                    _sfxVolume = value;
                    var sfxBus = _instance.sfxBus;
                    sfxBus.SetVcaVolume(value);
                    sfxBus.UpdateSlider(value);
                    break;
                case VcaBusType.Music:
                    _musicVolume = value;
                    var musicBus = _instance.musicBus;
                    musicBus.SetVcaVolume(value);
                    musicBus.UpdateSlider(value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            PopulateBusHelpers();

            _sfxVolume = sfxBus.DefaultVolume;
            _musicVolume = musicBus.DefaultVolume;

            UpdateVcaBusVolumes();
        }

        public static void UpdateVcaBusVolumes()
        {
// todo, update this when fmod integrated
            return;
            SetVcaBusVolume(VcaBusType.Sfx, _sfxVolume);
            SetVcaBusVolume(VcaBusType.Music, _musicVolume);
        }

        internal static void PopulateBusHelpers()
        {
            var busHelpers = _instance.GetComponents<VcaBusHelper>();
            foreach (var helper in busHelpers) SetVcaBus(helper.vcaBusType, helper);
        }
    }
}