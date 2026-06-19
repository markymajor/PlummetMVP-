using UnityEngine;

namespace Plummet
{
    /// <summary>
    /// Persists the player's chosen character across runs and app launches.
    /// </summary>
    public static class SkinSelection
    {
        private const string Key = "plummet.selectedSkin";

        public static int SelectedIndex
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(Key, 0));
            set
            {
                PlayerPrefs.SetInt(Key, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }
    }
}
