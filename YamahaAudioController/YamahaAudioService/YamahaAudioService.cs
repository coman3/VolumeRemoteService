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

    partial class YamahaAudioService : ServiceBase, IObserver<DeviceVolumeChangedArgs>
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
        public Stack<CommandItem> CommandStack = new Stack<CommandItem>();
        

        public YamahaAudioService()
        {
            InitializeComponent();
            InitializeLoger();
            _logger = LogManager.GetCurrentClassLogger();
            _controller = new CoreAudioController(false);
            _requestTimer = new CustomTimer(150);
        }

        private async void _requestTimer_Elapsed(object state)
        {
            if (CommandStack.Count <= 0) return;
            var command = CommandStack.Pop();
            if (command is VolumeChangeCommandItem)
            {
                var commandVolume = (VolumeChangeCommandItem)command;

                
                var stringContent = new StringContent(commandVolume.GetCommandContent(), Encoding.UTF8, "text/xml");
                _logger.Info("Sending volume change command ({1}): {0}", commandVolume.GetCommandContent(), commandVolume.Volume);
                await client.PostAsync("http://10.0.0.91/YamahaRemoteControl/ctrl", stringContent);
            }
            CommandStack.Clear();
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
            // TODO: Add code here to start your service.
            _logger.Info("Service Started.");


            _controller.LoadDevices(false, false, false);
            _logger.Info("Successfully found {0} audio devices.", _controller.GetDevices().Count());

            _selectedDevice = _controller.DefaultPlaybackDevice;
            if (_selectedDevice == null)
                throw new InvalidOperationException("Error loading default playback device");
            _selectedDevice.ReloadAudioEndpointVolume();

            _selectedDevice.VolumeChanged.Subscribe(this);

            _logger.Info("Selected Default Audio Device ({0}) for volume tracking.", _selectedDevice.FullName);
            _requestTimer.OnTimer = _requestTimer_Elapsed;

            SetServiceState(ServiceState.SERVICE_RUNNING);

        }

        protected override void OnStop()
        {
            SetServiceState(ServiceState.SERVICE_STOP_PENDING, 100000);
            // TODO: Add code here to perform any tear-down necessary to stop your service.
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

        public static double Map(double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        void IObserver<DeviceVolumeChangedArgs>.OnNext(DeviceVolumeChangedArgs value)
        {
            var audioDeviceVolume = Map(value.Volume, 0, 100, -80, -10);
            audioDeviceVolume = Math.Round(audioDeviceVolume * 2, MidpointRounding.AwayFromZero) / 2;
            var audioVolumeToBeSent = audioDeviceVolume * 10;
            var command = new VolumeChangeCommandItem((int)audioVolumeToBeSent);
            CommandStack.Push(command);

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
    }
}
