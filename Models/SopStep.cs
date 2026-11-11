namespace WiringKitSOP.Models;

public sealed class SopStep
{
    public int Number { get; set; }
    public string Title { get; set; } = "Wire connection";
    public string WireColor { get; set; } = "Red";
    public string HexColor { get; set; } = "#E53935";
    public int FerruleNumber { get; set; }
    public string SourceTerminal { get; set; } = "TB-01";
    public string TargetPin { get; set; } = "P1";
    public string ConnectorName { get; set; } = "J1";
    public string ControllerSlot { get; set; } = "C4";
    public string ExpectedFeedback { get; set; } = "Connection OK + Tie OK";
    public List<string> Instructions { get; set; } = new();
    public PlcSignalMap Signals { get; set; } = new();
}

public sealed class PlcSignalMap
{
    public int DbNumber { get; set; } = 1;
    public int ByteOffset { get; set; }
    public int ConnectionBit { get; set; } = 0;
    public int TieGunBit { get; set; } = 1;
    public int TiedBit { get; set; } = 2;
    public int StageCompleteBit { get; set; } = 3;
}

public sealed class PlcSettings
{
    public string IpAddress { get; set; } = "192.168.0.1";
    public short Rack { get; set; } = 0;
    public short Slot { get; set; } = 1;
    public int PollIntervalMs { get; set; } = 250;
    public int MaxAttempts { get; set; } = 3;
    public string CpuType { get; set; } = "S71200";
}

public sealed class StepFeedback
{
    public bool Connected { get; set; }
    public bool TieGunActive { get; set; }
    public bool Tied { get; set; }
    public bool Complete { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public sealed class EventLogItem
{
    public DateTime Time { get; set; } = DateTime.Now;
    public string Event { get; set; } = "";
    public string Details { get; set; } = "";
}
