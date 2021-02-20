﻿using Prism.Commands;
using Prism.Mvvm;
using System.Linq;
using System.Windows.Input;

namespace CapFrameX.ViewModel.SubModels
{
    public class OverlayGroupControl : BindableBase
    {
        private readonly OverlayViewModel _overlayViewModel;

        private bool _overlayGroupCaptureItems;
        private bool _overlayGroupSystemInfo;
        private bool _overlayGroupOnlineMetrics;
        private bool _overlayGroupGpuBasics;
        private bool _overlayGroupCPULoads;
        private bool _overlayGroupCPUClocks;
        private bool _overlayGroupCPUTemps;
        private bool _overlayGroupRAMItems;

        public bool OverlayGroupCaptureItems
        {
            get { return _overlayGroupCaptureItems; }
            set
            {
                _overlayGroupCaptureItems = value;
                RaisePropertyChanged();
                ManageCXEntries();
            }
        }

        public bool OverlayGroupSystemInfo
        {
            get { return _overlayGroupSystemInfo; }
            set
            {
                _overlayGroupSystemInfo = value;
                RaisePropertyChanged();
                ManageSystemInfoEntries();
            }
        }

        public bool OverlayGroupOnlineMetrics
        {
            get { return _overlayGroupOnlineMetrics; }
            set
            {
                _overlayGroupOnlineMetrics = value;
                RaisePropertyChanged();
                ManageOnlineMetricEntries();
            }
        }

        public bool OverlayGroupGpuBasics
        {
            get { return _overlayGroupGpuBasics; }
            set
            {
                _overlayGroupGpuBasics = value;
                RaisePropertyChanged();
                ManageGpuBasicEntries();
            }
        }

        public bool OverlayGroupCPULoads
        {
            get { return _overlayGroupCPULoads; }
            set
            {
                _overlayGroupCPULoads = value;
                RaisePropertyChanged();
                ManageCPULoadEntries();
            }
        }

        public bool OverlayGroupCPUClocks
        {
            get { return _overlayGroupCPUClocks; }
            set
            {
                _overlayGroupCPUClocks = value;
                RaisePropertyChanged();
                ManageCPUClockEntries();
            }
        }

        public bool OverlayGroupCPUTemps
        {
            get { return _overlayGroupCPUTemps; }
            set
            {
                _overlayGroupCPUTemps = value;
                RaisePropertyChanged();
                ManageCPUTemperatureEntries();
            }
        }

        public bool OverlayGroupRAMItems
        {
            get { return _overlayGroupRAMItems; }
            set
            {
                _overlayGroupRAMItems = value;
                RaisePropertyChanged();
                ManageRAMEntries();
            }
        }
        public ICommand CheckCaptureItems { get; }
        public ICommand CheckSystemInfo { get; }
        public ICommand CheckOnlineMetrics { get; }
        public ICommand CheckGpuBasics { get; }
        public ICommand CheckCpuLoads { get; }
        public ICommand CheckCpuClocks { get; }
        public ICommand CheckCpuTemps { get; }
        public ICommand CheckRamItems { get; }
        public ICommand UncheckCaptureItems { get; }
        public ICommand UncheckSystemInfo { get; }
        public ICommand UncheckOnlineMetrics { get; }
        public ICommand UncheckGpuBasics { get; }
        public ICommand UncheckCpuLoads { get; }
        public ICommand UncheckCpuClocks { get; }
        public ICommand UncheckCpuTemps{ get; }
        public ICommand UncheckRamItems { get; }

        public OverlayGroupControl(OverlayViewModel overlayViewModel)
        {
            _overlayViewModel = overlayViewModel;

            CheckCaptureItems = new DelegateCommand(() => OverlayGroupCaptureItems = true);
            CheckSystemInfo = new DelegateCommand(() => OverlayGroupSystemInfo = true);
            CheckOnlineMetrics = new DelegateCommand(() => OverlayGroupOnlineMetrics = true);
            CheckGpuBasics = new DelegateCommand(() => OverlayGroupGpuBasics = true);
            CheckCpuLoads = new DelegateCommand(() => OverlayGroupCPULoads = true);
            CheckCpuClocks = new DelegateCommand(() => OverlayGroupCPUClocks = true);
            CheckCpuTemps = new DelegateCommand(() => OverlayGroupCPUTemps = true);
            CheckRamItems = new DelegateCommand(() => OverlayGroupRAMItems = true);

            UncheckCaptureItems = new DelegateCommand(() => OverlayGroupCaptureItems = false);
            UncheckSystemInfo = new DelegateCommand(() => OverlayGroupSystemInfo = false);
            UncheckOnlineMetrics = new DelegateCommand(() => OverlayGroupOnlineMetrics = false);
            UncheckGpuBasics = new DelegateCommand(() => OverlayGroupGpuBasics = false);
            UncheckCpuLoads = new DelegateCommand(() => OverlayGroupCPULoads = false);
            UncheckCpuClocks = new DelegateCommand(() => OverlayGroupCPUClocks = false);
            UncheckCpuTemps = new DelegateCommand(() => OverlayGroupCPUTemps = false);
            UncheckRamItems = new DelegateCommand(() => OverlayGroupRAMItems = false);
        }

