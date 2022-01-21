using Evergine.Common.Graphics;
using Evergine.DirectX11;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Platform;

namespace DX11App
{
    public partial class MyApplication : Application
    {

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
    }
}
