using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

RgbaFloat _clearColor = new RgbaFloat(0.45f, 0.55f, 0.6f, 1f);

Sdl2Window window;
GraphicsDevice gpu;
CommandList cmds;
ImGuiController renderer;

// Create window, GraphicsDevice, and all resources necessary for the demo.
VeldridStartup.CreateWindowAndGraphicsDevice(
	new WindowCreateInfo(50, 50, 1920, 1080, WindowState.Normal, "rzrBoy Studio"),
	new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
	out window,
	out gpu);

cmds = gpu.ResourceFactory.CreateCommandList();
renderer = new ImGuiController(gpu, gpu.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);
//renderer = new Veldrid.ImGuiRenderer(gpu, gpu.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);

window.Resized += () =>
{
	gpu.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
	renderer.WindowResized(window.Width, window.Height);
};

dbg.ui.Fonts.Init( );
renderer.RecreateFontDeviceTexture( gpu );

dbg.Debugger debugger = new();
dbg.ui.Gui gui = new(debugger);

gui.Init();

var stopwatch = System.Diagnostics.Stopwatch.StartNew();

while (window.Exists)
{
	float deltaTime = stopwatch.ElapsedTicks / (float)System.Diagnostics.Stopwatch.Frequency;
	stopwatch.Restart();

	InputSnapshot snapshot = window.PumpEvents();
	if (!window.Exists) { break; }

	renderer.Update(deltaTime, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

	gui.Update();

	cmds.Begin();
	cmds.SetFramebuffer(gpu.MainSwapchain.Framebuffer);
	cmds.ClearColorTarget(0, _clearColor);
	renderer.Render(gpu, cmds);
	cmds.End();
	gpu.SubmitCommands(cmds);
	gpu.SwapBuffers(gpu.MainSwapchain);
}

// Clean up Veldrid resources
gpu.WaitForIdle();
renderer.Dispose();
cmds.Dispose();
gpu.Dispose();