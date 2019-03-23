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
        int lineID;

        public JASMConverter(string bmsfile,string outfile)
        {
            BMSData = File.ReadAllBytes(bmsfile);
            OpMap = new int[BMSData.Length];

            subs = new Subroutine[32];
            JASMData = new string[32];


            var root = new Subroutine(ref BMSData, 0x000000); // root track
            AddJASMSequence(root,0);
            for (int i = 1; i < trackid + 1 ; i++)
            {
                AddJASMSequence(subs[i], i);
            }

            File.WriteAllText("test.jasm", outdata);
           
            
        }

        public void pushJASMString(string data)
        {
            lineID++;
            outdata += data + "\n";
        }


        private void AddJASMSequence(Subroutine sub, int id)
        {
            pushJASMString(":TRK" + id);
            JaiEventType lastOp = JaiEventType.UNKNOWN;
            while (lastOp!=JaiEventType.JUMP & lastOp!=JaiEventType.HALT)
            {
                lastOp = sub.loadNextOp();
                var state = sub.State;
                OpMap[sub.State.current_address] = lineID;
                
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
