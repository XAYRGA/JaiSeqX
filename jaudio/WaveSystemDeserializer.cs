using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using xayrga.byteglider;
using jaudio;
using System.Text.RegularExpressions;

///// THIS IS TACKED ON OLD CODE //////
///// THE CODE IS NICE BUT IT IS NOT DESIGNED FOR THIS INTERFACE ////

namespace jaudio
{
    public class WaveSystemDeserializer
    {

		private JWaveSystem bank;

        private const int WSYS = 0x57535953;
        private const int WINF = 0x57494E46;
        private const int WBCT = 0x57424354;
        private int id;
        private int total_sounds;

        private WSYSScene[] Scenes;
        private WSYSGroup[] Groups;

        internal int mBaseAddress = 0;
		

		public WaveSystemDeserializer()
		{
			bank = new JWaveSystem();
		}
		public JWaveSystem readBank(bgReader reader,string awPath)
		{

			loadFromStream(reader);			
			bank.ID = reader.ReadInt32();

			for (int i = 0; i < Scenes.Length; i++)
			{
				var Scene = Scenes[i];
				var Group = Groups[i];

				var NewWSYSScene = new JWaveSystem.Scene();
				NewWSYSScene.AWName = Group.awPath;

				if (Scene.DEFAULT.Length != Group.waves.Length)
					throw new Exception("Well, shit.");

				// C-ST (STATIC) and C-EX (EXTENDED) aren't used.
				// There's not even code in the executable to load them.
				// We're only going to worry about the default scene.
				for (int j = 0; j < Scene.DEFAULT.Length; j++)
				{	
					var WaveSceneInfo = Scene.DEFAULT[j];
					var Wave = Group.waves[j];

					if (WaveSceneInfo.GroupID != i)
						throw new Exception("God dammit");

					var WaveID = WaveSceneInfo.WaveID;

					if (!bank.Waves.ContainsKey(WaveID))
					{
						var newWave = new JWaveSystem.Wave()
						{
							Format = (JWaveSystem.Wave.EWaveFormat)Wave.format,
							Key = Wave.key,
							SampleRate = Wave.sampleRate,
							SampleCount = Wave.sampleCount,	

							Loop = Wave.loop,
							LoopStart = Wave.loop_start,
							LoopEnd = Wave.loop_end,

							Last = Wave.last,
							Penult = Wave.penult,
						};

						//todo: project reference folder.
						if (File.Exists($"{awPath}/{Group.awPath}"))
						{
							var FileHnd = File.OpenRead($"{awPath}/{Group.awPath}");
							FileHnd.Position = Wave.awOffset;
							newWave.CurrentBuffer = new byte[Wave.awLength];
							FileHnd.Read(newWave.CurrentBuffer, 0, Wave.awLength);
							FileHnd.Flush();
							FileHnd.Close();
						}
						else
							Console.WriteLine($"Unable to load buffer data for wave {WaveID} from Waves/{Group.awPath}");
						
						bank.Waves[WaveID] = newWave;
					}
					NewWSYSScene.Waves.Add(WaveID);				
				}
			}

			return bank;
		}

        private void loadWinf(bgReader rd)
        {
            if (rd.ReadInt32BE() != WINF)
                throw new Exception("WINF corrupt");
            var count = rd.ReadInt32BE();
            var ptrs = rd.ReadInt32ArrayBE(count);
            Groups = new WSYSGroup[count];
            for (int i = 0; i < count; i++)
            {
                rd.BaseStream.Position = ptrs[i];
                Groups[i] = WSYSGroup.CreateFromStream(rd);
            }
        }

        private void loadWbct(bgReader rd)
        {
            if (rd.ReadInt32BE() != WBCT)
                throw new Exception("WBCT corrupt");
            rd.ReadInt32BE(); // Empty?
            var count = rd.ReadInt32BE();
            var ptrs = rd.ReadInt32ArrayBE(count);
            Scenes = new WSYSScene[count];
            for (int i = 0; i < count; i++)
            {
                rd.BaseStream.Position = ptrs[i];
                Scenes[i] = WSYSScene.CreateFromStream(rd);
            }
        }

        private void loadFromStream(bgReader rd)
        {
            if (rd.ReadInt32BE() != WSYS)
                throw new InvalidDataException("Couldn't match WSYS header!");
            var size = rd.ReadInt32BE();
            id = rd.ReadInt32BE();
            total_sounds = rd.ReadInt32BE();

            var winfOffset = rd.ReadInt32BE();
            var wbctOffset = rd.ReadInt32BE();

            rd.BaseStream.Position = winfOffset;
            loadWinf(rd);

            rd.BaseStream.Position = wbctOffset;
            loadWbct(rd);
        }


		public class WSYSScene
		{

