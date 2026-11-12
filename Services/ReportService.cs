
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using ClosedXML.Excel;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using WiringKitSOP.Models;

namespace WiringKitSOP.Services
{
    public static class ReportService
    {
        // =====================================================
        // SECTION 1: EXPORT EXCEL REPORT
        // Logic:
        // 1. Create report folder.
        // 2. Generate unique Excel file name.
        // 3. Create SOP Summary worksheet.
        // 4. Export wiring stage details.
        // 5. Create Events worksheet.
        // 6. Export event logs.
        // 7. Save Excel report.
        // =====================================================

        public static string ExportExcel(
            IEnumerable<EventLogItem> events,
            IEnumerable<SopStep> steps,
            string folder)
        {
            // Create report directory if it does not exist
            Directory.CreateDirectory(folder);

            // Generate Excel file path
            var path = Path.Combine(
                folder,
                $"WiringSOP_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );

            // Create Excel workbook
            using var wb = new XLWorkbook();

            // -------------------------------------------------
            // SECTION 1.1: SOP SUMMARY WORKSHEET
            // Logic: Stores all wiring stage information.
            // -------------------------------------------------

            var ws = wb.Worksheets.Add("SOP Summary");

            // Report heading
            ws.Cell(1, 1).Value = "WIRING KIT SOP REPORT";

            ws.Range(1, 1, 1, 6)
              .Merge()
              .Style.Font.Bold = true;

            // Column headers
            string[] headers =
            [
                "Stage",
                "Wire",
                "Ferrule",
                "Source",
                "Target",
                "Controller Slot"
            ];

            // Insert column headers
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(3, i + 1).Value = headers[i];
            }

            // Export wiring stage details
            var row = 4;

            foreach (var s in steps)
            {
                ws.Cell(row, 1).Value = s.Number;
                ws.Cell(row, 2).Value = s.WireColor;
                ws.Cell(row, 3).Value = s.FerruleNumber;
                ws.Cell(row, 4).Value = s.SourceTerminal;
                ws.Cell(row, 5).Value = s.TargetPin;
                ws.Cell(row, 6).Value = s.ControllerSlot;

                row++;
            }

            // Automatically adjust column widths
            ws.Columns().AdjustToContents();


            // -------------------------------------------------
            // SECTION 1.2: EVENT LOG WORKSHEET
            // Logic: Stores dashboard event history.
            // -------------------------------------------------

            var log = wb.Worksheets.Add("Events");

            // Event log headers
            log.Cell(1, 1).Value = "Time";
            log.Cell(1, 2).Value = "Event";
            log.Cell(1, 3).Value = "Details";

            // Export event information
            row = 2;

            foreach (var e in events)
            {
                log.Cell(row, 1).Value = e.Time;
                log.Cell(row, 2).Value = e.Event;
                log.Cell(row, 3).Value = e.Details;

                row++;
            }

            // Automatically adjust event column widths
            log.Columns().AdjustToContents();

            // Save Excel workbook
            wb.SaveAs(path);

            return path;
        }


        // =====================================================
        // SECTION 2: EXPORT WORD REPORT
        // Logic:
        // 1. Create report folder.
        // 2. Generate unique Word file name.
        // 3. Create Word document.
        // 4. Add report heading and controller information.
        // 5. Add wiring stage summary.
        // 6. Add recent event logs.
        // 7. Save Word report.
        // =====================================================

        public static string ExportWord(
            IEnumerable<EventLogItem> events,
            IEnumerable<SopStep> steps,
            string folder)
        {
            // Create report directory
            Directory.CreateDirectory(folder);

            // Generate Word file path
            var path = Path.Combine(
                folder,
                $"WiringSOP_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
            );

            // Create Word document
            using var doc = WordprocessingDocument.Create(
                path,
                WordprocessingDocumentType.Document
            );

            // Create main document part
            var main = doc.AddMainDocumentPart();

            main.Document = new Document(new Body());

            var body = main.Document.Body!;


            // -------------------------------------------------
            // SECTION 2.1: REPORT HEADER
            // Logic: Adds company/report heading and controller.
            // -------------------------------------------------

            body.Append(
                Paragraph(
                    "GREAVES COTTON LTD – WIRING KIT SOP REPORT",
                    true,
                    30
                )
            );

            body.Append(
                Paragraph(
                    $"Controller: DSE G4501    Generated: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                )
            );


            // -------------------------------------------------
            // SECTION 2.2: STAGE SUMMARY
            // Logic: Adds all wiring stage details to Word.
            // -------------------------------------------------

            body.Append(
                Paragraph("Stage Summary", true, 24)
            );

            foreach (var s in steps)
            {
                body.Append(
                    Paragraph(
                        $"Stage {s.Number}: {s.WireColor} wire | " +
                        $"Ferrule {s.FerruleNumber} | " +
                        $"{s.SourceTerminal} → {s.TargetPin} | " +
                        $"Slot {s.ControllerSlot}"
                    )
                );
            }


            // -------------------------------------------------
            // SECTION 2.3: RECENT EVENTS
            // Logic: Adds event logs to Word report.
            // -------------------------------------------------

            body.Append(
                Paragraph("Recent Events", true, 24)
            );

            foreach (var e in events)
            {
                body.Append(
                    Paragraph(
                        $"{e.Time:HH:mm:ss} | {e.Event} | {e.Details}"
                    )
                );
            }


            // Save Word document
            main.Document.Save();

            return path;
        }


        // =====================================================
        // SECTION 3: WORD PARAGRAPH HELPER
        // Logic:
        // Reusable method to create formatted Word paragraphs.
        // bold = true  -> Bold text.
        // size         -> Font size in half-points.
        // =====================================================

        private static Paragraph Paragraph(
            string text,
            bool bold = false,
            int size = 22)
        {
            // Configure font size
            var runProps = new RunProperties(
                new FontSize
                {
                    Val = size.ToString()
                }
            );

            // Apply bold formatting when requested
            if (bold)
            {
                runProps.Append(new Bold());
            }

            // Create Word paragraph
            return new Paragraph(
                new Run(
                    runProps,
                    new Text(text)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    }
                )
            );
        }
    }
}
