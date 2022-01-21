using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Platform;

namespace VulkanApp
{
    public partial class MyApplication : Application
    {
        public MyScene Scene;
        public GraphicsContext graphicsContext;

        public MyApplication()
        {
            this.Container.RegisterType<Clock>();
            this.Container.RegisterType<TimerFactory>();
            this.Container.RegisterType<Random>();
            this.Container.RegisterType<ErrorHandler>();
            this.Container.RegisterType<ScreenContextManager>();
            this.Container.RegisterType<GraphicsPresenter>();
            this.Container.RegisterType<AssetsDirectory>();
            this.Container.RegisterType<AssetsService>();
            this.Container.RegisterType<ForegroundTaskSchedulerService>();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Get ScreenContextManager
            var screenContextManager = this.Container.Resolve<ScreenContextManager>();            

            // Navigate to scene
            this.Scene = new MyScene();
            ScreenContext screenContext = new ScreenContext(this.Scene);
            screenContextManager.To(screenContext);
        }
    }
}
