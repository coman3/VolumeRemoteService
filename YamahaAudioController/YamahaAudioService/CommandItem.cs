using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YamahaAudioService
{
    abstract class CommandItem
    {

    }

    class VolumeChangeCommandItem : CommandItem
    {
        public int Volume { get; set; }

        public VolumeChangeCommandItem(int volume)
        {
            Volume = volume;
        }        

        public string GetCommandContent()
        {
            return $"<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Volume><Lvl><Val>{Volume}</Val><Exp>1</Exp><Unit>dB</Unit></Lvl></Volume></Main_Zone></YAMAHA_AV>";
        }

    }
}
