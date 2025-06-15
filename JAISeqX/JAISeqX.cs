using JAISeqX.cube.rarc;
using jaudio;
using jaudio.instrument;
using xayrga.byteglider;

namespace JAISeqX
{
    internal class JAISeqX
    {
        public static Dictionary<int,JWaveSystem> waveSystems = new Dictionary<int,JWaveSystem>();
        public static Dictionary<int, JInstrumentBank> Instruments = new Dictionary<int,JInstrumentBank>(); 
        public static Dictionary<int, Dictionary<int, jdsp.SoundBuffer>> sounds = new();
        static void Main(string[] args)
        {
            var fileHnd = File.OpenRead("JaiInit.aaf");
            var reader = new bgReader(fileHnd);
            var aafile = new jaudio.AudioArchiveFile();
            jdsp.jdsp.init();
            aafile.load(reader);

            foreach (AudioArchiveSectionInfo sect in aafile.Sections)
                switch(sect.type)
                {
                    case AAFChunkType.WSYS:
                        {
                            var item = new WaveSystemDeserializer().readBank(sect.reader);
                            Console.WriteLine(item.ID);
                            waveSystems[item.ID] = item;
                            break;
                        }
                    case AAFChunkType.IBNK:
                        {
                            var item = new InstrumentBankV1Deserializer(sect.reader).readBank();
                            Instruments[item.ID] = item;
                            break;
                        }
                }

            foreach(JWaveSystem wsys in waveSystems.Values)
            {
                Console.WriteLine($"WSYS {wsys.ID}");
                var current = sounds[wsys.ID] = new Dictionary<int, jdsp.SoundBuffer>();
                foreach (JWaveSystem.Scene scene in wsys.Scenes)
                {
                    Console.WriteLine($"\tChecking scene {scene.AWName}");
                    var fHnd = File.OpenRead($"Banks/{scene.AWName}");
                    var fRead = new bgReader(fHnd);
                   
                    foreach (int wave in scene.Waves)
                    {
                        if (current.ContainsKey(wave))
                            continue;

                        var waveInfo = wsys.Waves[wave];
                        fRead.Seek(waveInfo.Offset);
                        var fBuff = fRead.ReadBytes(waveInfo.Length);
                        var waveBuffer = current[wave] = new jdsp.SoundBuffer(waveInfo, fBuff);
                        waveBuffer.EncodeBuffer();
                        Console.WriteLine($"\t\tLoading waveID {wave} from {scene.AWName}");
                    }
                }               
            }
            var sRef = sounds[0][1];
            var VoiceTest = new jdsp.Voice(ref sRef);
            VoiceTest.setAttackRelease(1500, 0);
            VoiceTest.Play();
            while (true)
            {
                VoiceTest.updateVoice(1);
                Thread.Sleep(1);
            }
            
      
           
            Console.ReadKey();
        
        }
    }
}
