using Evergine.Common.Graphics;
using Evergine.Vulkan;
using System;
using System.Diagnostics;
using WaveEngine.Bindings.Vulkan;

namespace VulkanApp
{
    public class Program
    {
        public static FrameBuffer FrameBuffer;

        static void Main(string[] args)
        {
            // Create app
            MyApplication application = new MyApplication();

            // Create windows
            uint width = 1280;
            uint height = 720;
            WindowsSystem windowsSystem = new Evergine.Forms.FormsWindowsSystem();
            application.Container.RegisterInstance(windowsSystem);
            var window = windowsSystem.CreateWindow("Vulkan Render", width, height);

            // Configure Graphics Context
            var vulkanGraphicsContext = CreateVulkanContext();            
            IntPtr surfaceHandle = new IntPtr(Convert.ToInt32(args[0], 16));
            CreateSharedTexture(surfaceHandle, vulkanGraphicsContext);

            application.Container.RegisterInstance(vulkanGraphicsContext);

            Stopwatch clockTimer = Stopwatch.StartNew();
            windowsSystem.Run(
            () =>
            {
                application.Initialize();
            },
            () =>
            {
                var gameTime = clockTimer.Elapsed;
                clockTimer.Restart();

                application.UpdateFrame(gameTime);
                application.DrawFrame(gameTime);
            });
        }

        private static GraphicsContext CreateVulkanContext()
        {
            GraphicsContext graphicsContext = new VKGraphicsContext();
            graphicsContext.CreateDevice(new ValidationLayer(ValidationLayer.NotifyMethod.Trace));

            return graphicsContext;
        }

