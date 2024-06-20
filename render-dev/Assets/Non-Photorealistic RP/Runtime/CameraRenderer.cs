using UnityEngine;
using UnityEngine.Rendering;

namespace Non_Photorealistic_RP.Runtime
{
    /// <summary>
    ///     All the render instructions will be sent here, and it will render every camera
    /// </summary>
    public partial class CameraRenderer
    {
        private                 Camera                  _camera;
        private                 ScriptableRenderContext _context;
        private                 CullingResults          _cullingResults;
        private static readonly ShaderTagId             UnlitShaderTagId = new("SRPDefaultUnlit");
        private const           string                  BufferName       = "Render Camera";

        private readonly CommandBuffer _buffer = new()
                                                 {
                                                     name = BufferName
                                                 };

        /// <summary>
        ///     command a rendering to one camera
        /// </summary>
        /// <param name="context">defines state and drawing commands that custom render pipelines use</param>
        /// <param name="camera">the camera you want to render</param>
        public void Render(ScriptableRenderContext context, Camera camera)
        {
            _context = context;
            _camera  = camera;

            PrepareBuffer();
            PrepareForSceneWindow();
            Cull();
            Setup();
            DrawVisibleGeometry();
            DrawUnsupportedShaders();
            DrawGizmos();
            Submit();
        }

        private void Setup()
        {
            //hand the camera's projection matrix to the context
            _context.SetupCameraProperties(_camera);
            var flags = _camera.clearFlags;
            _buffer.ClearRenderTarget
                (
                 flags <= CameraClearFlags.Depth,
                 flags == CameraClearFlags.Color,
                 flags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear
                );
            _buffer.BeginSample(SampleName);
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }

        private void Submit()
        {
            _buffer.EndSample(SampleName);
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
            //submit a rendering request
            _context.Submit();
        }

        private void DrawVisibleGeometry()
        {
            //draw the opaque object first
            var sortingSettings = new SortingSettings(_camera)
                                  {
                                      criteria = SortingCriteria.CommonOpaque
                                  };
            var drawingSettings   = new DrawingSettings(UnlitShaderTagId, sortingSettings);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);

            //then draw the skybox
            _context.DrawSkybox(_camera);

            //finally draw transparent objects
            sortingSettings.criteria           = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings    = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        /// <summary>
        ///     camera viewport culling
        /// </summary>
        /// <returns>return true when the culling task is successfully executed</returns>
        private void Cull()
        {
            if (!_camera.TryGetCullingParameters(out var p)) return;
            _cullingResults = _context.Cull(ref p);
        }
    }
}