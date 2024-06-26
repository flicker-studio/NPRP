using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Non_Photorealistic_RP.Runtime
{
    /// <summary>
    ///     Commands and settings that define non-photorealistic rendering that describe how Unity renders frames.
    /// </summary>
    public class NPRenderPipeline : RenderPipeline
    {
        private readonly CameraRenderer _renderer = new();

        /// <summary>
        ///     Enable SRP batch processing
        /// </summary>
        public NPRenderPipeline()
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
        }

        ///<inheritdoc />
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
        }

        ///<inheritdoc />
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            foreach (var camera in cameras) _renderer.Render(context, camera);
        }
    }
}