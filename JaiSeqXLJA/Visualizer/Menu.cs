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
        private static int piT = -1;
        private static int piR = 0;
        private static int piV = 0;

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

        private static int tickSteps = 0;
        public static void SubmitUI()
        {

            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(132 + 512,200));


            ImGui.Begin("ControlWindow", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                
                var itn = Player.JAISeqPlayer.ppqn;
                var itb = Player.JAISeqPlayer.bpm;
                var pau = Player.JAISeqPlayer.paused;

                ImGui.SliderInt("BPM", ref itb, 1, 256);
                ImGui.SliderInt("PPQN", ref itn, 1, 8192);
                ImGui.SliderFloat("Gain Multiplier", ref Player.JAISeqPlayer.gainMultiplier, 0, 1);
                ImGui.Checkbox("Paused", ref Player.JAISeqPlayer.paused);
                ImGui.SliderInt("Tick Steps", ref tickSteps, 1, 3000);


                var totalVoices = 0f;
                var totalTracks = 0f;
                for (int i = 0; i < Player.JAISeqPlayer.tracks.Length; i++)
                {
                    
                    if (Player.JAISeqPlayer.tracks[i] == null)
                        continue;
                    totalTracks++;
                    var w = Player.JAISeqPlayer.tracks[i];
              
                    totalVoices += w.activeVoices;
                }

                totalVoices = ((totalVoices / totalTracks) / 7f) * 100f;
                if (itn!= Player.JAISeqPlayer.ppqn || itb!= Player.JAISeqPlayer.bpm)
                {
                    Player.JAISeqPlayer.ppqn = itn;
                    Player.JAISeqPlayer.bpm = itb;
                    Player.JAISeqPlayer.recalculateTimebase();
                }


                if (ImGui.Button("Tick Step"))
                {
                    var oldPauseState = pau;
                    Player.JAISeqPlayer.paused = false; 
                    for (int i=0; i < tickSteps; i++)
                    {
                        Player.JAISeqPlayer.tick();
                    }
                    Player.JAISeqPlayer.paused = oldPauseState;
                }
                ImGui.Text("Remaining DSP Bandwidth");
                ImGui.ProgressBar((100f - totalVoices) / 100f);

            }
            ImGui.End();




            ImGui.SetNextWindowPos(new Vector2(0, 200));
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


            ImGui.SetNextWindowPos(new Vector2(132, 200));
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
                    ImGui.Text($"LST:{w.lastOpcode,-15} VOI: {w.activeVoices,-5} DEL: {w.delay:X4}!{w.lastDelay,-8:X4}  PC: 0x{w.pc:X}");
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
                    if (w.Registers.changed[0] > 0)
                    {
                        w.Registers.changed[0]--;
                        igPushStyleColor(ImGuiCol.Text, new Vector4(255, 255, 0, 255));
                        ImGui.Text($"rS: {w.Registers[0]:x} rC:{w.Registers[3]:x} rA:{w.Registers[1],-6:X} p0:{w.Ports[0]:x} p1:{w.Ports[1]}:x");
                        igPopStyleColor(1);
                    } else
                    {
                        ImGui.Text($"rS: {w.Registers[0]:x} rC:{w.Registers[3]:x} rA:{w.Registers[1],-6:X} p0:{w.Ports[0]:x} p1:{w.Ports[1]}:x");
                    }

           
                    
        

                    //w.Registers.clearChanged();
                }
            }

            ImGui.End();

            ImGui.Begin("Port Injection");
            {
                ImGui.InputInt("Track", ref piT);
                ImGui.InputInt("Register", ref piR);
                ImGui.InputInt("Value", ref piV);
                if (ImGui.Button("INJECT"))
                {
                    for (int i = 0; i < Player.JAISeqPlayer.tracks.Length; i++)
                    {
                        if (Player.JAISeqPlayer.tracks[i] == null)
                            continue;
                        var trk = Player.JAISeqPlayer.tracks[i];
                        if (trk.trackNumber==piT)
                        {
                            trk.Registers[(byte)piR] = (short)piV;
                        }

                    }
                }
            }

        }

    }
}
