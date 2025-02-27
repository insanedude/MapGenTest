using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "TerrainData", menuName = "Scriptable Objects/TerrainData")]
    public class TerrainData : UpdatableData
    {
        public float mapScale = 1f;

        public bool useFalloff;
        public bool useFlatShading;

        public float meshHeightMultiplier;
        public AnimationCurve meshHeightCurve;

        public float minHeight
        {
            get
            {
                return mapScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
            }
        }
    
        public float maxHeight
        {
            get
            {
                return mapScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
            }
        }

    }
}
