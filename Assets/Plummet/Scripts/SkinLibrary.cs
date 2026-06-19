using System.Collections.Generic;
using UnityEngine;

namespace Plummet
{
    /// <summary>
    /// Scene-level registry of the available character skins. Populated by the
    /// editor setup tool (Plummet/Skins/Setup) so adding a kid is just dropping
    /// in art and re-running the tool.
    /// </summary>
    public sealed class SkinLibrary : MonoBehaviour
    {
        public static SkinLibrary Instance { get; private set; }

        [SerializeField] private List<Skin> skins = new List<Skin>();

        public IReadOnlyList<Skin> Skins => skins;

        public int Count => skins.Count;

        public Skin Selected => Get(SkinSelection.SelectedIndex);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public Skin Get(int index)
        {
            if (skins == null || skins.Count == 0)
            {
                return null;
            }

            return skins[Mathf.Clamp(index, 0, skins.Count - 1)];
        }

#if UNITY_EDITOR
        public void SetSkins(List<Skin> value)
        {
            skins = value ?? new List<Skin>();
        }
#endif
    }
}
