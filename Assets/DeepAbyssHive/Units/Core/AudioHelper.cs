using UnityEngine;

namespace DeepAbyssHive.Units.Core
{
    public static class AudioHelper
    {
        public static void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, Vector3.zero);
        }

        public static void PlaySound(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath)) return;
            var clip = Resources.Load<AudioClip>(resourcesPath);
            if (clip != null) PlaySound(clip);
        }
    }
}