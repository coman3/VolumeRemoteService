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

        private static double Map(double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        public VolumeChangeCommandItem(double currentVolume, int min = -80, int max = -5)
        {
            var audioDeviceVolume = Map(currentVolume, 0, 100, min, max);
            audioDeviceVolume = Math.Round(audioDeviceVolume * 2, MidpointRounding.AwayFromZero) / 2;
            Volume = (int)(audioDeviceVolume * 10);
        }      

        public string GetCommandContent()
        {
            return $"<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Volume><Lvl><Val>{Volume}</Val><Exp>1</Exp><Unit>dB</Unit></Lvl></Volume></Main_Zone></YAMAHA_AV>";
        }

    }

    class LoadSceneCommandItem : CommandItem
    {
        public byte SceneNumber { get; set; }

        public LoadSceneCommandItem(byte sceneNumber)
        {
            if (sceneNumber >= 5) throw new ArgumentOutOfRangeException(nameof(sceneNumber));
            SceneNumber = sceneNumber;
            
        }

        public string GetCommandContent()
        {
            return $"<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Scene><Scene_Load>Scene {SceneNumber}</Scene_Load></Scene></Main_Zone></YAMAHA_AV>";
        }

    }

    class PowerChangeCommandItem : CommandItem
    {
        public bool PowerStatus { get; set; }

        public PowerChangeCommandItem(bool powerStatus)
        {
            PowerStatus = powerStatus;
        }

        public string GetCommandContent()
        {
            return $"<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Power_Control><Power>{(PowerStatus ? "On" : "Standby")}</Power></Power_Control></Main_Zone></YAMAHA_AV>";
        }

    }

    class MuteChangeCommandItem : CommandItem
    {
        public bool MuteStatus { get; set; }

        public MuteChangeCommandItem(bool muteStatus)
        {
            MuteStatus = muteStatus;
        }

        public string GetCommandContent()
        {
            return $"<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Volume><Mute>{(MuteStatus ? "On" : "Off")}</Mute></Volume></Main_Zone></YAMAHA_AV>";
        }

    }
}

//<YAMAHA_AV cmd="PUT"><Main_Zone><Volume><Mute>On</Mute></Volume></Main_Zone></YAMAHA_AV>