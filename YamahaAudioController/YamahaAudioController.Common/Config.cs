using Newtonsoft.Json;
using System;
using System.IO;

namespace YamahaAudioController.Common
{
    [Serializable]
    public class Config
    {
        public bool UseDefaultPlaybackDevice { get; set; }
        public string VolumeTrackedDeviceId { get; set; }
        public string AudioDeviceIP { get; set; }
        public int MinimumTargetVolume { get; set; }
        public int MaximumTargetVolume { get; set; }

        internal static Config Default()
        {
            return new Config();
        }

        internal void Save(string path)
        {
            var configFile = JsonConvert.SerializeObject(this);
            File.WriteAllText(path, configFile);
        }

        internal static Config Load(string path)
        {
            var configFile = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Config>(configFile);
        }
    }
}
