using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

RgbaFloat _clearColor = new RgbaFloat(0.45f, 0.55f, 0.6f, 1f);

Sdl2Window window;
GraphicsDevice gpu;
CommandList cmds;
ImGuiController controller;

// Create window, GraphicsDevice, and all resources necessary for the demo.
VeldridStartup.CreateWindowAndGraphicsDevice(
	new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "rzrBoy Studio"),
	new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
	out window,
	out gpu);

cmds = gpu.ResourceFactory.CreateCommandList();
controller = new ImGuiController(gpu, gpu.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);

window.Resized += () =>
{
	gpu.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
	controller.WindowResized(window.Width, window.Height);
};

dbg.Debugger debugger = new();
dbg.Gui gui = new(debugger);

var stopwatch = System.Diagnostics.Stopwatch.StartNew();
float deltaTime = 0f;

while (window.Exists)
{
	deltaTime = stopwatch.ElapsedTicks / (float)System.Diagnostics.Stopwatch.Frequency;
	stopwatch.Restart();

	InputSnapshot snapshot = window.PumpEvents();
	if (!window.Exists) { break; }

	controller.Update(deltaTime, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

	gui.Update();

	cmds.Begin();
	cmds.SetFramebuffer(gpu.MainSwapchain.Framebuffer);
	cmds.ClearColorTarget(0, _clearColor);
	controller.Render(gpu, cmds);
	cmds.End();
	gpu.SubmitCommands(cmds);
	gpu.SwapBuffers(gpu.MainSwapchain);
}

// Clean up Veldrid resources
gpu.WaitForIdle();
controller.Dispose();
cmds.Dispose();
gpu.Dispose();