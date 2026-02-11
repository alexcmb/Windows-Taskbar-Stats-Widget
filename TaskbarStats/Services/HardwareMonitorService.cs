using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using TaskbarStats.Models;

namespace TaskbarStats.Services
{
    public class HardwareMonitorService : IDisposable
    {
        private Computer? _computer;
        private readonly Queue<float> _cpuTempHistory = new Queue<float>();
        private readonly Queue<float> _gpuTempHistory = new Queue<float>();
        private const int HistoryLength = 5;
        private CancellationTokenSource? _cts;
        private Task? _pollingTask;
        private int _consecutiveErrors = 0;
        private const int MaxConsecutiveErrors = 3;

        public event Action<TemperatureData>? OnDataUpdated;

        public HardwareMonitorService()
        {
            InitializeComputer();
        }

        private void InitializeComputer()
        {
            CloseComputer();
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true
            };
            try
            {
                _computer.Open();
            }
            catch (Exception)
            {
                // Silently fail or handle gracefully
            }
        }

        private void CloseComputer()
        {
            try
            {
                _computer?.Close();
            }
            catch { }
            _computer = null;
        }

        public void Start()
        {
            if (_pollingTask != null && !_pollingTask.IsCompleted) return;

            _cts = new CancellationTokenSource();
            _pollingTask = Task.Run(() => PollHardware(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { _pollingTask?.Wait(2000); } catch { }
            CloseComputer();
        }

        private async Task PollHardware(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_computer == null) InitializeComputer();

                    var data = new TemperatureData();
                    bool readSuccess = false;
                    
                    if (_computer != null)
                    {
                        foreach (var hardware in _computer.Hardware)
                        {
                            try 
                            {
                                hardware.Update();
                            }
                            catch 
                            { 
                                continue; 
                            }

                            if (hardware.HardwareType == HardwareType.Cpu)
                            {
                                var tempSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Package")) 
                                                 ?? hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core"))
                                                 ?? hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                                
                                if (tempSensor != null && tempSensor.Value.HasValue)
                                {
                                    data.CpuTemp = SmoothValue(_cpuTempHistory, tempSensor.Value.Value);
                                    readSuccess = true;
                                }
                            }
                            else if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel)
                            {
                                try
                                {
                                    var tempSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Hot Spot"))
                                                     ?? hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core"))
                                                     ?? hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
    
                                    if (tempSensor != null && tempSensor.Value.HasValue)
                                    {
                                        data.GpuTemp = SmoothValue(_gpuTempHistory, tempSensor.Value.Value);
                                        readSuccess = true;
                                    }
                                }
                                catch { }
                            }
                        }
                    }

                    if (readSuccess)
                    {
                        OnDataUpdated?.Invoke(data);
                        _consecutiveErrors = 0;
                    }
                }
                catch (Exception)
                {
                    _consecutiveErrors++;
                    if (_consecutiveErrors >= MaxConsecutiveErrors)
                    {
                        InitializeComputer();
                        _consecutiveErrors = 0;
                    }
                }

                try { await Task.Delay(1000, token); } catch (OperationCanceledException) { break; }
            }
        }

        private float SmoothValue(Queue<float> history, float newValue)
        {
            if (history.Count >= HistoryLength)
            {
                history.Dequeue();
            }
            history.Enqueue(newValue);
            return history.Average();
        }



        public void Dispose()
        {
            Stop();
        }
    }
}
