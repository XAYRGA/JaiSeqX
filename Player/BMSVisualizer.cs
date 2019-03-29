using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SdlDotNet.Core;
using SdlDotNet.Graphics;
using System.Drawing;
using System.Runtime;
using System.Runtime.InteropServices;
using SdlDotNet.Input;

namespace JaiSeqX.Player
{
    static class BMSVisualizer
    {
    
        static Thread RenderThread;
        static Surface IVideoSurface;
        static SdlDotNet.Graphics.Font myfont;
        public static void Init()
        {

            Events.TargetFps = 60;
            Events.Quit += (QuitEventHandler);
            Events.Tick += (draw);
            Events.KeyboardDown += KeyDown;
            
            myfont = new SdlDotNet.Graphics.Font("tahoma.ttf", 12);
            RenderThread = new Thread(new ThreadStart(startDrawWindowThread));
            
            RenderThread.Start();
        }

        private static void startDrawWindowThread()
        {
            IVideoSurface = Video.SetVideoMode(800, 800, 16, false, false, false, true);
            Events.Run();
        }
        private static void KeyDown(object sender, KeyboardEventArgs kbe) 
        {
            
            var channel = (byte)kbe.Key - 97; // "A"
            
            if (channel > 0 & channel < BMSPlayer.mutes.Length)
            {
                BMSPlayer.mutes[channel] = !BMSPlayer.mutes[channel];
                if (BMSPlayer.mutes[channel])
                {
                    BMSPlayer.ChannelManager.silenceChannel((byte)channel);
                }
            }
        }

        private static void draw(object sender, TickEventArgs args)
        {
            IVideoSurface.Fill(Color.Black);
            
            Point HeaderPos = new Point(5, 5);
            var HeaderText = myfont.Render("JaiSeqX by XayrGA ", Color.White);
            Video.Screen.Blit(HeaderText, HeaderPos);
            HeaderText.Dispose();
          
            for (int i = 0; i < BMSPlayer.subroutine_count; i++)
            {
                Point dest = new Point(5, (i * 35) + 30);
                Point dest2 = new Point(5, (i * 35) + 42);

                new SdlDotNet.Graphics.Primitives.Box(new Point(0, (i * 35) + 25) , new Size(800, 2)).Draw(IVideoSurface, Color.White, false, true);
                

                Surface surfVar;
                Surface surfVar2 = myfont.Render("", Color.Red);

                if (BMSPlayer.halts[i]==true)
                {

          
                    
                    surfVar = myfont.Render("Track " + i + ": HALTED", Color.Red);

                    
                } else if(BMSPlayer.mutes[i] == true)
                    {

                        surfVar = myfont.Render("Track " + i + ": MUTED", Color.Yellow);


                    }
                else
                {
                    var col = Color.White;
                    if (BMSPlayer.updated[i] > 0)
                    {
                        BMSPlayer.updated[i]--;
                        col = Color.Green;
                    }
                    var CSubState = BMSPlayer.subroutines[i].State;
                    //var text2 = string.Format("Vel {3:X} | Note {0:X}, Bank {1:X}, Program {2:X} Delay {4} ActiveVoices {5}",CSubState.note,CSubState.voice_bank,CSubState.voice_program,CSubState.vel,CSubState.delay,BMSPlayer.ChannelManager.channels[i].ActiveVoices);
                    var text2 = string.Format("DEL {0}",CSubState.delay);
                    var text = string.Format("0x{0:X}", CSubState.current_address);
                    surfVar = myfont.Render("Track " + i + " (" + text + ")" , col);
                    surfVar2 = myfont.Render(text2, col);

                    for (int sub = 0; sub < BMSPlayer.ChannelManager.channels[i].ActiveVoices; sub++)
                    {
                        var sdest = dest;
                        sdest.Offset(140 + 27 * sub,0 );
                        var wtf = new SdlDotNet.Graphics.Primitives.Box(sdest, new Size(25, 25));
                        wtf.Draw(IVideoSurface, Color.Red, false, true);
                    }
                    

                }

                new SdlDotNet.Graphics.Primitives.Box(new Point(135, 0), new Size(2, 800)).Draw(IVideoSurface, Color.White, false, true);



                Video.Screen.Blit(surfVar, dest);
                Video.Screen.Blit(surfVar2, dest2);
                surfVar.Dispose();
                surfVar2.Dispose();

            }
          
          

            IVideoSurface.Update();
        }
        private static void QuitEventHandler(object sender, QuitEventArgs args)
        {
            Environment.Exit(0x00);
            Events.QuitApplication();

        }




    }
}
