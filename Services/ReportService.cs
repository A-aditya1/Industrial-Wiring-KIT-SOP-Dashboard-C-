using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WiringKitSOP.Models;

namespace WiringKitSOP.Services;

public static class ReportService
{
    public static string ExportExcel(IEnumerable<EventLogItem> events, IEnumerable<SopStep> steps, string folder)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"WiringSOP_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("SOP Summary");
        ws.Cell(1, 1).Value = "WIRING KIT SOP REPORT"; ws.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;
        string[] headers = ["Stage", "Wire", "Ferrule", "Source", "Target", "Controller Slot"];
        for (int i = 0; i < headers.Length; i++) ws.Cell(3, i + 1).Value = headers[i];
        var row = 4;
        foreach (var s in steps) { ws.Cell(row, 1).Value = s.Number; ws.Cell(row, 2).Value = s.WireColor; ws.Cell(row, 3).Value = s.FerruleNumber; ws.Cell(row, 4).Value = s.SourceTerminal; ws.Cell(row, 5).Value = s.TargetPin; ws.Cell(row, 6).Value = s.ControllerSlot; row++; }
        ws.Columns().AdjustToContents();
        var log = wb.Worksheets.Add("Events");
        log.Cell(1, 1).Value = "Time"; log.Cell(1, 2).Value = "Event"; log.Cell(1, 3).Value = "Details";
        row = 2; foreach (var e in events) { log.Cell(row, 1).Value = e.Time; log.Cell(row, 2).Value = e.Event; log.Cell(row, 3).Value = e.Details; row++; }
        log.Columns().AdjustToContents(); wb.SaveAs(path); return path;
    }

    public static string ExportWord(IEnumerable<EventLogItem> events, IEnumerable<SopStep> steps, string folder)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"WiringSOP_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart(); main.Document = new Document(new Body());
        var body = main.Document.Body!;
        body.Append(Paragraph("GREAVES COTTON LTD – WIRING KIT SOP REPORT", true, 30));
        body.Append(Paragraph($"Controller: DSE G4501    Generated: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"));
        body.Append(Paragraph("Stage Summary", true, 24));
        foreach (var s in steps) body.Append(Paragraph($"Stage {s.Number}: {s.WireColor} wire | Ferrule {s.FerruleNumber} | {s.SourceTerminal} → {s.TargetPin} | Slot {s.ControllerSlot}"));
        body.Append(Paragraph("Recent Events", true, 24));
        foreach (var e in events) body.Append(Paragraph($"{e.Time:HH:mm:ss} | {e.Event} | {e.Details}"));
        main.Document.Save(); return path;
    }
    private static Paragraph Paragraph(string text, bool bold = false, int size = 22)
    {
        var runProps = new RunProperties(new FontSize { Val = size.ToString() });
        if (bold) runProps.Append(new Bold());
        return new Paragraph(new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }
}