        private void ManageCXEntries()
        {
            foreach (var entry in _overlayViewModel.OverlayEntries
                   .Where(item => item.Identifier == "CaptureServiceStatus"
                       || item.Identifier == "CaptureTimer"
                       || item.Identifier == "RunHistory"))
            {
                if (entry.ShowOnOverlayIsEnabled)
                    entry.ShowOnOverlay = OverlayGroupCaptureItems;
            }
        }

        private void ManageSystemInfoEntries()
        {
            foreach (var entry in _overlayViewModel.OverlayEntries
                   .Where(item => item.Identifier == "CustomCPU"
                       || item.Identifier == "CustomGPU"
                       || item.Identifier == "Mainboard"
                       || item.Identifier == "CustomRAM"
                       || item.Identifier == "OS"
                       || item.Identifier == "GPUDriver"))
            {
                if (entry.ShowOnOverlayIsEnabled)
                    entry.ShowOnOverlay = OverlayGroupSystemInfo;
            }
        }

        private void ManageOnlineMetricEntries()
        {
            foreach (var entry in _overlayViewModel.OverlayEntries
                   .Where(item => item.Identifier == "OnlineAverage"
                       || item.Identifier == "OnlineP1"
                       || item.Identifier == "OnlineP0dot2"))
            {
                if (entry.ShowOnOverlayIsEnabled)
                    entry.ShowOnOverlay = OverlayGroupOnlineMetrics;
            }
        }

        private void ManageGpuBasicEntries()
        {
            foreach (var entry in _overlayViewModel.OverlayEntries
                 .Where(item => item.OverlayEntryType == Contracts.Overlay.EOverlayEntryType.GPU))
            {
                if (entry.Description.Contains("GPU Core") && entry.ShowOnOverlayIsEnabled)
                    entry.ShowOnOverlay = OverlayGroupGpuBasics;
            }
        }

        private void ManageCPULoadEntries()
        {
            foreach (var entry in _overlayViewModel.OverlayEntries
                   .Where(item => item.OverlayEntryType == Contracts.Overlay.EOverlayEntryType.CPU))
            {
                if (entry.Identifier.Contains("load") && entry.Description.Contains("CPU Core") 
                    && entry.ShowOnOverlayIsEnabled)
                    entry.ShowOnOverlay = OverlayGroupCPULoads;
            }
        }

        private void ManageCPUClockEntries()
        {
            foreach (var entry in _overlayViewModel.OverlayEntries
                   .Where(item => item.OverlayEntryType == Contracts.Overlay.EOverlayEntryType.CPU))
            {
                if (entry.Identifier.Contains("clock") && entry.Description.Contains("CPU Core") 
                    && entry.ShowOnOverlayIsEnabled)
                    entry.ShowOnOverlay = OverlayGroupCPUClocks;
            }
        }

        private void ManageCPUTemperatureEntries()
        {
            foreach (var entry in _overlayViewModel.OverlayEntries
                   .Where(item => item.OverlayEntryType == Contracts.Overlay.EOverlayEntryType.CPU))
            {
                if (entry.Identifier.Contains("temperature") && entry.Description.Contains("CPU Core") 
                    && entry.ShowOnOverlayIsEnabled)
                    entry.ShowOnOverlay = OverlayGroupCPUTemps;
            }
        }

        private void ManageRAMEntries()
        {
            foreach (var entry in _overlayViewModel.OverlayEntries
                   .Where(item => item.OverlayEntryType == Contracts.Overlay.EOverlayEntryType.RAM))
            {
                if (entry.ShowOnOverlayIsEnabled)
                    entry.ShowOnOverlay = OverlayGroupRAMItems;
            }
        }
    }
}
