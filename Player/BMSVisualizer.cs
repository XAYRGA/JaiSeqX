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
            myfont = new SdlDotNet.Graphics.Font("tahoma.ttf", 12);
            RenderThread = new Thread(new ThreadStart(startDrawWindowThread));
            RenderThread.Start();
        }

        private static void startDrawWindowThread()
        {
            IVideoSurface = Video.SetVideoMode(640, 480, 16, false, false, false, true);
            Events.Run();
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
                Point dest = new Point(5, (i * 25) + 50);
                Surface surfVar;
                
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
                    var text = string.Format(" Vel {3:X} | Note {0:X}, Bank {1:X}, Program {2:X} Delay {4} ActiveVoices {5}",CSubState.note,CSubState.voice_bank,CSubState.voice_program,CSubState.vel,CSubState.delay,BMSPlayer.ChannelManager.channels[i].ActiveVoices);
                    surfVar = myfont.Render("Track " + i + ":" + text, col);
                }

                Video.Screen.Blit(surfVar, dest);
                surfVar.Dispose();

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
