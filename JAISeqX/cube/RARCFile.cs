using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga.byteglider;

namespace JAISeqX.cube.rarc
{
    internal class RARCFile : DataArchive
    {      
        // thanks https://wiki.tockdom.com/wiki/RARC_(File_Format)#String_Table

        private void readFromStream(bgReader reader)
        {
            var magic = reader.ReadUInt32BE();
            var size = reader.ReadInt32BE();
            var infoBlockOffs = reader.ReadInt32BE();
            var fileBlockOffs = reader.ReadInt32BE() + infoBlockOffs ;
            var fileBlockLength = reader.ReadInt32BE();

            reader.Seek(infoBlockOffs);
  
            var nodeCount = reader.ReadInt32BE();
            var firstNodeOffset = reader.ReadInt32BE() + infoBlockOffs;
            var directoryCount = reader.ReadInt32BE();
            var directoryOffset = reader.ReadInt32BE() + infoBlockOffs;

            var stringtableLength = reader.ReadInt32BE() ;
            var stringTableOffset = reader.ReadInt32BE() + infoBlockOffs;

            var totalFiles = reader.ReadInt32BE();

            reader.Seek(directoryOffset);
            for (int i=0; i < directoryCount; i++)
            {
                var index = reader.ReadUInt16BE();
                var hash = reader.ReadInt16BE();
                var type = reader.ReadInt16BE();
                var nameOffset = reader.ReadInt16BE();
                var fileDataOffset = reader.ReadInt32BE();
                var fileDataLength = reader.ReadInt32BE();
                reader.ReadUInt32BE();

                if (index == 0xFFFF)
                    continue;

                reader.PushAnchor();
                reader.Seek(fileBlockOffs);
                var tempData = reader.ReadBytes(fileDataLength);
                reader.Seek(stringTableOffset + nameOffset);
                var tempName = reader.ReadTerminatedString();
                reader.PopAnchor();
                Files[tempName] = tempData;
            }
        }

        public static RARCFile CreateFromStream(bgReader reader)
        {
            var h = new RARCFile();
            h.readFromStream(reader);
            return h;
        }
      
    }

 
}