			private const int SCNE = 0x53434E45;

			private const int C_DF = 0x432D4446;
			private const int C_EX = 0x432D4558;
			private const int C_ST = 0x432D5354;

			public WSYSWaveID[] DEFAULT;
			public WSYSWaveID[] EXTENDED;
			public WSYSWaveID[] STATIC;

			internal int mBaseAddress = 0;

			private WSYSWaveID[] loadContainer(bgReader rd, int type)
			{
				var inType = rd.ReadInt32BE();
				if (inType != type)
					throw new Exception($"Unexpected section type {type:X} != {inType:X}");
				var count = rd.ReadInt32BE();
				var waves = new WSYSWaveID[count];
				var offsets = rd.ReadInt32ArrayBE(count);
				for (int i = 0; i < count; i++)
				{
					rd.BaseStream.Position = offsets[i];
					waves[i] = WSYSWaveID.CreateFromStream(rd);
				}
				return waves;
			}


			public static WSYSScene CreateFromStream(bgReader rd)
			{
				var b = new WSYSScene();
				b.loadFromStream(rd);
				return b;
			}


			private void loadFromStream(bgReader rd)
			{
				if (rd.ReadInt32BE() != SCNE)
					throw new Exception("SCNE corrupt");
				rd.ReadUInt64(); // Padding? Something???? Always zero.
				var cdfOffset = rd.ReadInt32BE();
				var cexOffset = rd.ReadInt32BE();
				var cstOffset = rd.ReadInt32BE();

				rd.BaseStream.Position = cdfOffset;
				DEFAULT = loadContainer(rd, C_DF);
				rd.BaseStream.Position = cexOffset;
				EXTENDED = loadContainer(rd, C_EX);
				rd.BaseStream.Position = cstOffset;
				STATIC = loadContainer(rd, C_ST);
			}


		}


		public class WSYSGroup
		{
			public string awPath;
			public WSYSWave[] waves;

			internal int mBaseAddress = 0;

			public static WSYSGroup CreateFromStream(bgReader rd)
			{
				var b = new WSYSGroup();
				b.loadFromStream(rd);
				return b;
			}

			private void loadFromStream(bgReader rd)
			{
				awPath = "";
				var stringBuff = rd.ReadBytes(0x70);
				for (int i = 0; i < 0x70; i++)
					if (stringBuff[i] != 0)
						awPath += (char)stringBuff[i];
					else
						break;

				var count = rd.ReadInt32BE();
				var ptrs = rd.ReadInt32ArrayBE(count);
				waves = new WSYSWave[ptrs.Length];
				for (int i = 0; i < ptrs.Length; i++)
				{
					rd.BaseStream.Position = ptrs[i];
					waves[i] = WSYSWave.CreateFromStream(rd);
				}
			}

		}


		public class WSYSWaveID
		{
			public short GroupID;
			public short WaveID;

			internal int mBaseAddress = 0;


			public void loadFromStream(bgReader rd)
			{
				GroupID = rd.ReadInt16BE();
				WaveID = rd.ReadInt16BE();
				rd.ReadInt32BE(); // CCCCCCCC
				rd.ReadInt32BE(); // FFFFFFFF
			}
			public static WSYSWaveID CreateFromStream(bgReader rd)
			{
				var b = new WSYSWaveID();
				b.loadFromStream(rd);
				return b;
			}
		}


		public struct WSYSWave
		{
			public byte format = 0;
			public byte key = 0;
			public float sampleRate = 0;
			public int sampleCount = 0;

			public int awOffset = 0;
			public int awLength = 0;

			public bool loop = false;
			public int loop_start = 0;
			public int loop_end = 0;

			public short last = 0;
			public short penult = 0;


			internal int mBaseAddress = 0;

			public WSYSWave() { }

			public void loadFromStream(bgReader rd)
			{
				rd.ReadByte(); // Empty.
				format = rd.ReadByte();
				key = rd.ReadByte();
				rd.ReadByte(); // empty. 
				sampleRate = rd.ReadSingleBE();
				awOffset = rd.ReadInt32BE();
				awLength = rd.ReadInt32BE();
				loop = rd.ReadUInt32() == 0xFFFFFFFF;
				loop_start = rd.ReadInt32BE();
				loop_end = rd.ReadInt32BE();
				sampleCount = rd.ReadInt32BE();
				last = rd.ReadInt16BE();
				penult = rd.ReadInt16BE();

				rd.ReadInt32BE(); // Zero.
				rd.ReadInt32BE(); // 0xCCCCCCCC Uninitialized stack
			}

			public static WSYSWave CreateFromStream(bgReader rd)
			{
				var b = new WSYSWave();
				b.loadFromStream(rd);
				return b;
			}
		}
	}
}

    