        public unsafe static void CreateSharedTexture(IntPtr surfaceHandle, GraphicsContext graphicsContext)
        {
            uint width = 1280;
            uint height = 720;

            var vkGraphicsContext = graphicsContext as VKGraphicsContext;

            bool dedicateMemoryExtension = true; //extProperties.externalMemoryFeatures & VK_EXTERNAL_MEMORY_FEATURE_DEDICATED_ONLY_BIT_NV

            // Creating the Vulkan Import Image
            VkExternalMemoryImageCreateInfoNV extMemoryImageInfo = new VkExternalMemoryImageCreateInfoNV();
            extMemoryImageInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_EXTERNAL_MEMORY_IMAGE_CREATE_INFO_NV;
            extMemoryImageInfo.handleTypes = VkExternalMemoryHandleTypeFlagsNV.VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_IMAGE_KMT_BIT_NV;

            VkDedicatedAllocationImageCreateInfoNV dedicatedImageCreateInfo = new VkDedicatedAllocationImageCreateInfoNV();
            dedicatedImageCreateInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DEDICATED_ALLOCATION_IMAGE_CREATE_INFO_NV;
            dedicatedImageCreateInfo.dedicatedAllocation = false;

            if (dedicateMemoryExtension)
            {
                extMemoryImageInfo.pNext = &dedicatedImageCreateInfo;
                dedicatedImageCreateInfo.dedicatedAllocation = true;
            }

            VkImageCreateInfo imageCreateInfo = new VkImageCreateInfo();
            imageCreateInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO;
            imageCreateInfo.pNext = &extMemoryImageInfo;

            imageCreateInfo.imageType = VkImageType.VK_IMAGE_TYPE_2D;
            imageCreateInfo.format = VkFormat.VK_FORMAT_B8G8R8A8_UNORM;
            imageCreateInfo.extent.width = width;
            imageCreateInfo.extent.height = height;
            imageCreateInfo.extent.depth = 1;
            imageCreateInfo.mipLevels = 1;
            imageCreateInfo.arrayLayers = 1;
            imageCreateInfo.samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT;
            imageCreateInfo.tiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL;
            imageCreateInfo.usage = VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
            imageCreateInfo.flags = VkImageCreateFlags.None;
            imageCreateInfo.sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE;

            VkImage image;
            VkResult result = VulkanNative.vkCreateImage(vkGraphicsContext.VkDevice, &imageCreateInfo, null, &image);
            VKHelpers.CheckErrors(vkGraphicsContext, result);

            // Binding Memory to the Vulkan Image
            VkMemoryRequirements memoryRequirements;
            VulkanNative.vkGetImageMemoryRequirements(vkGraphicsContext.VkDevice, image, &memoryRequirements);

            var memoryType = VKHelpers.FindMemoryType(vkGraphicsContext, memoryRequirements.memoryTypeBits, VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);

            VkImportMemoryWin32HandleInfoNV importMemInfoNV = new VkImportMemoryWin32HandleInfoNV();
            importMemInfoNV.sType = VkStructureType.VK_STRUCTURE_TYPE_IMPORT_MEMORY_WIN32_HANDLE_INFO_NV;
            importMemInfoNV.pNext = null;
            importMemInfoNV.handleType = VkExternalMemoryHandleTypeFlagsNV.VK_EXTERNAL_MEMORY_HANDLE_TYPE_D3D11_IMAGE_KMT_BIT_NV;
            importMemInfoNV.handle = surfaceHandle;

            VkMemoryAllocateInfo allocInfo = new VkMemoryAllocateInfo();
            allocInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO;
            allocInfo.pNext = &importMemInfoNV;
            allocInfo.allocationSize = memoryRequirements.size;

            if (memoryType == -1)
            {
                vkGraphicsContext.ValidationLayer?.Notify("Vulkan", "No suitable memory type.");
            }

            allocInfo.memoryTypeIndex = (uint)memoryType;

            if (dedicateMemoryExtension)
            {

                VkDedicatedAllocationMemoryAllocateInfoNV dedicatedAllocationInfo = new VkDedicatedAllocationMemoryAllocateInfoNV();
                dedicatedAllocationInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DEDICATED_ALLOCATION_MEMORY_ALLOCATE_INFO_NV;
                dedicatedAllocationInfo.image = image;

                importMemInfoNV.pNext = &dedicatedAllocationInfo;
            }

            VkDeviceMemory deviceMemory;
            result = VulkanNative.vkAllocateMemory(vkGraphicsContext.VkDevice, &allocInfo, null, &deviceMemory);
            VKHelpers.CheckErrors(vkGraphicsContext, result);

            result = VulkanNative.vkBindImageMemory(vkGraphicsContext.VkDevice, image, deviceMemory, 0);
            VKHelpers.CheckErrors(vkGraphicsContext, result);

            // Create Framebuffer
            TextureDescription textureDescription = new TextureDescription()
            {
                CpuAccess = ResourceCpuAccess.None,
                Width = width,
                Height = height,
                Depth = 1,
                Usage = Evergine.Common.Graphics.ResourceUsage.Default,
                Format = PixelFormat.B8G8R8A8_UNorm,
                ArraySize = 1,
                Faces = 1,
                MipLevels = 1,
                SampleCount = TextureSampleCount.None,
                Flags = TextureFlags.RenderTarget,
            };
            var SharedTexture = VKTexture.FromVulkanImage(vkGraphicsContext, ref textureDescription, image);
            var rTDepthTargetDescription = new TextureDescription()
            {
                Type = TextureType.Texture2D,
                Format = PixelFormat.D24_UNorm_S8_UInt,
                Width = width,
                Height = height,
                Depth = 1,
                ArraySize = 1,
                Faces = 1,
                Flags = TextureFlags.DepthStencil,
                CpuAccess = ResourceCpuAccess.None,
                MipLevels = 1,
                Usage = Evergine.Common.Graphics.ResourceUsage.Default,
                SampleCount = TextureSampleCount.None,
            };

            var factory = vkGraphicsContext.Factory;
            var rTDepthTarget = factory.CreateTexture(ref rTDepthTargetDescription, "SwapChain_Depth");

            FrameBuffer = factory.CreateFrameBuffer(new FrameBufferAttachment(rTDepthTarget, 0, 1), new[] { new FrameBufferAttachment(SharedTexture, 0, 1) });
            FrameBuffer.IntermediateBufferAssociated = true;            
        }
    }
}
