using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.Observables;
using NLog;
namespace YamahaAudioService
{
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };


    partial class AudioService : ServiceBase,  IObserver<DeviceVolumeChangedArgs>
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
        private CoreAudioDevice _defaultPlaybackDevice;

        public AudioService()
        {
            InitializeComponent();
            InitializeLoger();
            _logger = LogManager.GetCurrentClassLogger();
            _controller = new CoreAudioController(false);
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

            _defaultPlaybackDevice = _controller.DefaultPlaybackDevice;
            if (_defaultPlaybackDevice == null)
                throw new InvalidOperationException("Error loading default playback device");
            _defaultPlaybackDevice.ReloadAudioEndpointVolume();

            _defaultPlaybackDevice.VolumeChanged.Subscribe(this);

            _logger.Info("Selected Default Audio Device ({0}) for volume tracking.", _defaultPlaybackDevice.FullName);


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

        void IObserver<DeviceVolumeChangedArgs>.OnNext(DeviceVolumeChangedArgs value)
        {
            _logger.Info("Volume Changed from {0} to {1}", _defaultPlaybackDevice.Volume, value.Volume);
        }

        void IObserver<DeviceVolumeChangedArgs>.OnError(Exception error)
        {
            
        }

        void IObserver<DeviceVolumeChangedArgs>.OnCompleted()
        {
            
        }
    }
}
