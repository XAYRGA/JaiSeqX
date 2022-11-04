using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using jaudio.bms.v1;

namespace jaudio.bms
{
    internal class bmsparser
    {
        public static Type[] OpcodeToClass = new Type[0x100];
        public bmsparser()
        {
            OpcodeToClass[(byte)BMSCommandType.CALL] = typeof(Call);
            OpcodeToClass[(byte)BMSCommandType.JMP] = typeof(Jump);
            OpcodeToClass[(byte)BMSCommandType.RETURN] = typeof(Return);
            OpcodeToClass[(byte)BMSCommandType.RETURN_NOARG] = typeof(ReturnNoArg);
            OpcodeToClass[(byte)BMSCommandType.FINISH] = typeof(Finish);
            OpcodeToClass[(byte)BMSCommandType.LOOP_S] = typeof(LoopStart);
            OpcodeToClass[(byte)BMSCommandType.LOOP_E] = typeof(LoopEnd);
            OpcodeToClass[(byte)BMSCommandType.OPENTRACK] = typeof(OpenTrack);
            OpcodeToClass[(byte)BMSCommandType.CLOSETRACK] = typeof(CloseTrack);
            OpcodeToClass[(byte)BMSCommandType.IIRCUTOFF] = typeof(IIRCutoff);
            OpcodeToClass[(byte)BMSCommandType.CMD_WAIT8] = typeof(WaitCommand8);
            OpcodeToClass[(byte)BMSCommandType.CMD_WAIT16] = typeof(WaitCommand16);
            OpcodeToClass[(byte)BMSCommandType.CMD_WAITR] = typeof(WaitRegister);
            OpcodeToClass[(byte)BMSCommandType.PARAM_SET_16] = typeof(ParameterSet16);
            OpcodeToClass[(byte)BMSCommandType.PARAM_ADD_16] = typeof(ParameterAdd16);
            OpcodeToClass[(byte)BMSCommandType.SIMPLEENV] = typeof(SimpleEnvelope);
            OpcodeToClass[(byte)BMSCommandType.SETINTERRUPT] = typeof(SetInterrupt);
            OpcodeToClass[(byte)BMSCommandType.OPOVERRIDE_4] = typeof(OpOverride4);
            OpcodeToClass[(byte)BMSCommandType.OPOVERRIDE_1] = typeof(OpOverride1);
            OpcodeToClass[(byte)BMSCommandType.PRINTF] = typeof(PrintF);
            OpcodeToClass[(byte)BMSCommandType.SIMPLEOSC] = typeof(SimpleOscillator);
            OpcodeToClass[(byte)BMSCommandType.TRANSPOSE] = typeof(Transpose);
            OpcodeToClass[(byte)BMSCommandType.OSCROUTE] = typeof(OscillatorRoute);
            OpcodeToClass[(byte)BMSCommandType.VIBDEPTH] = typeof(VibratoDepth);
            OpcodeToClass[(byte)BMSCommandType.VIBDEPTHMIDI] = typeof(VibratoDepthMidi);
            OpcodeToClass[(byte)BMSCommandType.VIBPITCH] = typeof(VibratoPitch);
            OpcodeToClass[(byte)BMSCommandType.SIMPLEADSR] = typeof(SimpleADSR);
            OpcodeToClass[(byte)BMSCommandType.CLRI] = typeof(ClearInterrupt);
            OpcodeToClass[(byte)BMSCommandType.RETI] = typeof(ReturnInterrupt);
            OpcodeToClass[(byte)BMSCommandType.FLUSHALL] = typeof(FlushAll);
            OpcodeToClass[(byte)BMSCommandType.READPORT] = typeof(ReadPort);
            OpcodeToClass[(byte)BMSCommandType.WRITEPORT] = typeof(WritePort);
            OpcodeToClass[(byte)BMSCommandType.CHILDWRITEPORT] = typeof(ChildWritePort);
            OpcodeToClass[(byte)BMSCommandType.PERF_S8_DUR_U16] = typeof(PERFS8DURU16);
            OpcodeToClass[(byte)BMSCommandType.PERF_S16_NODUR] = typeof(PERFS16);
            OpcodeToClass[(byte)BMSCommandType.PERF_S16_DUR_U8_9E] = typeof(PERFS16U89E);
            OpcodeToClass[(byte)BMSCommandType.PERF_S8_DUR_U8] = typeof(PERFS8DURU8);
            OpcodeToClass[(byte)BMSCommandType.PERF_S8_NODUR] = typeof(PERFS8);
            OpcodeToClass[(byte)BMSCommandType.PERF_U8_NODUR] = typeof(PERFU8);
            OpcodeToClass[(byte)BMSCommandType.PARAM_SET_R] = typeof(ParameterSetRegister);
            OpcodeToClass[(byte)BMSCommandType.PARAM_ADD_R] = typeof(ParameterAddRegister);
            OpcodeToClass[(byte)BMSCommandType.PARAM_SET_8] = typeof(ParameterSet8);
            OpcodeToClass[(byte)BMSCommandType.PARAM_ADD_8] = typeof(ParameterAdd8);
            OpcodeToClass[(byte)BMSCommandType.PARAM_MUL_8] = typeof(ParameterMultiply8);
            OpcodeToClass[(byte)BMSCommandType.PARAM_CMP_8] = typeof(ParameterCompare8);
            OpcodeToClass[(byte)BMSCommandType.PARAM_CMP_R] = typeof(ParameterCompareRegister);
            OpcodeToClass[(byte)BMSCommandType.SETPARAM_90] = typeof(ParameterSet8_90);
            OpcodeToClass[(byte)BMSCommandType.SETPARAM_92] = typeof(ParameterSet16_92);
            OpcodeToClass[(byte)BMSCommandType.SETLASTNOTE] = typeof(SetLastNote);
            OpcodeToClass[(byte)BMSCommandType.PARAM_BITWISE] = typeof(ParamBitwise);
            OpcodeToClass[(byte)BMSCommandType.SYNCCPU] = typeof(SyncCpu);
            OpcodeToClass[(byte)BMSCommandType.TEMPO] = typeof(Tempo);
            OpcodeToClass[(byte)BMSCommandType.TIMEBASE] = typeof(Timebase);
            OpcodeToClass[(byte)BMSCommandType.PANSWSET] = typeof(PanSweepSet);
            OpcodeToClass[(byte)BMSCommandType.PANPOWSET] = typeof(PanPowerSet);
            OpcodeToClass[(byte)BMSCommandType.BUSCONNECT] = typeof(BusConnect);
            OpcodeToClass[(byte)BMSCommandType.OUTSWITCH] = typeof(OutSwitch);
            OpcodeToClass[(byte)BMSCommandType.PARAM_SUBTRACT] = typeof(ParameterSubtract);
        }

        public bmscommand readNextCommand(BeBinaryReader reader)
        {
            var origAddress = reader.BaseStream.Position;
            var opcode = reader.ReadByte();
            bmscommand outputCommand;
       
            if (opcode < 0x80)
            {
                var cmd = new NoteOnCommand();
                cmd.Note = opcode;
                cmd.read(reader);
                outputCommand = cmd;
            }
            else if (opcode >= 0x81 && opcode < 0x88)
            {
                var cmd = new NoteOffCommand();
                cmd.Voice = (byte)(opcode & 0xF); // -1;
                cmd.read(reader);
                outputCommand = cmd;
            } else
            {
                var opcodeType = OpcodeToClass[opcode];
                if (opcodeType == null)
                    throw new Exception($"0x{reader.BaseStream.Position:X5} Opcode not implemented 0x{opcode:X} {(BMSCommandType)opcode}");

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                outputCommand = (bmscommand)Activator.CreateInstance(opcodeType);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                if (outputCommand == null)
                    throw new Exception($"Failed to create instance of 0x{opcode:X} {(BMSCommandType)opcode}");
                outputCommand.read(reader);
            }
            outputCommand.OriginalAddress = (int)origAddress;
            return outputCommand;
        }
    }
}
