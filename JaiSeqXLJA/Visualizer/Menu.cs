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
            new WindowCreateInfo(50, 50, 1024, 768, WindowState.Normal, "JAISeqX - LibJAudio"),
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
        private static long Ticks = 0;
        public static void SubmitUI()
        {
        

            Ticks++;
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

                ImGui.Columns(2);

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

        
      
                ImGui.Columns(1);

                ImGui.Text("Remaining DSP Bandwidth\t\t JaiSeqX by Xayrga!");
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
            ImGui.SetNextWindowSize(new Vector2(320, 600));

            ImGui.Begin("TrackInfo", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                var ib = 0;
                for (int i = 0; i < Player.JAISeqPlayer.tracks.Length; i++)
                {
                    if (Player.JAISeqPlayer.tracks[i] == null)
                        continue;
                    var w = Player.JAISeqPlayer.tracks[i];
                    ImGui.Dummy(new Vector2(0, 2f));
                    if (w.lastOpcode == "FIN")
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
                        ImGui.Text($"opcode:{w.lastOpcode,-15} VOI: {w.activeVoices,-3}  PC: 0x{w.pc:X}");
                        ImGui.PopStyleColor();
                    } else
                    {
                        //DEL: {w.delay:X4}!{w.lastDelay,-8:X4}
                        ImGui.Text($"opcode:{w.lastOpcode,-15} VOI: {w.activeVoices,-3}  PC: 0x{w.pc:X}");
                    }
                }
                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FFFF);
                if (Player.JAISeqPlayer.noDKJBWhistle)
                    ImGui.Text("nodkwhistle: Filtering out DK whistle sounds.");
                ImGui.PopStyleColor();
            }

            ImGui.SetNextWindowPos(new Vector2( 452, 200));
            ImGui.SetNextWindowSize(new Vector2(190, 600));
            ImGui.Begin("TrackInfo2", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                var DrawList = ImGui.GetWindowDrawList();
                var trk = 0;
                for (int i = 0; i < Player.JAISeqPlayer.tracks.Length; i++)
                {
                    if (Player.JAISeqPlayer.tracks[i] == null)
                        continue;
                  
                    var w = Player.JAISeqPlayer.tracks[i];
                    ImGui.Dummy(new Vector2(0.0f, 0.1f));
                    ImGui.ProgressBar((float)w.delay / (float)w.lastDelay, new Vector2(80,15), $"{w.delay:X4}/{w.lastDelay:X4}");

                    var ofs = w.currentVibrato * (w.activeVoices > 0 ? 1 : 0);
                    var col = 0xFFFF0000;
                    if (ofs != 0)
                        col = 0xFFFFCfCf;


                    DrawList.AddCircleFilled(new Vector2(590 + 20 * ofs ,218 + trk * 23.6f), 5, col);
                    trk++;
                }

              

            }
            ImGui.End();


            ImGui.SetNextWindowPos(new Vector2(643, 200));
            ImGui.SetNextWindowSize(new Vector2(380, 600));


            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, 0xFF0000FF);
            ImGui.Begin("Parameters", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                ImGui.Columns(3);
                var ib = 0;
                for (int i = 0; i < Player.JAISeqPlayer.tracks.Length; i++)
                {
                    if (Player.JAISeqPlayer.tracks[i] == null)
                        continue;
                    var w = Player.JAISeqPlayer.tracks[i];
                    ImGui.Dummy(new Vector2(0, 2f));
                    ImGui.ProgressBar(w.volume,new Vector2(100,13));
                    ImGui.NextColumn();
                    ImGui.Dummy(new Vector2(0, 2f));
                    ImGui.ProgressBar(0.5f - w.panning, new Vector2(100, 13));
   
                    ImGui.NextColumn();
                    ImGui.Dummy(new Vector2(0, 2f));
                    // ImGui.ProgressBar(( (w.currentPitchBend-1f) / 0.1f) + 0.5f, new Vector2(100, 13));
                    ImGui.ProgressBar( (w.pitchTarget / 0.4f) + 0.5f , new Vector2(100, 13));
                    ImGui.NextColumn();

                    //w.Registers.clearChanged();
                }
                ImGui.Text("MIX VOLU");
                ImGui.NextColumn();
                ImGui.Text("MIX POSI");
                ImGui.NextColumn();
                ImGui.Text("PTCH WHL");
            }
            ImGui.PopStyleColor();

            ImGui.End();



            /*
            ImGui.Begin("CReg / TPrt");
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
                        ImGui.Text($"T {Player.JAISeqPlayer.tracks[i].trackNumber:X2} rS: {w.Registers[0]:x} rC:{w.Registers[3]:x} rA:{w.Registers[1],-6:X} p0:{w.Ports[0]:x} p1:{w.Ports[1]}:x");
                        igPopStyleColor(1);
                    } else
                    {
                        ImGui.Text($"T {Player.JAISeqPlayer.tracks[i].trackNumber:X2} rS: {w.Registers[0]:x} rC:{w.Registers[3]:x} rA:{w.Registers[1],-6:X} p0:{w.Ports[0]:x} p1:{w.Ports[1]}:x");
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
            */

        }

    }
}
