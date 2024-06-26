using UnityEngine;

namespace Non_Photorealistic_RP.Tests
{
    /// <summary>
    ///     GPU instantiation test component
    /// </summary>
    [DisallowMultipleComponent]
    public class MaterialProperties : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color baseColor = Color.white;

        private static MaterialPropertyBlock _block;

        private void Awake()
        {
            baseColor = new Color(Random.value, Random.value, Random.value);
            OnValidate();
        }

        private void OnValidate()
        {
            _block ??= new MaterialPropertyBlock();
            _block.SetColor(BaseColorId, baseColor);
            GetComponent<Renderer>().SetPropertyBlock(_block);
        }
    }
}