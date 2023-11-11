// See https://aka.ms/new-console-template for more information

using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

using static ImGuiNET.ImGuiNative;

RgbaFloat _clearColor = new RgbaFloat(0.45f, 0.55f, 0.6f, 1f);
//private static bool _showImGuiDemoWindow = true;

Sdl2Window _window;
GraphicsDevice _gd;
CommandList _cl;

float _f = 0f;

// Create window, GraphicsDevice, and all resources necessary for the demo.
VeldridStartup.CreateWindowAndGraphicsDevice(
	new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program"),
	new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
	out _window,
	out _gd);
_window.Resized += () =>
{
	_gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
	//_controller.WindowResized(_window.Width, _window.Height);
};

_cl = _gd.ResourceFactory.CreateCommandList();

while (_window.Exists)
{
	//deltaTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
	//.Restart();
	InputSnapshot snapshot = _window.PumpEvents();
	if (!_window.Exists) { break; }
	//_controller.Update(deltaTime, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

	//SubmitUI();

	ImGui.Text("");
    ImGui.Text(string.Empty);
    ImGui.Text("Hello, world!");                                        // Display some text (you can use a format string too)
    ImGui.SliderFloat("float", ref _f, 0, 1, _f.ToString("0.000"));  // Edit 1 float using a slider from 0.0f to 1.0f    

    ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");

	_cl.Begin();
	_cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
	_cl.ClearColorTarget(0, _clearColor);
	//_controller.Render(_gd, _cl);
	_cl.End();
	_gd.SubmitCommands(_cl);
	_gd.SwapBuffers(_gd.MainSwapchain);
}

// Clean up Veldrid resources
_gd.WaitForIdle();
//_controller.Dispose();
_cl.Dispose();
_gd.Dispose();

Console.WriteLine("Hello, World!");
