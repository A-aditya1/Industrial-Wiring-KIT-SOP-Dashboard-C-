using S7.Net;
using WiringKitSOP.Models;

namespace WiringKitSOP.Services;

public sealed class PlcService : IDisposable
{
    private readonly PlcSettings _settings;
    private Plc? _plc;
    public bool IsConnected => _plc?.IsConnected == true;
    public PlcService(PlcSettings settings) => _settings = settings;

    public Task ConnectAsync() => Task.Run(() =>
    {
        if (IsConnected) return;
        var cpu = Enum.TryParse<CpuType>(_settings.CpuType, true, out var parsed) ? parsed : CpuType.S71200;
        _plc = new Plc(cpu, _settings.IpAddress, _settings.Rack, _settings.Slot);
        _plc.Open();
        if (!IsConnected) throw new InvalidOperationException("PLC connection could not be established.");
    });

    public Task<StepFeedback> ReadStepAsync(PlcSignalMap map) => Task.Run(() =>
    {
        if (!IsConnected || _plc == null) throw new InvalidOperationException("PLC is not connected.");
        var bytes = _plc.ReadBytes(DataType.DataBlock, map.DbNumber, map.ByteOffset, 1);
        var value = bytes[0];
        return new StepFeedback
        {
            Connected = GetBit(value, map.ConnectionBit),
            TieGunActive = GetBit(value, map.TieGunBit),
            Tied = GetBit(value, map.TiedBit),
            Complete = GetBit(value, map.StageCompleteBit),
            Timestamp = DateTime.Now
        };
    });

    private static bool GetBit(byte value, int bit) => bit is >= 0 and <= 7 && (value & (1 << bit)) != 0;
    public void Disconnect() { try { _plc?.Close(); } catch { } }
    public void Dispose() { Disconnect(); _plc = null; }
}
