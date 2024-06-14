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
        ///<inheritdoc />
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
        }
        
        ///<inheritdoc />
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
        }
    }
}