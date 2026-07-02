using System;
using UnityEngine;

namespace Plummet
{
    /// <summary>
    /// One playable character: a standing pose plus the falling animation frames,
    /// and optionally a one-shot dive animation played during the trapdoor drop.
    /// </summary>
    [Serializable]
    public sealed class Skin
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite standing;
        [SerializeField] private Sprite[] fallingFrames;
        [SerializeField] private Sprite[] diveFrames;

        public Skin()
        {
        }

        public Skin(string id, string displayName, Sprite standing, Sprite[] fallingFrames)
            : this(id, displayName, standing, fallingFrames, null)
        {
        }

        public Skin(string id, string displayName, Sprite standing, Sprite[] fallingFrames, Sprite[] diveFrames)
        {
            this.id = id;
            this.displayName = displayName;
            this.standing = standing;
            this.fallingFrames = fallingFrames;
            this.diveFrames = diveFrames;
        }

        public string Id => id;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;

        public Sprite[] FallingFrames => fallingFrames;

        /// <summary>One-shot trapdoor-drop dive frames; null/empty for skins that
        /// use the procedural tip-and-recover instead.</summary>
        public Sprite[] DiveFrames => diveFrames;

        public bool HasDiveFrames
        {
            get
            {
                if (diveFrames == null)
                {
                    return false;
                }

                for (int i = 0; i < diveFrames.Length; i++)
                {
                    if (diveFrames[i] != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public Sprite Standing => standing != null ? standing : FirstFrame;

        public Sprite FirstFrame
        {
            get
            {
                if (fallingFrames != null)
                {
                    for (int i = 0; i < fallingFrames.Length; i++)
                    {
                        if (fallingFrames[i] != null)
                        {
                            return fallingFrames[i];
                        }
                    }
                }

                return standing;
            }
        }
    }
}
