using System;
using System.IO;
using System.Numerics;
using Newtonsoft.Json;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;
using System.Diagnostics;
using JaiSeqXLJA.Player;
using Un4seen.Bass;
using JaiSeqXLJA.DSP;
using System.Collections.Generic;
using Veldrid.ImageSharp;
using SixLabors.ImageSharp;


namespace JaiSeqXLJA.Visualizer
{
    public static class Menu 
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;

        private static ImGuiRenderer _controller;
        private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);
        private static bool showReverbMenu = false;
        private static int piT = -1;
        private static int piR = 9;
        private static int piV = 0;

        private static int riR = 0;
        private static int riV = 0;

        private static ImageSharpTexture JSXLogo;
        private static nint JSXLogoDevTxr;

        public static void init()
        {
            VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(50, 50, 1024, 768, WindowState.Normal, "JAISeqX"),
            new GraphicsDeviceOptions(true, null, true),
            out _window,
            out _gd);

            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };

            _cl = _gd.ResourceFactory.CreateCommandList();
            _controller = new ImGuiRenderer(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

            JSXLogo = new ImageSharpTexture("./assets/jaiseqx.png");
            var devtxr = JSXLogo.CreateDeviceTexture(_gd, _gd.ResourceFactory);
            JSXLogoDevTxr = _controller.GetOrCreateImGuiBinding(_gd.ResourceFactory,devtxr);
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

        private static Vector2 dummyzero = new Vector2();
        private static void drawTrackText(JAISeqTrack w)
        {

            var DrawList = ImGui.GetWindowDrawList();
            var io = ImGui.GetIO();
            ImGui.SameLine();
            var col = 0xFFFFFFFF;

            if (w.lastOpcode == "ff-FIN")
            {
                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
                ImGui.Text($"((STOPPED)) 0x{w.pc:x5}(0x{w.lastOpcode})");
                ImGui.PopStyleColor();

            }
            else if (w.crashed)
            {

                if (Math.Sin(JAISeqPlayer.tickTimer.ElapsedMilliseconds / 100f) > 0)
                    col = 0xFF0000FF;

                ImGui.PushStyleColor(ImGuiCol.Text, col);
                ImGui.Text($"((CRASHED)) 0x{w.pc:x5}(0x{w.lastOpcode})");
                ImGui.PopStyleColor();
            }
            else
            {
                //DEL: {w.delay:X4}!{w.lastDelay,-8:X4}
                //ImGui.Text($"0x{w.pc:x5}(0x{w.lastOpcode})");
                ImGui.Text($"@ 0x{w.pc:x5}");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    if (io.KeyShift)
                    {
                        ImGui.Text($"---{w.TrackName} Ports---");
                       
                        for (int i = 0; i < 16; i++)
                            if (w.Ports[i] == 0)
                                ImGui.Text($"{PORTS_LIST[i]} = {w.Ports[i]:X}");
                            else
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FFFF);
                                ImGui.Text($"{PORTS_LIST[i]} = {w.Ports[i]:X}");
                                ImGui.PopStyleColor();
                            }

                    }
                    else if (io.KeyAlt)
                    {
                        ImGui.Text($"-------------------{w.TrackName} Registers--------------------");
                        ImGui.Columns(3);
                        for (byte i = 0; i < 16; i++)
                            if (w.TrackRegisters[i] > 0)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FFFF);
                                ImGui.Text($"[{i:X2}]={w.TrackRegisters[i]:x3}");
                                ImGui.PopStyleColor();
                            }
                            else                            
                                ImGui.Text($"[{i:X2}]={w.TrackRegisters[i]:x3}");
                        ImGui.NextColumn();
                        for (byte i = 16; i < 32; i++)
                            if (w.TrackRegisters[i] > 0)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FFFF);
                                ImGui.Text($"[{i:X2}]={w.TrackRegisters[i]:x3}");
                                ImGui.PopStyleColor();
                            }
                            else
                                ImGui.Text($"[{i:X2}]={w.TrackRegisters[i]:x3}");
                        ImGui.NextColumn();
                        for (byte i = 32; i < 48; i++)
                            if (w.TrackRegisters[i] > 0)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FFFF);
                                ImGui.Text($"[{i:X2}]={w.TrackRegisters[i]:x3}");
                                ImGui.PopStyleColor();
                            }
                            else                                
                                ImGui.Text($"[{i:X2}]={w.TrackRegisters[i]:x3}");
                                
                            
                    }
                    else
                    {

                        ImGui.Text($"------------Track {w.TrackName} Info------------");
                        ImGui.Text($"Bank: {w.TrackRegisters[0x21]:X}");
                        ImGui.Text($"Program: {w.TrackRegisters[0x20]:X}");
                        ImGui.Text($"Offset: 0x{w.pc:X}");
                        ImGui.Text($"Subroutine: {w.lastCallAddress:X}");
                        if (w.lastCallAddress > 0)
                        {
                            var endAddr = Math.Clamp((float)(w.previousCallEnd - w.lastCallAddress), 0, 999999999);
                            ImGui.Text("Subroutine progress");
                            ImGui.SameLine();
                            ImGui.ProgressBar(((float)(w.pc - w.lastCallAddress) / endAddr));
                        }
                        ImGui.Text($"Last Instruction: {w.lastOpcode}");
                        ImGui.Text("---Stack---");
                        var starr = w.CallStack.ToArray();
                        for (int b = 0; b < starr.Length; b++)
                            ImGui.Text($"\t0x{starr[b]:X}");
                        ImGui.Text($"Mute: {w.muted}");
                        ImGui.Text($"Reading from ports: ");
                        foreach (int b in w.PortReads)
                        {
                            ImGui.SameLine();
                            ImGui.Text($"{b:X2}");
                        }
                        ImGui.Text("----------");
                        ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FFFF);
                        ImGui.Text($"ALT = View Registers");
                        ImGui.Text($"SHIFT = View Ports");
                        ImGui.PopStyleColor();

                    }
                    ImGui.EndTooltip();
                };
           

               
            }
        }

        private static void drawTrackParams(JAISeqTrack w)
        {

            ImGui.Dummy(new Vector2(0, 2f));
            ImGui.ProgressBar(w.volume, new Vector2(85, 13));
            ImGui.NextColumn();
            ImGui.Dummy(new Vector2(0, 2f));
            ImGui.ProgressBar(w.panning / 128f, new Vector2(85, 13));

            ImGui.NextColumn();
            ImGui.Dummy(new Vector2(0, 2f));
            // ImGui.ProgressBar(( (w.currentPitchBend-1f) / 0.1f) + 0.5f, new Vector2(100, 13));
            ImGui.ProgressBar((w.pitchTarget / 0.4f) + 0.5f, new Vector2(85, 13));
            ImGui.NextColumn();
            ImGui.Dummy(new Vector2(0, 2f));
            ImGui.ProgressBar(w.reverb, new Vector2(85, 13));
            ImGui.NextColumn();


            foreach (KeyValuePair<int, JAISeqTrack> child in w.Children)
                drawTrackParams(child.Value);

        }

        private static (int, int) getVoiceInfo(JAISeqTrack track)
        {
            var releasing = 0;
            var active = 0;
            releasing += track.activeVoiceOrphans;
            active += track.activeVoices;

            foreach (KeyValuePair<int, JAISeqTrack> child in track.Children)
            {

                var info = getVoiceInfo(child.Value);
                active += info.Item1;
                releasing += info.Item2;
            }
            return (active, releasing);
        }

        private static void drawTrackStatus(JAISeqTrack track, int depth = 0)
        {
            var indents = "";
            ImGui.Dummy(new Vector2(depth * 10, 1));
            ImGui.SameLine();
            ImGui.ProgressBar((track.lastDelay - (float)track.delay) / (float)track.lastDelay, new Vector2(80, 15), $"{track.delay:X4}/{track.lastDelay:X4}");

            ImGui.SameLine();
            if (ImGui.Checkbox(track.TrackName, ref track.muted) && track.muted)
                track.purgeVoices();

            drawTrackText(track);


            depth++;

            foreach (KeyValuePair<int, JAISeqTrack> child in track.Children)
                drawTrackStatus(child.Value, depth);
        }

        private static void drawTrackVibrato(JAISeqTrack w)
        {
            var DrawList = ImGui.GetWindowDrawList();


            var offsetSide = w.currentVibrato * (w.activeVoices > 0 ? 1 : 0);
            var col = 0xFFFF0000;
            if (offsetSide != 0)
                col = 0xFFFFCfCf;

            DrawList.AddCircleFilled(ImGui.GetCursorScreenPos() + new Vector2(15 + offsetSide * 13f, 0), 5, col);


            for (int i = 1; i < w.voiceStatus.Length; i++)
            {
                var col2 = 0xFF0000FF;

                var pch = (w.pitchTarget / 0.4f) * 10; ;
                var topLeft = ImGui.GetCursorScreenPos() + new Vector2(35 + 12f * i, -5f);
                var bottomRight = ImGui.GetCursorScreenPos() + new Vector2(45 + 12f * i, 5f);
                if (w.voiceStatus[i] == true)
                {
                    topLeft += new Vector2(0, -pch);
                    bottomRight += new Vector2(0, -pch);

                    var nAlp = 0x7f + 0x7F * w.volume;
                    uint nCol = 0xFFFFFF | ((uint)nAlp << 24);
                    DrawList.AddRectFilled(topLeft, bottomRight, col2, 0, ImDrawCornerFlags.None);
                    DrawList.AddRectFilled(topLeft, bottomRight, nCol, 0, ImDrawCornerFlags.None);
                }
                else
                    DrawList.AddRectFilled(topLeft, bottomRight, col2, col, ImDrawCornerFlags.None);

            }


            ImGui.Dummy(new Vector2(0, 19f));
            foreach (KeyValuePair<int, JAISeqTrack> child in w.Children)
                drawTrackVibrato(child.Value);
        }
        static int kk = 0;


        public enum JAIPortNames
        {
            CMD = 0x00,
            END,
            STATUS,
            WAIT,
            NUMBER,
            PORT_5,
            MAP_PORT,
            NOTE_PORT,
            SE_SELECT_PORT,
            BGM_STATUS_PORT,
            BGM_PORT2,
            BGM_PORT3,
            PORT12,
            PORT13,
            FILTER_PORT,
            FX_PORT
        }

        private static string[] PORTS_LIST =
        {
            "00 - CMD",
            "01 - END",
            "02 - STATUS",
            "03 - WAIT",
            "04 - NUMBER",
            "05 - PORT_5",
            "06 - MAP_PORT",
            "07 - NOTE_PORT",
            "08 - SE_SELECT_PORT",
            "09 - BGM_STATUS_PORT",
            "0A - BGM_PORT2",
            "0B - BGM_PORT3",
            "0C - PORT12",
            "0D - PORT13",
            "0E - FILTER_PORT",
            "0F - FX_PORT"
        };
        public static void SubmitUI()
        {
            Ticks++;
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(132 + 512, 200));






            ImGui.Begin("ControlWindow", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                var itn = Player.JAISeqPlayer.ppqn;
                var itb = Player.JAISeqPlayer.bpm;
                var pau = Player.JAISeqPlayer.paused;

                var totalTracks = 0f;
                var totalOrphans = 0;
                var totalVoices = 0;


                var voiceInfo = getVoiceInfo(JAISeqPlayer.RootTrack);

                totalOrphans = voiceInfo.Item2;
                totalVoices = voiceInfo.Item1 + voiceInfo.Item2;


                ImGui.SliderInt("BPM", ref itb, 1, 256);
                ImGui.SliderInt("PPQN", ref itn, 1, 8192);
                ImGui.SliderFloat("Gain Multiplier", ref Player.JAISeqPlayer.gainMultiplier, 0, 2);
                ImGui.Checkbox("Paused", ref Player.JAISeqPlayer.paused);
                ImGui.SameLine();
                ImGui.Checkbox("Reverb Settings", ref showReverbMenu);
                ImGui.SameLine();
                ImGui.Checkbox("Sequence Debug", ref JAISeqPlayer.Debug);
                ImGui.SameLine();
                if (ImGui.Button("Dump WaveID"))
                    JAISeqPlayer.dumpReferencedWaves();

                if (itn != Player.JAISeqPlayer.ppqn || itb != Player.JAISeqPlayer.bpm)
                {
                    Player.JAISeqPlayer.ppqn = itn;
                    Player.JAISeqPlayer.bpm = itb;
                    Player.JAISeqPlayer.recalculateTimebase();
                }
                ImGui.Columns(1);

                var fac = 0.29f;
       
                ImGui.Image(JSXLogoDevTxr, new Vector2(390 * fac, 130 * fac));
                ImGui.SameLine();
                ImGui.Text("\n | https://github.com/xayrga/jaiseqx");
      
                ImGui.Dummy(new Vector2(0, 0));

        
                ImGui.Columns(1);
                var DrawList = ImGui.GetWindowDrawList();


                var usage = JAISeqPlayer.loadedSampleBytes / (1024);
                ImGui.Text($"SAMPLE RAM: {usage}KB ({JAISeqPlayer.loadedSamples:D2} samples)  ");
                DrawList.AddRectFilled(new Vector2(9, 160), new Vector2((usage / 8192f) * 200f, 165), 0xFFFF00FF);

                var totalWidth = 330f;

                var percNormal = totalWidth * ((totalVoices - totalOrphans) / 64f);
                DrawList.AddRectFilled(new Vector2(9, 173), new Vector2(percNormal + 9, 178), 0xFF0000FF);


                var percOrphans = totalWidth * ((totalOrphans) / 64f);
                DrawList.AddRectFilled(new Vector2(percNormal + 9, 173), new Vector2(percNormal + 9 + percOrphans, 178), 0xFFFF0000);


                DrawList.AddRectFilled(new Vector2(percNormal + percOrphans + 9, 173), new Vector2(totalWidth, 178), 0xFF00FFFF);

                ImGui.Dummy(new Vector2(0, 12.5f));
                ImGui.Text($"DSP CHNL: {(int)totalVoices:D2}/64 total, {(int)(totalVoices - totalOrphans):D2} active, {totalOrphans:D2} orphans  \\  {(int)(totalVoices * 44100):D7}/{44100 * 7 * 16} buffer  \\  {(totalVoices / 64f) * 100f,3:00.0}%%");





            }

            ImGui.SetNextWindowPos(new Vector2(0, 200));
            ImGui.SetNextWindowSize(new Vector2(500, 600));

            ImGui.Begin("TrackInfo", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                ImGui.Text("Track Structure");
                drawTrackStatus(JAISeqPlayer.RootTrack);
            }


            ImGui.SetNextWindowPos(new Vector2(500, 200));
            ImGui.SetNextWindowSize(new Vector2(143, 600));

            ImGui.Begin("TrackInfo2", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                ImGui.Text("VIBR      VOIC");
                ImGui.Dummy(new Vector2(0, 10f));
                drawTrackVibrato(JAISeqPlayer.RootTrack);
            }


            ImGui.SetNextWindowPos(new Vector2(643, 200));
            ImGui.SetNextWindowSize(new Vector2(380, 600));

            ImGui.Begin("Parameters", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {

                ImGui.Columns(4);
                ImGui.Text("VOL");
                ImGui.NextColumn();
                ImGui.Text("PAN");
                ImGui.NextColumn();
                ImGui.Text("PCH");
                ImGui.NextColumn();
                ImGui.Text("RVB");
                ImGui.NextColumn();
                drawTrackParams(JAISeqPlayer.RootTrack);
            }


            ImGui.SetNextWindowPos(new Vector2(644, 0));
            ImGui.SetNextWindowSize(new Vector2(380, 200));
            ImGui.Begin("Port Injection", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
            {
                ImGui.Text("Port Control");
                ImGui.Separator();
                ImGui.Combo("Port", ref piR, PORTS_LIST, 16);
                ImGui.InputInt("Value", ref piV);
                if (ImGui.Button("INJECT"))
                {
                    Player.JAISeqPlayer.RootTrack.Ports[piR] = (short)piV;
                    if (piR == 9)
                    {
                        JaiSeqXLJA.sequenceTransitioning = true;
                        kk = 200;
                    }
                    Player.JAISeqPlayer.RootTrack.portImport = true;
                }
                if (Math.Sin(JAISeqPlayer.tickTimer.ElapsedMilliseconds / 100f) > 0 && JaiSeqXLJA.sequenceTransitioning)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(1, 1, 0, 1), "Waiting for sequence transition....");
                }
                else if (JaiSeqXLJA.sequenceTransitioning == false && kk > 0)
                {

                    kk--;
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), "Transition OK!");
                }





                ImGui.Spacing();
                ImGui.Text("Interrupt Control");
                ImGui.Separator();
                /*
                ImGui.InputInt("Register##reg", ref riR);
                ImGui.InputInt("Value##reg", ref riV);
                if (ImGui.Button("INJECT##register"))
                {
                    Player.JAISeqPlayer.RootTrack.TrackRegisters[(byte)riR] = (short)riV;
                    kk = 120;
                }
                */

                ImGui.InputInt("Sync CBVAL", ref JAISeqPlayer.syncCallbackValue);
                if (ImGui.Button("Write Sync Value"))
                    JAISeqPlayer.RootTrack.writeSyncValue((byte)JAISeqPlayer.syncCallbackValue);
                ImGui.SameLine();
                if (ImGui.Button("Clear Interrupt"))
                    JAISeqPlayer.RootTrack.clearInterrupt();


            }

            if (showReverbMenu)
            {
                ImGui.Begin("JaiDSP Reverb Settings");
                ImGui.SliderFloat("fDamp", ref JaiSeqXLJA.fDamp, 0, 1);
                ImGui.SliderFloat("fDryMix", ref JaiSeqXLJA.fDryMix, 0, 1);
                ImGui.SliderFloat("fRoomSize", ref JaiSeqXLJA.fRoomSize, 0, 1);
                ImGui.SliderFloat("fWetMix", ref JaiSeqXLJA.fWetMix, 0, 3);
                ImGui.SliderFloat("fWidth", ref JaiSeqXLJA.fWidth, 0, 1);
                ImGui.End();
            }


        }

    }
}
