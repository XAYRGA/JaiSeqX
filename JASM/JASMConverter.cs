using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Seq;
using JaiSeqX.JAI;
using System.IO;

namespace JaiSeqX.JASM
{
    class JASMConverter
    {
        byte[] BMSData;
        int trackid;

        Subroutine[] subs;
        string[] JASMData;

        string outdata;
        int[] OpMap;
        int lineMax;
        int jumpPosID;
        int[] jumps;

        string JaiFun = "";
        string[] JaiFuncs;
        int JaiFunID = 1;
        int[] JaiFunMap;

        public JASMConverter(string bmsfile, string outfile)
        {
            BMSData = File.ReadAllBytes(bmsfile);
            OpMap = new int[BMSData.Length];
            jumps = new int[BMSData.Length];
            JaiFunMap = new int[BMSData.Length];
            JASMData = new string[BMSData.Length + 0xFFF]; // huge string array. 
            JaiFuncs = new string[16384];

            var root = new Subroutine(ref BMSData, 0x000000); // root track
            subs = new Subroutine[64];
            subs[0] = root;

            AddJASMSequence(root,0);

            var b = File.OpenWrite(outfile);

              for (int i = 1; i < trackid + 1 ; i++)
              {
                     AddJASMSequence(subs[i], i);
              }

              for (int i=0; i < JASMData.Length; i++)
            {
                var bx = JASMData[i];
                if (bx!=null && bx.Length > 2)
                {
                    string add = "";
                    if (jumps[i]!=0)
                    {
                        add = ":JPOS" + jumps[i] + "\n";
                    }
                    var asd = Encoding.ASCII.GetBytes(add + JASMData[i]);
                    b.Write(asd, 0, asd.Length);
                }
            }
              for (int i=1; i < JaiFunID ;i++)
            {
                var asd = Encoding.ASCII.GetBytes(JaiFuncs[i]);
                b.Write(asd, 0, asd.Length);
            }

        }

        public void addJump(int line)
        {
            jumpPosID++;
            jumps[line] = jumpPosID;
        }


        public void pushJASMString(string data)
        {
            lineMax++;
            JASMData[lineMax] = data + "\n";
        }


        private void AddJASMCall(Subroutine sub)
        {

            JaiEventType lastOp = JaiEventType.UNKNOWN;
        
            var JaiFunL = "";
            JaiFunL += ":JFUN" + JaiFunID  + "\n";

            while (lastOp!=JaiEventType.RET)
            {
                lastOp = sub.loadNextOp();
                var state = sub.State;
                switch (lastOp)
                {
                    case JaiEventType.DELAY:
                        {
                            JaiFunL += ("DEL " + state.delay) + "\n";
                            break;
                        }
               
                    case JaiEventType.BANK_CHANGE:
                        {
                            JaiFunL += ("BCN " + state.voice_bank) + "\n";
                            break;
                        }
                    case JaiEventType.NOTE_OFF:
                        {
                            JaiFunL += ("NOF " + state.voice) + "\n";
                            break;

                        }
                    case JaiEventType.NOTE_ON:
                        {
                            JaiFunL += ("NON " + state.voice + "," + state.note + "," + state.vel) + "\n";
                            break;
                        }
                    case JaiEventType.PARAM:
                        {
                            JaiFunL +=  ("PAR " + state.param + "," + state.param_value) + "\n";
                            break;
                        }
                    case JaiEventType.PAUSE:
                    case JaiEventType.UNKNOWN:
                        {
                            JaiFunL += ("NOP") + "\n";
                            break;
                        }
                    case JaiEventType.PROG_CHANGE:
                        {
                            JaiFunL +=  ("PRC " + state.voice_program) + "\n";
                            break;
                        }
                    case JaiEventType.PERF:
                        {
                            JaiFunL += ("PRF " + state.perf + "," + state.perf_value + "," + state.perf_duration) + "\n";

                            break;
                        }
                    case JaiEventType.TIME_BASE:
                        {
                            JaiFunL += ("TBS " + state.bpm + "," + state.ppqn) + "\n";
                            break;
                        }
                

                    case JaiEventType.HALT:
                        {
                            JaiFunL += ("HAL") + "\n";
                            break;
                        }
                    case JaiEventType.RET:
                        {
                            JaiFunL += ("RET") + "\n";
                            break;
                        }
                    default:
                        {
                            JaiFunL += ("NOP") + "\n";
                            break;
                        }
                }

            }
            //Console.WriteLine(JaiFunL);
            JaiFuncs[JaiFunID] = JaiFunL;
            JaiFunID++;

        }

        private void AddJASMSequence(Subroutine sub, int id)
        {
            pushJASMString(":TRK" + id);
            JaiEventType lastOp = JaiEventType.UNKNOWN;
            while (lastOp != JaiEventType.JUMP & lastOp != JaiEventType.HALT)
            {
                lastOp = sub.loadNextOp();
                var state = sub.State;
                OpMap[sub.State.current_address] = lineMax;

                switch (lastOp)
                {
                    case JaiEventType.DELAY:
                        {
                            pushJASMString("DEL " + state.delay);
                            break;
                        }
                    case JaiEventType.NEW_TRACK:
                        {
                            trackid++;
                            pushJASMString("NTK TRK" + trackid);
                            subs[trackid] = new Subroutine(ref BMSData, state.track_address);
                            break;
                        }
                    case JaiEventType.BANK_CHANGE:
                        {
                            pushJASMString("BCN " + state.voice_bank);
                            break;
                        }
                    case JaiEventType.NOTE_OFF:
                        {
                            pushJASMString("NOF " + state.voice);
                            break;

                        }
                    case JaiEventType.NOTE_ON:
                        {
                            pushJASMString("NON " + state.voice + "," + state.note + "," + state.vel);
                            break;
                        }
                    case JaiEventType.PARAM:
                        {
                            pushJASMString("PAR " + state.param + "," + state.param_value);
                            break;
                        }
                    case JaiEventType.PAUSE:
                    case JaiEventType.UNKNOWN:
                        {
                            pushJASMString("NOP");
                            break;
                        }
                    case JaiEventType.PROG_CHANGE:
                        {
                            pushJASMString("PRC " + state.voice_program);
                            break;
                        }
                    case JaiEventType.PERF:
                        {
                            pushJASMString("PRF " + state.perf + "," + state.perf_value + "," + state.perf_duration);

                            break;
                        }
                    case JaiEventType.TIME_BASE:
                        {
                            pushJASMString("TBS " + state.bpm + "," + state.ppqn);
                            break;
                        }
                    case JaiEventType.JUMP:
                        {
                 
                            var addr = state.jump_address;
                            var line = OpMap[addr];
                           
                            
                            addJump(line);
                            pushJASMString("JMP JPOS" + jumpPosID);
                            break;
                        }
                    case JaiEventType.CALL:
                        {
                            var sub_re = sub.nextOpAddress();
                            if (JaiFunMap[state.jump_address] == 0)
                            {
                                sub.jump(state.jump_address);
                                AddJASMCall(sub);
                                pushJASMString("CAL JFUN" + (JaiFunID - 1));
                                JaiFunMap[state.jump_address] = JaiFunID - 1;
                                sub.jump(sub_re);
                            } else
                            {
                                pushJASMString("CAL JFUN" + JaiFunMap[state.jump_address]);
                            }


                            break;
                        }
                    
                    case JaiEventType.HALT:
                        {
                            pushJASMString("HAL");
                            break;
                        }
                    default:
                        {
                            pushJASMString("NOP");
                            break;
                        }
                }
            }
        }
    }

}

