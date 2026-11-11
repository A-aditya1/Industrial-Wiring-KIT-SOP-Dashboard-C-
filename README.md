# Wiring KIT SOP Dashboard – DSE G4501

WPF desktop dashboard for .NET 8, Siemens S7-1200 PLC using S7.NetPlus.

## Requirements
- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 (Desktop development with .NET) or VS Code with C# Dev Kit
- Network access to restore NuGet packages

## Run in Visual Studio
1. Open `WiringKitSOP.csproj`.
2. Restore NuGet packages.
3. Press F5.
4. Leave **Demo Mode** checked and click **Start Task** to test the animated sequence without a PLC.

## Run in terminal
```powershell
dotnet restore
dotnet build
dotnet run
```

## Configure stages
Edit `Configuration/wiring-steps.json`. The number of array entries defines the total stages. Each stage contains:
- Wire color and `HexColor`
- Ferrule number
- Source terminal, target pin, connector and controller slot
- Instructions
- PLC DB number, byte offset and bit numbers

The sample configuration contains 4 illustrative stages. Copy an entry and change its fields to add more stages. Keep byte offsets and DB addresses aligned with the PLC program.

## PLC byte/bit map used by the sample
For each stage's configured DB byte:
- Bit 0: wire connected
- Bit 1: tie gun active
- Bit 2: wire tied
- Bit 3: stage complete

Example Stage 1: `DB1.DBB0` (bits 0–3). Stage 2: `DB1.DBB1`, etc.

Edit `Configuration/plc-settings.json` for the PLC IP address, rack, slot, and polling interval. The sample uses S7-1200, rack 0, slot 1, IP `192.168.0.1` as a placeholder. Confirm the actual CPU connection parameters with the PLC engineer.

## Important PLC commissioning notes
- These are example addresses and must be matched to the actual PLC DB layout before connecting to production equipment.
- In TIA Portal, enable PUT/GET access if required by your CPU/security configuration and confirm the DB is accessible. If using optimized DBs, check the access method and library requirements with your controls engineer.
- This dashboard reads feedback only. It does not write outputs to the PLC or command a real tie gun.
- Test in Demo Mode first, then on a safe test bench with the machine isolated. Do not use software feedback as a substitute for safety interlocks or validated torque/continuity checks.
- The controller graphic is a schematic visualization, not a verified DSE G4501 terminal drawing. Replace it with your approved controller image/layout and verified pin coordinates before production use.

## Reports
Excel and Word reports are saved under `Documents\WiringKitSOPReports`.


## Build troubleshooting update
The source package has explicit `System.IO` imports in the configuration/report services and fully qualified
`System.IO.Path` calls in the WPF window to avoid conflict with `System.Windows.Shapes.Path`.
The S7.NetPlus package reference is `S7netplus` version `0.20.0`, and its namespace is `S7.Net`.

From the folder containing `WiringKitSOP.csproj`, run:
```powershell
dotnet clean
dotnet restore
dotnet build
dotnet run
```
Use Demo Mode first. The sample PLC IP and DB/byte/bit mapping are placeholders; verify them with the PLC engineer
before attempting a real connection.
