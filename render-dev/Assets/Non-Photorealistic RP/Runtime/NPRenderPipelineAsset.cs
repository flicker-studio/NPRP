using UnityEngine;
using UnityEngine.Rendering;

namespace Non_Photorealistic_RP.Runtime
{
    /// <summary>
    ///     Give Unity a way to get a hold of a pipeline object instance that is responsible for rendering.
    /// </summary>
    [CreateAssetMenu(menuName = "Rendering/Non-Photorealistic Render Pipeline")]
    public class NPRenderPipelineAsset : RenderPipelineAsset
    {
        ///<inheritdoc />
        protected override RenderPipeline CreatePipeline()
        {
            return new NPRenderPipeline();
        }
    }
}