using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VulkanApp
{
    public class MyScene : Scene
    {
        public GraphicsPresenter GraphicsPresenter { get; private set; }

        public Display SceneDisplay { get; private set; }
        public GraphicsContext GraphicsContext { get; private set; }

        protected TaskCompletionSource<bool> loadedTaskCompletionSource;

        public MyScene()
            : base()
        {
            this.loadedTaskCompletionSource = new TaskCompletionSource<bool>();
        }

        protected override void CreateScene()
        {
            this.GraphicsPresenter = Application.Current.Container.Resolve<GraphicsPresenter>();
            var graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
            this.GraphicsContext = graphicsContext;

            var assetsService = Application.Current.Container.Resolve<AssetsService>();

            // Create camera
            var camera = new Entity("Camera")
                  .AddComponent(new Transform3D() { LocalPosition = new Vector3(0, 0, -3) })
                  .AddComponent(new Camera3D()
                  {
                      BackgroundColor = Color.CornflowerBlue,
                      FrameBuffer = Program.FrameBuffer,
                  });

            camera.FindComponent<Transform3D>().LocalLookAt(Vector3.Zero, Vector3.Up);
            this.Managers.EntityManager.Add(camera);

            // Create light
            Entity light = new Entity()
                        .AddComponent(new Transform3D() { LocalRotation = new Vector3(4, -4, 4) })
                        .AddComponent(new DirectionalLight()
                        {
                            Color = Color.White,
                            Intensity = 1.0f,
                        });

            this.Managers.EntityManager.Add(light);

            // Load Material
            var effect = assetsService.Load<Effect>(EvergineContent.Effects.StandardEffect);
            Material basicMaterial = new Material(effect);

            RenderLayerDescription opaqueLayerDescription = new RenderLayerDescription()
            {
                RenderState = new RenderStateDescription()
                {
                    RasterizerState = RasterizerStates.CullBack,
                    BlendState = BlendStates.Opaque,
                    DepthStencilState = DepthStencilStates.ReadWrite,
                },
            };

            StandardMaterial standardMaterial = new StandardMaterial(basicMaterial)
            {
                LayerDescription = opaqueLayerDescription,
                BaseColor = Color.Red,
                LightingEnabled = true,
            };

            Entity primitive = new Entity()
               .AddComponent(new Transform3D())
               .AddComponent(new MaterialComponent() { Material = basicMaterial })
               .AddComponent(new TeapotMesh())
               .AddComponent(new Spinner() { AxisIncrease = new Vector3(1, 2, 3) })
               .AddComponent(new MeshRenderer());

            this.Managers.EntityManager.Add(primitive);

            this.loadedTaskCompletionSource.SetResult(true);
        }        
    }
}
