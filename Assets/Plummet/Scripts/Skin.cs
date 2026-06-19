using System;
using UnityEngine;

namespace Plummet
{
    /// <summary>
    /// One playable character: a standing pose plus the falling animation frames.
    /// </summary>
    [Serializable]
    public sealed class Skin
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite standing;
        [SerializeField] private Sprite[] fallingFrames;

        public Skin()
        {
        }

        public Skin(string id, string displayName, Sprite standing, Sprite[] fallingFrames)
        {
            this.id = id;
            this.displayName = displayName;
            this.standing = standing;
            this.fallingFrames = fallingFrames;
        }

        public string Id => id;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;

        public Sprite[] FallingFrames => fallingFrames;

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
