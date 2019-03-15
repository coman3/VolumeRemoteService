using System;
using System.IO;

namespace YamahaAudioController.Common
{
    public class ConfigManager
    {
        public const string ConfigFile = "YamahaAudioServiceConfig.json";
        public static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "YamahaAudioController");
        public static readonly string ConfigPath = Path.Combine(ConfigDirectory, ConfigFile);


        public static Config GetConfig()
        {
            CreateDefaultIfNotExsits();
            
            return Config.Load(ConfigPath);
        }

        private static void CreateDefaultIfNotExsits()
        {
            if (!Directory.Exists(ConfigDirectory))
                Directory.CreateDirectory(ConfigDirectory);

            if (!File.Exists(ConfigPath))
                Config.Default().Save(ConfigPath);
        }
    }
}
