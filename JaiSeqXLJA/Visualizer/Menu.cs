using System;
using System.IO;
using System.Numerics;
using Newtonsoft.Json;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;
using static ImGuiNET.ImGuiNative;

namespace JaiSeqXLJA.Visualizer
{
    public static class Menu
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;

        private static ImGuiController _controller;
        private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);


        public static void init()
        {
            VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(50, 50, 1024, 768, WindowState.Normal, "ProceduralOreGeneratorUI"),
            new GraphicsDeviceOptions(true, null, true),
            out _window,
            out _gd);
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };

            _cl = _gd.ResourceFactory.CreateCommandList();
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);
        }

        public static void update()
        {
            if (!_window.Exists) { Environment.Exit(0); return; }
            InputSnapshot snapshot = _window.PumpEvents();
            if (!_window.Exists) { return; }
            _controller.Update(1f / 60f, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

            SubmitUI();

            _cl.Begin();
            _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
            _controller.Render(_gd, _cl);
            _cl.End();
            _gd.SubmitCommands(_cl);
            _gd.SwapBuffers(_gd.MainSwapchain);
        }

        private static int changeFrames = 0;
     
        public static void SubmitUI()
        {

            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(132 + 512,168));


            ImGui.Begin("ControlWindow", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                
                var itn = Player.JAISeqPlayer.ppqn;
                var itb = Player.JAISeqPlayer.bpm;
                ImGui.SliderInt("BPM", ref itb, 1, 256);
                ImGui.SliderInt("PPQN", ref itn, 1, 8192);
                ImGui.SliderFloat("Gain Multiplier", ref Player.JAISeqPlayer.gainMultiplier, 0, 1);
                if (itn!= Player.JAISeqPlayer.ppqn || itb!= Player.JAISeqPlayer.bpm)
                {
                    Player.JAISeqPlayer.ppqn = itn;
                    Player.JAISeqPlayer.bpm = itb;
                    Player.JAISeqPlayer.recalculateTimebase();
                }


            }
            ImGui.End();




            ImGui.SetNextWindowPos(new Vector2(0, 168));
            ImGui.SetNextWindowSize(new Vector2(132, 600));


            ImGui.Begin("Window", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                var ib = 0;
                for (int i=0; i < Player.JAISeqPlayer.tracks.Length; i++)
                {
                    if (Player.JAISeqPlayer.tracks[i] == null)
                        continue;
                    bool w = Player.JAISeqPlayer.tracks[i].muted;
                    if (Player.JAISeqPlayer.tracks[i].trackNumber == -1)
                    {
                        ImGui.Checkbox($"Root Track", ref w);
                    }
                    else
                    {
                        ImGui.Checkbox($"Track {Player.JAISeqPlayer.tracks[i].trackNumber} mute", ref w);
                    }

                    if (w != Player.JAISeqPlayer.tracks[i].muted)
                    {
                        Player.JAISeqPlayer.setTrackMuted(Player.JAISeqPlayer.tracks[i].trackNumber, w);
                    }
                }
            }
            ImGui.End();


            ImGui.SetNextWindowPos(new Vector2(132, 168));
            ImGui.SetNextWindowSize(new Vector2(512, 600));

            ImGui.Begin("TrackInfo", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                var ib = 0;
                for (int i = 0; i < Player.JAISeqPlayer.tracks.Length; i++)
                {
                    if (Player.JAISeqPlayer.tracks[i] == null)
                        continue;
                    var w = Player.JAISeqPlayer.tracks[i];
                    ImGui.Dummy(new Vector2(0, 2f));
                    ImGui.Text($"LST:{w.lastOpcode,-15} VOI: {w.activeVoices,-5} DEL: {w.delay,-5} PC: 0x{w.pc:X} ");
                
                }
            }

            ImGui.SetNextWindowPos(new Vector2(648, 168));
            ImGui.SetNextWindowSize(new Vector2(375, 600));

            ImGui.Begin("CReg / TPrt", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
            {
                var ib = 0;
                for (int i = 0; i < Player.JAISeqPlayer.tracks.Length; i++)
                {
                    if (Player.JAISeqPlayer.tracks[i] == null)
                        continue;
                    var w = Player.JAISeqPlayer.tracks[i];
                    ImGui.Dummy(new Vector2(0, 2f));
                    if (w.Registers.changed[0])
                        changeFrames = 30;
                        
                    if (changeFrames > 0 )
                        igPushStyleColor(ImGuiCol.Text, new Vector4(255, 0, 0, 255));
                    ImGui.Text($"rS: {w.Registers[0]} rC:{w.Registers[3]} rA:{w.Registers[1],-6} p0:{w.Ports[0]} p1:{w.Ports[1]}");
                    if (changeFrames > 0)
                    {
                        igPopStyleColor(1);
                        changeFrames--;
                    }

                    w.Registers.clearChanged();
                }
            }

            ImGui.End();
        }

    }
}
