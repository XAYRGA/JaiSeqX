using jaudio.sequence;
using JAudioStudio.jaudio.sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga.byteglider;

namespace jaudio.sequence.jv1
{
    internal class SequenceReader : ISequenceReader
    {
        private static Dictionary<byte, Type> Mapping = new Dictionary<byte, Type>()
        {
            [(byte)BMSCommandType.CALL] = typeof(Call),
            [(byte)BMSCommandType.JMP] = typeof(Jump),
            [(byte)BMSCommandType.RETURN] = typeof(Return),
            [(byte)BMSCommandType.RETURN_NOARG] = typeof(ReturnNoArg),
            [(byte)BMSCommandType.FINISH] = typeof(Finish),
            [(byte)BMSCommandType.LOOP_S] = typeof(LoopStart),
            [(byte)BMSCommandType.LOOP_E] = typeof(LoopEnd),
            [(byte)BMSCommandType.OPENTRACK] = typeof(OpenTrack),
            [(byte)BMSCommandType.CLOSETRACK] = typeof(CloseTrack),
            [(byte)BMSCommandType.IIRCUTOFF] = typeof(IIRCutoff),
            [(byte)BMSCommandType.CMD_WAIT8] = typeof(WaitCommand8),
            [(byte)BMSCommandType.CMD_WAIT16] = typeof(WaitCommand16),
            [(byte)BMSCommandType.CMD_WAITR] = typeof(WaitRegister),
            [(byte)BMSCommandType.PARAM_SET_16] = typeof(ParameterSet16),
            [(byte)BMSCommandType.PARAM_ADD_16] = typeof(ParameterAdd16),
            [(byte)BMSCommandType.SIMPLEENV] = typeof(SimpleEnvelope),
            [(byte)BMSCommandType.SETINTERRUPT] = typeof(SetInterrupt),
            [(byte)BMSCommandType.OPOVERRIDE_4] = typeof(OpOverride4),
            [(byte)BMSCommandType.OPOVERRIDE_1] = typeof(OpOverride1),
            [(byte)BMSCommandType.PRINTF] = typeof(PrintF),
            [(byte)BMSCommandType.SIMPLEOSC] = typeof(SimpleOscillator),
            [(byte)BMSCommandType.TRANSPOSE] = typeof(Transpose),
            [(byte)BMSCommandType.OSCROUTE] = typeof(OscillatorRoute),
            [(byte)BMSCommandType.VIBDEPTH] = typeof(VibratoDepth),
            [(byte)BMSCommandType.VIBDEPTHMIDI] = typeof(VibratoDepthMidi),
            [(byte)BMSCommandType.VIBPITCH] = typeof(VibratoPitch),
            [(byte)BMSCommandType.SIMPLEADSR] = typeof(SimpleADSR),
            [(byte)BMSCommandType.CLRI] = typeof(ClearInterrupt),
            [(byte)BMSCommandType.RETI] = typeof(ReturnInterrupt),
            [(byte)BMSCommandType.INTTIMER] = typeof(InterruptTimer),
            [(byte)BMSCommandType.FLUSHALL] = typeof(FlushAll),
            [(byte)BMSCommandType.READPORT] = typeof(ReadPort),
            [(byte)BMSCommandType.WRITEPORT] = typeof(WritePort),
            [(byte)BMSCommandType.CHILDWRITEPORT] = typeof(ChildWritePort),
            [(byte)BMSCommandType.PERF_S8_DUR_U16] = typeof(PERFS8DURU16),
            [(byte)BMSCommandType.PERF_S16_DUR_U16] = typeof(PERFS16DURU16),
            [(byte)BMSCommandType.PERF_S16_NODUR] = typeof(PERFS16),
            [(byte)BMSCommandType.PERF_S16_DUR_U8_9E] = typeof(PERFS16U89E),
            [(byte)BMSCommandType.PERF_S8_DUR_U8] = typeof(PERFS8DURU8),
            [(byte)BMSCommandType.PERF_S8_NODUR] = typeof(PERFS8),
            [(byte)BMSCommandType.PERF_U8_NODUR] = typeof(PERFU8),
            [(byte)BMSCommandType.PARAM_SET_R] = typeof(ParameterSetRegister),
            [(byte)BMSCommandType.PARAM_ADD_R] = typeof(ParameterAddRegister),
            [(byte)BMSCommandType.PARAM_SET_8] = typeof(ParameterSet8),
            [(byte)BMSCommandType.PARAM_ADD_8] = typeof(ParameterAdd8),
            [(byte)BMSCommandType.PARAM_MUL_8] = typeof(ParameterMultiply8),
            [(byte)BMSCommandType.PARAM_CMP_8] = typeof(ParameterCompare8),
            [(byte)BMSCommandType.PARAM_CMP_16] = typeof(ParameterCompare16),
            [(byte)BMSCommandType.PARAM_CMP_R] = typeof(ParameterCompareRegister),
            [(byte)BMSCommandType.SETPARAM_90] = typeof(ParameterSet8_90),
            //
            [(byte)BMSCommandType.SETPARAM_91] = typeof(ParameterSet16_91),
            [(byte)BMSCommandType.SETPARAM_92] = typeof(ParameterSet16_92),
            [(byte)BMSCommandType.SETLASTNOTE] = typeof(SetLastNote),
            [(byte)BMSCommandType.PARAM_BITWISE] = typeof(ParamBitwise),
            [(byte)BMSCommandType.SYNCCPU] = typeof(SyncCpu),
            [(byte)BMSCommandType.TEMPO] = typeof(Tempo),
            [(byte)BMSCommandType.TIMEBASE] = typeof(Timebase),
            [(byte)BMSCommandType.PANSWSET] = typeof(PanSweepSet),
            [(byte)BMSCommandType.PANPOWSET] = typeof(PanPowerSet),
            [(byte)BMSCommandType.BUSCONNECT] = typeof(BusConnect),
            [(byte)BMSCommandType.OUTSWITCH] = typeof(OutSwitch),
            [(byte)BMSCommandType.PARAM_SUBTRACT] = typeof(ParameterSubtract),
            [(byte)BMSCommandType.CHECKPORTIMPORT] = typeof(CheckPortImport),
            [(byte)BMSCommandType.TIMERELATE_JV0] = typeof(TimeRelateJV0),
            [(byte)BMSCommandType.IIRSET] = typeof(IIRSet),
            [(byte)BMSCommandType.VOLUMEMODE] = typeof(VolumeMode),
            [0x89] = typeof(IIRSet),
            [0x8A] = typeof(IIRSet),
            [(byte)BMSCommandType.PERF_U8_DUR_U8] = typeof(PERFU8DURU8),
            [(byte)BMSCommandType.PERF_S16_DUR_U8] = typeof(PERFS16DURU8)
        };
        public SequenceReader()
        {
            InstructionMapping = Mapping;
        }

        public override SequenceCommand readNextCommand(bgReader reader)
        {
            var origAddress = reader.BaseStream.Position;
            var opcode = reader.ReadByte();
            SequenceCommand outputCommand;


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
                cmd.Voice = (byte)((opcode & 0xF) - 1); // -1;
                cmd.read(reader);
                outputCommand = cmd;
            }
            else
            {
                var opcodeType = InstructionMapping[opcode];
                if (opcodeType == null)
                    throw new Exception($"0x{reader.BaseStream.Position:X5} Opcode not implemented 0x{opcode:X} {(BMSCommandType)opcode}");

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                outputCommand = (SequenceCommand)Activator.CreateInstance(opcodeType);
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
