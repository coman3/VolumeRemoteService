using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.Observables;
using NLog;
using YamahaAudioController.Common;

namespace YamahaAudioService
{

    partial class YamahaAudioService : ServiceBase, IObserver<DeviceVolumeChangedArgs>, IObserver<DeviceMuteChangedArgs>
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        private void SetServiceState(ServiceState state, int dwWaitHint = 0)
        {
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            serviceStatus.dwWaitHint = dwWaitHint;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        private Logger _logger { get; }
        private CoreAudioController _controller;
        private CoreAudioDevice _selectedDevice;
        private HttpClient client = new HttpClient();

        public Config Config { get; private set; }
        public CustomTimer _requestTimer;
        public List<CommandItem> CommandStack = new List<CommandItem>();
        

        public YamahaAudioService()
        {
            InitializeComponent();
            InitializeLoger();
            _logger = LogManager.GetCurrentClassLogger();
            _controller = new CoreAudioController(false);
            _requestTimer = new CustomTimer(25);
        }

        private async void _requestTimer_Elapsed(object state)
        {
            if (CommandStack.Count <= 0) return;
            await PerformCommandStack();
            _requestTimer.Stop();
        }

        private async Task PerformCommandStack()
        {
            CommandItem command = CommandStack.OfType<VolumeChangeCommandItem>().LastOrDefault();
            if (command is VolumeChangeCommandItem)
            {
                try
                {
                    var commandVolume = (VolumeChangeCommandItem)command;

                    var stringContent = new StringContent(commandVolume.GetCommandContent(), Encoding.UTF8, "text/xml");
                    _logger.Info("Sending volume change command ({1}): {0}", commandVolume.GetCommandContent(), commandVolume.Volume);
                    await client.PostAsync("http://10.0.0.91/YamahaRemoteControl/ctrl", stringContent);
                }
                catch (Exception)
                {
                    _logger.Error("Sending volume change command Failed!");
                }
                CommandStack.RemoveAll(x => x is VolumeChangeCommandItem);
            }
            command = CommandStack.OfType<PowerChangeCommandItem>().LastOrDefault();
            if (command is PowerChangeCommandItem)
            {
                try
                {
                    var commandPower = (PowerChangeCommandItem)command;

                    var stringContent = new StringContent(commandPower.GetCommandContent(), Encoding.UTF8, "text/xml");
                    _logger.Info("Sending power change command ({1}): {0}", commandPower.GetCommandContent(), commandPower.PowerStatus);
                    await client.PostAsync("http://10.0.0.91/YamahaRemoteControl/ctrl", stringContent);
                }
                catch (Exception)
                {
                    _logger.Error("Sending power change command Failed!");
                }
                CommandStack.RemoveAll(x => x is PowerChangeCommandItem);
            }
            command = CommandStack.OfType<LoadSceneCommandItem>().LastOrDefault();
            if (command is LoadSceneCommandItem)
            {
                try
                {
                    var commandScene = (LoadSceneCommandItem)command;

                    var stringContent = new StringContent(commandScene.GetCommandContent(), Encoding.UTF8, "text/xml");
                    _logger.Info("Sending scene change command ({1}): {0}", commandScene.GetCommandContent(), commandScene.SceneNumber);
                    await client.PostAsync("http://10.0.0.91/YamahaRemoteControl/ctrl", stringContent);
                }
                catch (Exception)
                {
                    _logger.Error("Sending scene change command Failed!");
                }
                CommandStack.RemoveAll(x => x is LoadSceneCommandItem);
            }
            command = CommandStack.OfType<MuteChangeCommandItem>().LastOrDefault();
            if (command is MuteChangeCommandItem)
            {
                try
                {
                    var commandMute = (MuteChangeCommandItem)command;

                    var stringContent = new StringContent(commandMute.GetCommandContent(), Encoding.UTF8, "text/xml");
                    _logger.Info("Sending mute change command ({1}): {0}", commandMute.GetCommandContent(), commandMute.MuteStatus);
                    await client.PostAsync("http://10.0.0.91/YamahaRemoteControl/ctrl", stringContent);
                }
                catch (Exception)
                {
                    _logger.Error("Sending mute change command Failed!");
                }
                CommandStack.RemoveAll(x => x is MuteChangeCommandItem);
            }
        }

        private void InitializeLoger()
        {
            if (!EventLog.SourceExists("YamahaAudioService", "."))
            {
                EventLog.CreateEventSource("YamahaAudioService", "Default");
            }

            var target = new NLog.Targets.EventLogTarget();
            target.Source = "YamahaAudioService";
            target.Log = "Default";
            target.MachineName = ".";

            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Debug);
        }

        protected override void OnStart(string[] args)
        {
            SetServiceState(ServiceState.SERVICE_START_PENDING, 100000);
            _logger.Info("Service Started.");


            _controller.LoadDevices(false, false, false);
            _logger.Info("Successfully found {0} audio devices.", _controller.GetDevices().Count());

            _selectedDevice = _controller.DefaultPlaybackDevice;
            if (_selectedDevice == null)
                throw new InvalidOperationException("Error loading default playback device");
            _selectedDevice.ReloadAudioEndpointVolume();

            _selectedDevice.VolumeChanged.Subscribe(this);
            _selectedDevice.MuteChanged.Subscribe(this);

            _logger.Info("Selected Default Audio Device ({0}) for volume tracking.", _selectedDevice.FullName);
            _requestTimer.OnTimer = _requestTimer_Elapsed;
            
            CommandStack.Add(new PowerChangeCommandItem(true));
            CommandStack.Add(new LoadSceneCommandItem(3));
            PerformCommandStack().Wait();
            var volumeTask = _selectedDevice.GetVolumeAsync();
            volumeTask.Wait();
            CommandStack.Add(new VolumeChangeCommandItem(volumeTask.Result));
            PerformCommandStack().Wait();
            SetServiceState(ServiceState.SERVICE_RUNNING);

        }

        protected override void OnStop()
        {
            SetServiceState(ServiceState.SERVICE_STOP_PENDING, 100000);

            CommandStack.Add(new PowerChangeCommandItem(false));
            PerformCommandStack().Wait();
            _logger.Info("Service Stopped.");
            _controller.Dispose();

            SetServiceState(ServiceState.SERVICE_STOPPED);
        }

        protected override void OnCustomCommand(int command)
        {
            switch ((YamahaAudioServiceCommand)command)
            {
                case YamahaAudioServiceCommand.ReloadConfig:
                    Config = ConfigManager.GetConfig();
                    Reload();
                    break;
                default:
                    break;
            }
            base.OnCustomCommand(command);
        }

        private void Reload()
        {

        }

        void IObserver<DeviceVolumeChangedArgs>.OnNext(DeviceVolumeChangedArgs value)
        {
            CommandStack.Add(new VolumeChangeCommandItem(value.Volume));
            // Restart Debounce Timer
            if (_requestTimer.Enabled) _requestTimer.Stop();
                _requestTimer.Start();
        }

        void IObserver<DeviceVolumeChangedArgs>.OnError(Exception error)
        {

        }

        void IObserver<DeviceVolumeChangedArgs>.OnCompleted()
        {

        }

        void IObserver<DeviceMuteChangedArgs>.OnNext(DeviceMuteChangedArgs value)
        {
            CommandStack.Add(new MuteChangeCommandItem(value.IsMuted));
            PerformCommandStack().Wait();
        }

        void IObserver<DeviceMuteChangedArgs>.OnError(Exception error)
        {

        }

        void IObserver<DeviceMuteChangedArgs>.OnCompleted()
        {

        }
    }
}
