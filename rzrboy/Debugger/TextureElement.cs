using System.Runtime.InteropServices;

namespace dbg.ui
{
	public class TextureElement : ImGuiScopeBase
	{
		private rzr.IFramebuffer m_img;
		private Renderer m_rend;
		private Veldrid.Texture m_tex;
		private IntPtr m_imguiImg;

		public float Width {get; set;} = 1f;
		public float Height {get; set;} = 1f;		

		public TextureElement( rzr.IFramebuffer img, Renderer rend, string label ) : base( ImGuiNET.ImGui.BeginChild, ImGuiNET.ImGui.EndChild, label )
		{
			m_img = img;
			m_rend = rend;
			var desc = Veldrid.TextureDescription.Texture2D(
				img.Width, img.Height, mipLevels: 1, arrayLayers: 1, format: Veldrid.PixelFormat.R8_G8_B8_A8_UNorm, Veldrid.TextureUsage.Sampled);

			m_tex = rend.Device.ResourceFactory.CreateTexture(desc);
			m_imguiImg = rend.GetOrCreateImGuiBinding(m_tex);

			//https://github.com/ImGuiNET/ImGui.NET/issues/141
		}

		private unsafe void UpdateImage()
		{
			var data = m_img.GetPixels();
			int PixelSizeInBytes = sizeof(rzr.Rgba32);
			uint sizeInBytes = (uint)(PixelSizeInBytes * m_img.Width * m_img.Height);
			fixed (void* pin = &MemoryMarshal.GetArrayDataReference(data))
            {
				m_rend.Device.UpdateTexture(
					m_tex,
					(IntPtr)pin,
					sizeInBytes: sizeInBytes,
					x: 0,
					y: 0,
					z: 0,
					width: m_img.Width,
					height: m_img.Height,
					depth: 1,
					mipLevel: 0,
					arrayLayer: 0
					);
			}
			m_img.Dirty = false;
		}

		protected override bool BodyFunc()
		{
			if(m_img.Dirty)
				UpdateImage();				

			ImGuiNET.ImGui.Image(m_imguiImg, new (Width*m_img.Width, Height*m_img.Height));

			return true;
		}
	}
}
