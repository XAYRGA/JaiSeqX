using System;
using System.IO;
using System.Numerics;
using Newtonsoft.Json;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;
using static ImGuiNET.ImGuiNative;
using System.Diagnostics;
using JaiSeqXLJA.Player;
using Un4seen.Bass;
using JaiSeqXLJA.DSP;
using System.Collections.Generic;

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
        private static int piR = 9;
        private static int piV = 0;

        private static int riR = 0;
        private static int riV = 0;

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


        private static void drawTrackText(JAISeqTrack w)
        {
  
     

            var DrawList = ImGui.GetWindowDrawList();
            //ImGui.Dummy(new Vector2(0, 2f));
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
                ImGui.Text($"0x{w.pc:x5}(0x{w.lastOpcode})");
            }


            //ImGui.Dummy(new Vector2(0.0f, 0.1f));
    

            /*var offsetSide = w.currentVibrato * (w.activeVoices > 0 ? 1 : 0);
            col = 0xFFFF0000;
            if (offsetSide != 0)
                col = 0xFFFFCfCf;

            DrawList.AddCircleFilled(new Vector2(590f + 19.5f * offsetSide, 235 + trackRowIndex * 23.6f), 5, col);*/
         

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

        private static (int,int) getVoiceInfo(JAISeqTrack track)
        {
            var releasing = 0;
            var active = 0;
            releasing += track.activeVoiceOrphans;
            active += track.activeVoices;

         foreach (KeyValuePair<int, JAISeqTrack> child in track.Children) { 
 
            var info = getVoiceInfo(child.Value);
                active += info.Item1;
                releasing += info.Item2;
            }
            return (active,releasing);
        }

        private static void drawTrackStatus(JAISeqTrack track, int depth = 0)
        {
            var indents = "";
            ImGui.Dummy(new Vector2(depth * 10, 1));
            ImGui.SameLine();
            ImGui.ProgressBar((float)track.delay / (float)track.lastDelay, new Vector2(80, 15), $"{track.delay:X4}/{track.lastDelay:X4}");       
           
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

            DrawList.AddCircleFilled(ImGui.GetCursorScreenPos() + new Vector2(15 + offsetSide*13f,0), 5, col);


            for (int i = 1; i < w.voiceStatus.Length; i++) {
                var col2 = 0xFF0000FF;

                var pch = (w.pitchTarget / 0.4f) * 10; ;
                var topLeft = ImGui.GetCursorScreenPos() + new Vector2(35 + 12f * i, -5f );
                var bottomRight = ImGui.GetCursorScreenPos() + new Vector2(45 + 12f * i, 5f);
                if (w.voiceStatus[i] == true)
                {
                    topLeft += new Vector2(0, -pch);
                    bottomRight += new Vector2(0, -pch);

                    var nAlp = 0x7f + 0x7F * w.volume;
                    uint nCol = 0xFFFFFF | ((uint)nAlp << 24);
                    DrawList.AddRectFilled(topLeft, bottomRight, col2, 0, ImDrawCornerFlags.None);
                    DrawList.AddRectFilled(topLeft, bottomRight, nCol, 0, ImDrawCornerFlags.None);
                } else 
                    DrawList.AddRectFilled(topLeft, bottomRight, col2, col, ImDrawCornerFlags.None);

            }
            

            ImGui.Dummy(new Vector2(0, 19f));
            foreach (KeyValuePair<int, JAISeqTrack> child in w.Children)
                drawTrackVibrato(child.Value);
        }
        static int kk = 0;

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
                ImGui.SliderInt("Tick Steps", ref tickSteps, 1, 3000);

                if (itn != Player.JAISeqPlayer.ppqn || itb != Player.JAISeqPlayer.bpm)
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
                    for (int i = 0; i < tickSteps; i++)
                    {
                        Player.JAISeqPlayer.tick();
                    }
                    Player.JAISeqPlayer.paused = oldPauseState;
                }
                ImGui.NextColumn();
                ImGui.Text("\nJAISeqX https://github.com/xayrga/jaiseqx");


                ImGui.Columns(1);
                var DrawList = ImGui.GetWindowDrawList();


                var usage = JAISeqPlayer.loadedSampleBytes / (1024);
                ImGui.Text($"SAMPLE RAM: {usage}KB ({JAISeqPlayer.loadedSamples:D2} samples)  ");
                DrawList.AddRectFilled(new Vector2(9, 160), new Vector2((usage / 8192f) * 200f, 165), 0xFFFF00FF);

                var totalWidth = 330f;       

                var percNormal = totalWidth * ((totalVoices - totalOrphans) / 64f);
                DrawList.AddRectFilled(new Vector2(9, 175), new Vector2(percNormal + 9, 180), 0xFF0000FF);


                var percOrphans = totalWidth * ((totalOrphans) / 64f);
                DrawList.AddRectFilled(new Vector2(percNormal + 9, 175), new Vector2(percNormal + 9 + percOrphans, 180), 0xFFFF0000);


                DrawList.AddRectFilled(new Vector2(percNormal + percOrphans + 9, 175), new Vector2(totalWidth, 180), 0xFF00FFFF);

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
                    Player.JAISeqPlayer.RootTrack.Ports[piR] = (short) piV;
                    if (piR == 9)
                    {
                        JaiSeqXLJA.sequenceTransitioning = true;
                        kk = 200;
                    }
                }
                if (Math.Sin(JAISeqPlayer.tickTimer.ElapsedMilliseconds / 100f) > 0 && JaiSeqXLJA.sequenceTransitioning)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(1, 1, 0, 1), "Waiting for sequence transition....");
                } else if (JaiSeqXLJA.sequenceTransitioning == false && kk > 0)
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


            ImGui.Begin("JaiDSP Reverb Settings");
            ImGui.SliderFloat("fDamp", ref JaiSeqXLJA.fDamp, 0, 1);
            ImGui.SliderFloat("fDryMix", ref JaiSeqXLJA.fDryMix, 0, 1);
            ImGui.SliderFloat("fRoomSize", ref JaiSeqXLJA.fRoomSize, 0, 1);
            ImGui.SliderFloat("fWetMix", ref JaiSeqXLJA.fWetMix, 0, 3);
            ImGui.SliderFloat("fWidth", ref JaiSeqXLJA.fWidth, 0, 1);
            ImGui.End();





        }

            /*
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
                    ImGui.SliderFloat("Gain Multiplier", ref Player.JAISeqPlayer.gainMultiplier, 0, 2);
                    ImGui.Checkbox("Paused", ref Player.JAISeqPlayer.paused);
                    ImGui.SliderInt("Tick Steps", ref tickSteps, 1, 3000);


                    var totalVoices = 0f;
                    var totalTracks = 0f;
                    var totalOrphans = 0;
                    for (int i = 0; i < Player.JAISeqPlayer.tracks.Length; i++)
                    {

                        if (Player.JAISeqPlayer.tracks[i] == null)
                            continue;
                        totalTracks++;
                        var w = Player.JAISeqPlayer.tracks[i];

                        totalOrphans += w.activeVoiceOrphans;
                        totalVoices += w.activeVoices + w.activeVoiceOrphans;
                    }

                    //totalVoices = ((totalVoices / totalTracks) / 7f) * 100f;
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
                    var DrawList = ImGui.GetWindowDrawList();


                    var usage = JAISeqPlayer.loadedSampleBytes / (1024);
                    ImGui.Text($"SAMPLE RAM: {usage}KB ({JAISeqPlayer.loadedSamples:D2} samples)  ");
                    DrawList.AddRectFilled(new Vector2(9, 160), new Vector2( (usage/8192f) * 200f, 165), 0xFFFF00FF);

                    var totalWidth = 330f;          

                    var percNormal = totalWidth * ((totalVoices - totalOrphans) / 64);
                    DrawList.AddRectFilled(new Vector2(9, 175), new Vector2(percNormal + 9 ,180),0xFF0000FF);


                    var percOrphans = totalWidth * ((totalOrphans) / 64f);
                    DrawList.AddRectFilled(new Vector2(percNormal + 9, 175), new Vector2( percNormal + 9 + percOrphans, 180), 0xFFFF0000);


                    DrawList.AddRectFilled(new Vector2(percNormal + percOrphans + 9, 175), new Vector2(totalWidth, 180), 0xFF00FFFF);

                    ImGui.Dummy(new Vector2(0,12.5f));
                    ImGui.Text($"DSP CHNL: {(int)totalVoices:D2}/64 total, {(int)(totalVoices - totalOrphans):D2} active, {totalOrphans:D2} orphans  \\  {(int)(totalVoices * 44100):D7}/{44100 * 7 * 16} buffer  \\  {(totalVoices / 64f) *100f,3:00.0}%%");




                }
                ImGui.End();




                ImGui.SetNextWindowPos(new Vector2(0, 200));
                ImGui.SetNextWindowSize(new Vector2(70, 600));


                ImGui.Begin("Window", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
                {
                    ImGui.Text("MUTE");
                    var ib = 0;
                    for (int i=0; i < Player.JAISeqPlayer.tracks.Length; i++)
                    {
                        if (Player.JAISeqPlayer.tracks[i] == null)
                            continue;
                        bool w = Player.JAISeqPlayer.tracks[i].muted;
                        if (Player.JAISeqPlayer.tracks[i].trackNumber == -1)
                            ImGui.Checkbox($"Root", ref w);
                        else
                        {
                            ImGui.Checkbox($"{Player.JAISeqPlayer.tracks[i].trackNumber}", ref w);
                        }

                        if (w != Player.JAISeqPlayer.tracks[i].muted)
                        {
                            Player.JAISeqPlayer.setTrackMuted(Player.JAISeqPlayer.tracks[i].trackNumber, w);
                        }
                    }
                }
                ImGui.End();


                ImGui.SetNextWindowPos(new Vector2(70, 200));
                ImGui.SetNextWindowSize(new Vector2(382, 600));

                ImGui.Begin("TrackInfo", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
                {
                    ImGui.Text($"  {"INSTRUCTION",-23} {"VOICE",-6} {"ADDRESS",-10} {"STACK"}");
                    var ib = 0;
                    for (int i = 0; i < Player.JAISeqPlayer.tracks.Length; i++)
                    {
                        if (Player.JAISeqPlayer.tracks[i] == null)
                            continue;
                        var w = Player.JAISeqPlayer.tracks[i];
                        drawFunnyThing(w);

                        foreach (JAISeqTrack trk in w.Children)
                        {
                            drawFunnyThing(trk);
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
                    ImGui.Text($"{"WAIT",-13}  VIBRATO");
                    var DrawList = ImGui.GetWindowDrawList();
                    var trackRowIndex = 0;
                    for (int trackIndex = 0; trackIndex < Player.JAISeqPlayer.tracks.Length; trackIndex++)
                    {
                        if (Player.JAISeqPlayer.tracks[trackIndex] == null)
                            continue;

                        var w = Player.JAISeqPlayer.tracks[trackIndex];
                        ImGui.Dummy(new Vector2(0.0f, 0.1f));
                        ImGui.ProgressBar((float)w.delay / (float)w.lastDelay, new Vector2(80,15), $"{w.delay:X4}/{w.lastDelay:X4}");

                        var offsetSide = w.currentVibrato * (w.activeVoices > 0 ? 1 : 0);
                        var col = 0xFFFF0000;
                        if (offsetSide != 0)
                            col = 0xFFFFCfCf;

                        DrawList.AddCircleFilled(new Vector2(590f + 19.5f * offsetSide ,235 + trackRowIndex * 23.6f), 5, col);
                        trackRowIndex++;
                    }
                }
                ImGui.End();


                ImGui.SetNextWindowPos(new Vector2(643, 200));
                ImGui.SetNextWindowSize(new Vector2(380, 600));


                ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, 0xFF0000FF);
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

                    var ib = 0;
                    for (int i = 0; i < Player.JAISeqPlayer.tracks.Length; i++)
                    {
                        if (Player.JAISeqPlayer.tracks[i] == null)
                            continue;
                        var w = Player.JAISeqPlayer.tracks[i];
                        ImGui.Dummy(new Vector2(0, 2f));
                        ImGui.ProgressBar(w.volume,new Vector2(85,13));
                        ImGui.NextColumn();
                        ImGui.Dummy(new Vector2(0, 2f));
                        ImGui.ProgressBar( w.panning/128f , new Vector2(85, 13));

                        ImGui.NextColumn();
                        ImGui.Dummy(new Vector2(0, 2f));
                        // ImGui.ProgressBar(( (w.currentPitchBend-1f) / 0.1f) + 0.5f, new Vector2(100, 13));
                        ImGui.ProgressBar( (w.pitchTarget / 0.4f) + 0.5f , new Vector2(85, 13));
                        ImGui.NextColumn();
                        ImGui.Dummy(new Vector2(0, 2f));
                        ImGui.ProgressBar(w.reverb , new Vector2(85, 13));
                        ImGui.NextColumn();

                        //w.Registers.clearChanged();
                    }

                }
                ImGui.PopStyleColor();

                ImGui.End();


                ImGui.Begin("JaiDSP Reverb Settings");
                ImGui.SliderFloat("fDamp", ref JaiSeqXLJA.fDamp, 0, 1);
                ImGui.SliderFloat("fDryMix", ref JaiSeqXLJA.fDryMix, 0, 1);
                ImGui.SliderFloat("fRoomSize", ref JaiSeqXLJA.fRoomSize, 0, 1);
                ImGui.SliderFloat("fWetMix", ref JaiSeqXLJA.fWetMix, 0, 3);
                ImGui.SliderFloat("fWidth", ref JaiSeqXLJA.fWidth,0,1);
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

            //}

        }
}
