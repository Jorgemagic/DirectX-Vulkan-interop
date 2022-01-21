using Evergine.Common.Graphics;
using Evergine.DirectX11;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using System;
using System.Diagnostics;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace DX11App
{
    public class Program
    {
        static SwapChain SwapChain;

        static void Main(string[] args)
        {
            // Create app
            MyApplication application = new MyApplication();

            // Create windows
            uint width = 1280;
            uint height = 720;
            WindowsSystem windowsSystem = new Evergine.Forms.FormsWindowsSystem();
            application.Container.RegisterInstance(windowsSystem);
            var window = windowsSystem.CreateWindow("DX11 App", width, height);

            // Configure Graphics Context
            var dx11GraphicsContext = CreateDX11Context(window);

            application.Container.RegisterInstance(dx11GraphicsContext);

            ID3D11Texture2D renderTarget = null;

            Stopwatch clockTimer = Stopwatch.StartNew();
            windowsSystem.Run(
            () =>
            {
                var renderTargetDescription = new Texture2DDescription
                {
                    CpuAccessFlags = CpuAccessFlags.None,
                    Width = (int)width,
                    Height = (int)height,
                    Usage = Vortice.Direct3D11.ResourceUsage.Default,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                    ArraySize = 1,
                    BindFlags = BindFlags.RenderTarget,
                    OptionFlags = ResourceOptionFlags.Shared,//// | ResourceOptionFlags.SharedKeyedMutex,
                    MipLevels = 1,
                    SampleDescription = new SampleDescription(1, 0),
                };

                // Create texture
                renderTarget = ((DX11GraphicsContext)dx11GraphicsContext).DXDevice.CreateTexture2D(renderTargetDescription);

                // Get share handle
                var resource = renderTarget.QueryInterface<IDXGIResource>();
                var sharedHandle = resource.SharedHandle;
                Console.WriteLine("DX11 Shared Texture handle: 0x{0:X16}", sharedHandle);
                resource.Release();
            },
            () =>
            {
                var gameTime = clockTimer.Elapsed;
                clockTimer.Restart();

               
                ((DX11GraphicsContext)dx11GraphicsContext).DXDeviceContext.CopyResource(((DX11Texture)SwapChain.FrameBuffer.ColorTargets[0].Texture).NativeTexture, renderTarget);

                SwapChain.Present();               
            });
        }

        private static GraphicsContext CreateDX11Context(Window window)
        {
            GraphicsContext graphicsContext = new DX11GraphicsContext();
            graphicsContext.CreateDevice(new ValidationLayer(ValidationLayer.NotifyMethod.Exceptions));
            Evergine.Common.Graphics.SwapChainDescription swapChainDescription = new Evergine.Common.Graphics.SwapChainDescription()
            {
                SurfaceInfo = window.SurfaceInfo,
                Width = window.Width,
                Height = window.Height,
                ColorTargetFormat = PixelFormat.B8G8R8A8_UNorm,
                ColorTargetFlags = TextureFlags.RenderTarget | TextureFlags.ShaderResource,
                DepthStencilTargetFormat = PixelFormat.D24_UNorm_S8_UInt,
                DepthStencilTargetFlags = TextureFlags.DepthStencil,
                SampleCount = TextureSampleCount.None,
                IsWindowed = true,
                RefreshRate = 60
            };
            SwapChain = graphicsContext.CreateSwapChain(swapChainDescription);
            SwapChain.VerticalSync = true;

            var graphicsPresenter = Application.Current.Container.Resolve<GraphicsPresenter>();
            var firstDisplay = new Display(window, SwapChain);
            graphicsPresenter.AddDisplay("DefaultDisplay", firstDisplay);

            return graphicsContext;
        }
    }
}
