using UnityEngine;
using UnityEngine.Rendering;

namespace Non_Photorealistic_RP.Runtime
{
    /// <summary>
    ///     All the render instructions will be sent here, and it will render every camera
    /// </summary>
    public partial class CameraRenderer
    {
        #if UNITY_EDITOR

        private static readonly ShaderTagId[] legacyShaderTagIds =
        {
            new("Always"),
            new("ForwardBase"),
            new("PrepassBase"),
            new("Vertex"),
            new("VertexLMRGBM"),
            new("VertexLM")
        };

        private static Material errorMaterial;

        private void DrawUnsupportedShaders()
        {
            if (errorMaterial == null)
                errorMaterial =
                    new Material(Shader.Find("Hidden/InternalErrorShader"));

            var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(_camera))
                                  {
                                      overrideMaterial = errorMaterial
                                  };

            for (var i = 1; i < legacyShaderTagIds.Length; i++)
                drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);

            var filteringSettings = FilteringSettings.defaultValue;
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        #endif
    }
}