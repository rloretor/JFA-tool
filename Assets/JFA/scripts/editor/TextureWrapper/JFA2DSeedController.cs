using UnityEngine;

namespace JFA.editor
{
    public abstract class JFA2DSeedController
    {
        protected Texture2D seed;
        protected int width;
        protected int height;
        public Texture2D Seed => seed;

        public abstract void DrawProperties();

        public virtual void PaintSeeds()
        {
            seed = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                name = "Seed",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };
            seed.filterMode = FilterMode.Point;
        }

        public abstract bool IsTextureReady();

        public int GetMinPasses()
        {
            return Mathf.CeilToInt(Mathf.Log(Mathf.Max(width, height), 2f));
        }
    }
}
