using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga.byteglider;
using jaudio.sequence;
using jaudio.sequence.jv1;
using Un4seen.BassAsio;

namespace JAISeqX.player
{
    internal class SequenceTrack
    {

        public int Delay = 0;      

        public Dictionary<int, SequenceTrack> Children = new();

        public int[] Registers = new int[32];
        public int[] Ports = new int[32];

        private SequenceTrack? _parent;
        private SequencePlayer _player;
        private TrackChannel _channel;

        private int InitialAddress;

        bgReader sequenceDataReader;
        byte[] sequenceData;
        ISequenceReader Interpreter;


        public SequenceTrack(SequencePlayer player, SequenceTrack? Parent, ref byte[] buffer, int Offset)
        {
            _parent = Parent;
            _player = player;
            InitialAddress = Offset;
            sequenceData = buffer;
            sequenceDataReader = new bgReader(new MemoryStream(buffer));
            Interpreter = new SequenceReader();
            _channel = new TrackChannel(_player);
        }

        public void update(double deltaTime)
        {
          _channel.updateDSP(deltaTime);
           foreach (var child in Children.Values)
                child.update(deltaTime);
        }
        public void tick()
        {
            _channel.tick();
            foreach (var child in Children.Values) 
                child.tick();
            Delay--;
            if (Delay > 0)
                return;

            var Instruction = Interpreter.readNextCommand(sequenceDataReader);

        }

        public void destroy()
        {
            destroyChildren();
        }

        private void destroyChildren()
        {
            foreach (SequenceTrack child in Children.Values)
                child.destroy();       
        }
    }
}
