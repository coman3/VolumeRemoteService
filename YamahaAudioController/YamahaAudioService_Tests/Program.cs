using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.CoreAudio.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YamahaAudioService_Tests
{
    class Program : IObserver<DeviceVolumeChangedArgs>
    {
        static void Main(string[] args)
        {
            new Program().Start();

        }

        void IObserver<DeviceVolumeChangedArgs>.OnCompleted()
        {
            Console.WriteLine("Completed");
        }

        void IObserver<DeviceVolumeChangedArgs>.OnError(Exception error)
        {
            Console.WriteLine(error);
        }
        public static double Map(double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        void IObserver<DeviceVolumeChangedArgs>.OnNext(DeviceVolumeChangedArgs value)
        {
            Console.WriteLine(value.Volume);
            var audioDeviceVolume = Map(value.Volume, 0, 100, -80, -10);
            audioDeviceVolume = Math.Round(audioDeviceVolume * 2, MidpointRounding.AwayFromZero) / 2;
            var audioVolumeToBeSent = audioDeviceVolume * 10;
            var contentToBeSent = $"<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Volume><Lvl><Val>{audioVolumeToBeSent}</Val><Exp>1</Exp><Unit>dB</Unit></Lvl></Volume></Main_Zone></YAMAHA_AV>";
            Console.WriteLine(contentToBeSent);
        }

        private void Start()
        {

            var controller = new CoreAudioController(false);
            controller.LoadDevices(false, false, false);
            CoreAudioDevice defaultPlaybackDevice = controller.DefaultPlaybackDevice;
            defaultPlaybackDevice.ReloadAudioEndpointVolume();
            defaultPlaybackDevice.VolumeChanged.Subscribe(this);
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}
