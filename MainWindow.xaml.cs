using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using System.Windows;

using System.Windows.Media;

using System.Windows.Media.Animation;

using System.Windows.Shapes;

using WiringKitSOP.Models;

using WiringKitSOP.Services;



namespace WiringKitSOP;



public partial class MainWindow : Window

{

    private readonly PlcSettings _plcSettings;

    private readonly PlcService _plcService;

    private readonly SopManager _sop;

    private readonly ObservableCollection<EventLogItem> _events = new();

    private CancellationTokenSource? _runCts;

    private bool _paused;

    private bool _isRunning;

    private bool _connectionAnimated;

    private bool _tieAnimated;

    private bool _tiedAnimated;



    public MainWindow()

    {

        InitializeComponent();

        _plcSettings = ConfigurationService.LoadPlcSettings();

        _plcService = new PlcService(_plcSettings);

        _sop = new SopManager(ConfigurationService.LoadSteps());

        EventGrid.ItemsSource = _events;

        PinVisuals.ItemsSource = Enumerable.Range(1, 8).Select(i => i.ToString()).ToList();

        TotalStepsText.Text = _sop.Steps.Count.ToString();

        DateTimeText.Text = DateTime.Now.ToString("dd-MM-yyyy  HH:mm:ss");

        AddEvent("Dashboard Ready", $"DSE G4501 | {_sop.Steps.Count} configured wiring stages");

        RenderCurrentStep();

        UpdateProgress();

        _ = ClockLoopAsync();

        Closed += (_, _) => { _runCts?.Cancel(); _plcService.Dispose(); };

    }



    private async Task ClockLoopAsync()

    {

        while (true)

        {

            DateTimeText.Text = DateTime.Now.ToString("dd-MM-yyyy  HH:mm:ss");

            await Task.Delay(1000);

        }

    }



    private void RenderCurrentStep()

    {

        ResetAnimation();

        var step = _sop.Current;

        if (step == null)

        {

            StepTitle.Text = "All stages completed";

            StepState.Text = "TASK COMPLETED";

            StepProgress.Value = 100;

            AnimationStatus.Text = "All wiring stages completed";

            return;

        }

        StepTitle.Text = $"Step {step.Number} of {_sop.Steps.Count}";

        StepState.Text = "WIRING IN PROGRESS";

        WireColorText.Text = $"●  {step.WireColor}";

        WireColorText.Foreground = BrushFromHex(step.HexColor);

        FerruleText.Text = step.FerruleNumber.ToString();

        SourceText.Text = step.SourceTerminal;

        TargetText.Text = step.TargetPin;

        ConnectorText.Text = $"{step.ConnectorName}  /  {step.ControllerSlot}";

        ExpectedText.Text = step.ExpectedFeedback;

        InstructionList.ItemsSource = step.Instructions;

        AttemptText.Text = $"{_sop.Attempts} / {_plcSettings.MaxAttempts}";

        CurrentAttemptText.Text = AttemptText.Text;

        NextStepText.Text = _sop.CurrentIndex + 1 < _sop.Steps.Count ? $"Step {_sop.Steps[_sop.CurrentIndex + 1].Number}" : "Finish";

        StepProgress.Value = 0;

        WirePath.Stroke = BrushFromHex(step.HexColor);

        AnimationStatus.Text = $"Waiting for {step.WireColor} wire feedback...";

        UpdateStageOverview();

    }



    private void UpdateStageOverview()

    {

        var rows = _sop.Steps.Select(s => _sop.CompletedSteps.Contains(s.Number)

            ? $"🟢  Stage {s.Number} – {s.WireColor} (Completed)"

            : s.Number == _sop.Current?.Number ? $"🟡  Stage {s.Number} – {s.WireColor} (Current)" : $"⚪  Stage {s.Number} – {s.WireColor} (Pending)").ToList();

        StageList.ItemsSource = rows;

    }



    private void UpdateProgress()

    {

        var total = _sop.Steps.Count;

        var done = _sop.CompletedCount;

        var percent = total == 0 ? 0 : (double)done / total * 100;

        TaskProgress.Value = percent;

        ProgressPercent.Text = $"{percent:0}%";

        CompletedText.Text = done.ToString();

        RemainingText.Text = (total - done).ToString();

        TotalStepsText.Text = total.ToString();

        StepProgress.Value = _sop.Current == null ? 100 : percent;

        UpdateStageOverview();

    }



    private async void StartButton_Click(object sender, RoutedEventArgs e)

    {

        if (_isRunning || _sop.IsFinished) return;

        if (!DemoModeCheck.IsChecked.GetValueOrDefault() && !_plcService.IsConnected)

        {

            MessageBox.Show("Connect to the Siemens PLC first, or enable Demo Mode.", "PLC Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);

            return;

        }

        _runCts = new CancellationTokenSource();

        _paused = false;

        _isRunning = true;

        AddEvent("Task Started", DemoModeCheck.IsChecked == true ? "Simulation mode" : "Live Siemens PLC mode");

        try

        {

            if (DemoModeCheck.IsChecked == true) await RunDemoAsync(_runCts.Token);

            else await RunPlcPollingAsync(_runCts.Token);

        }

        catch (OperationCanceledException) { AddEvent("Task Stopped", "Task was aborted or reset by operator."); }

        catch (Exception ex) { AddEvent("Runtime Error", ex.Message); MessageBox.Show(ex.Message, "Dashboard Error", MessageBoxButton.OK, MessageBoxImage.Error); }

        finally { _isRunning = false; _paused = false; }

    }



    private async Task RunDemoAsync(CancellationToken token)

    {

        while (!_sop.IsFinished && !token.IsCancellationRequested)

        {

            await WaitIfPaused(token);

            var step = _sop.Current!;

            AddEvent($"Step {step.Number} Started", $"Wire: {step.WireColor} | Ferrule: {step.FerruleNumber} | Pin: {step.TargetPin}");

            await DemoDelay(900, token);

            ApplyFeedback(new StepFeedback { Connected = true });

            AddEvent("Wire Connected", $"{step.SourceTerminal} → {step.ConnectorName}/{step.TargetPin}");

            await DemoDelay(650, token);

            ApplyFeedback(new StepFeedback { Connected = true, TieGunActive = true });

            AddEvent("Tie Gun Activated", "PLC demo signal: tie gun active");

            await DemoDelay(900, token);

            ApplyFeedback(new StepFeedback { Connected = true, TieGunActive = true, Tied = true });

            AddEvent("Wire Tied", "Tie operation completed; green completion dot displayed.");

            await DemoDelay(500, token);

            ApplyFeedback(new StepFeedback { Connected = true, TieGunActive = false, Tied = true, Complete = true });

            CompleteCurrentStage();

        }

        if (_sop.IsFinished) FinishTask();

    }



    private async Task RunPlcPollingAsync(CancellationToken token)

    {

        while (!_sop.IsFinished && !token.IsCancellationRequested)

        {

            await WaitIfPaused(token);

            var step = _sop.Current!;

            StepFeedback feedback;

            try { feedback = await _plcService.ReadStepAsync(step.Signals); }

            catch (Exception ex) { PlcStatusText.Text = "● PLC Read Error"; PlcStatusText.Foreground = Brushes.Red; AddEvent("PLC Read Error", ex.Message); await Task.Delay(1000, token); continue; }

            ApplyFeedback(feedback);

            if (feedback.Connected && feedback.Tied && feedback.Complete)

            {

                CompleteCurrentStage();

                continue;

            }

            await Task.Delay(Math.Max(100, _plcSettings.PollIntervalMs), token);

        }

        if (_sop.IsFinished) FinishTask();

    }



    private async Task DemoDelay(int milliseconds, CancellationToken token)

    {

        var elapsed = 0;

        while (elapsed < milliseconds)

        {

            await WaitIfPaused(token);

            var slice = Math.Min(100, milliseconds - elapsed);

            await Task.Delay(slice, token);

            elapsed += slice;

        }

    }



    private async Task WaitIfPaused(CancellationToken token)

    {

        while (_paused) await Task.Delay(100, token);

        token.ThrowIfCancellationRequested();

    }



    private void ApplyFeedback(StepFeedback feedback)

    {

        if (feedback.Connected && !_connectionAnimated)

        {

            _connectionAnimated = true;

            WirePath.StrokeDashOffset = 90;

            WirePath.BeginAnimation(Shape.StrokeDashOffsetProperty, new DoubleAnimation(90, 0, TimeSpan.FromMilliseconds(900)));

            ConnectionStatus.Text = "🟢  Wire connected";

            AnimationStatus.Text = "Wire routed to target pin";

            FeedbackText.Text = "Connection OK";

            StepProgress.Value = 30;

        }

        if (feedback.TieGunActive && !_tieAnimated)

        {

            _tieAnimated = true;

            TieGun.Visibility = Visibility.Visible;

            var transform = new TranslateTransform(0, 0);

            TieGun.RenderTransform = transform;

            transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, 45, TimeSpan.FromMilliseconds(450)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(2) });

            TieStatus.Text = "🟡  Tie gun active";

            TorqueText.Text = "Tie gun operating...";

            TorqueProgress.Value = 65;

            TorquePercent.Text = "65%";

            StepProgress.Value = 60;

            AnimationStatus.Text = "Tie gun animation running";

        }

        if (feedback.Tied && !_tiedAnimated)

        {

            _tiedAnimated = true;

            ConnectionDot.Visibility = Visibility.Visible;

            ConnectionDot.Fill = Brushes.LimeGreen;

            ConnectionDot.Opacity = 0;

            ConnectionDot.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350)));

            TieStatus.Text = "🟢  Wire tied";

            TorqueText.Text = "Tie process OK";

            TorqueProgress.Value = 100;

            TorquePercent.Text = "100%";

            StepProgress.Value = 85;

            AnimationStatus.Text = "Green completion indicator ON";

        }

        if (feedback.Complete)

        {

            CompleteStatus.Text = "🟢  Stage complete signal";

            FeedbackText.Text = "PLC Confirmed";

            StepProgress.Value = 100;

        }

    }



    private void CompleteCurrentStage()

    {

        var finished = _sop.Current;

        if (finished == null) return;

        _sop.MarkCurrentComplete();

        AddEvent("Stage Completed", $"Stage {finished.Number} | {finished.WireColor} wire | {finished.TargetPin}");

        UpdateProgress();

        if (!_sop.IsFinished) RenderCurrentStep();

    }



    private void FinishTask()

    {

        StepTitle.Text = "All stages completed";

        StepState.Text = "TASK COMPLETED";

        FeedbackText.Text = "All stages OK";

        AnimationStatus.Text = "Wiring SOP completed successfully";

        AddEvent("Task Completed", $"All {_sop.Steps.Count} wiring stages completed.");

        UpdateProgress();

        try

        {

            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WiringKitSOPReports");

            var excelPath = ReportService.ExportExcel(_events, _sop.Steps, folder);

            var wordPath = ReportService.ExportWord(_events, _sop.Steps, folder);

            AddEvent("Reports Generated", $"Excel: {System.IO.Path.GetFileName(excelPath)} | Word: {System.IO.Path.GetFileName(wordPath)}");

            MessageBox.Show(
                $"All wiring stages completed.\n\nReports generated:\n{excelPath}\n{wordPath}",
                "SOP Completed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

        }

        catch (Exception ex)

        {

            AddEvent("Report Generation Failed", ex.Message);

            MessageBox.Show($"Task completed, but report generation failed: {ex.Message}", "SOP Completed", MessageBoxButton.OK, MessageBoxImage.Warning);

        }

    }



    private async void ConnectButton_Click(object sender, RoutedEventArgs e)

    {

        try

        {

            PlcStatusText.Text = "● Connecting...";

            PlcStatusText.Foreground = Brushes.DarkOrange;

            await _plcService.ConnectAsync();

            PlcStatusText.Text = $"● Connected ({_plcSettings.IpAddress})";

            PlcStatusText.Foreground = Brushes.Green;

            ModeLabel.Text = "REAL PLC MODE";

            AddEvent("PLC Connected", $"Siemens S7-1200 at {_plcSettings.IpAddress}");

            DemoModeCheck.IsChecked = false;

        }

        catch (Exception ex)

        {

            PlcStatusText.Text = "● Connection Failed";

            PlcStatusText.Foreground = Brushes.Red;

            AddEvent("PLC Connection Failed", ex.Message);

            MessageBox.Show($"Could not connect to PLC. Check IP, rack/slot, network and DB access.\n\n{ex.Message}", "PLC Connection", MessageBoxButton.OK, MessageBoxImage.Error);

        }

    }



    private void DemoModeCheck_Changed(object sender, RoutedEventArgs e)

    {

        if (ModeLabel == null || PlcStatusText == null) return;

        if (DemoModeCheck.IsChecked == true)

        {

            ModeLabel.Text = "SIMULATION MODE";

            PlcStatusText.Text = "● Demo Connected";

            PlcStatusText.Foreground = Brushes.Green;

        }

        else if (!_plcService.IsConnected)

        {

            ModeLabel.Text = "REAL PLC MODE – NOT CONNECTED";

            PlcStatusText.Text = "● Not Connected";

            PlcStatusText.Foreground = Brushes.Red;

        }

    }



    private void PauseButton_Click(object sender, RoutedEventArgs e) { if (_isRunning) { _paused = true; AddEvent("Task Paused", "Paused by operator."); FeedbackText.Text = "Paused"; } }

    private void ResumeButton_Click(object sender, RoutedEventArgs e) { if (_isRunning && _paused) { _paused = false; AddEvent("Task Resumed", "Resumed by operator."); } }

    private void AbortButton_Click(object sender, RoutedEventArgs e) { _runCts?.Cancel(); _paused = false; }



    private void ResetButton_Click(object sender, RoutedEventArgs e)

    {

        _runCts?.Cancel(); _isRunning = false; _paused = false; _sop.Reset();

        AddEvent("Dashboard Reset", "All stage progress cleared.");

        RenderCurrentStep(); UpdateProgress();

        ConnectionStatus.Text = "● Waiting for wire"; TieStatus.Text = "● Tie gun idle"; CompleteStatus.Text = "● Stage pending";

        TorqueProgress.Value = 0; TorquePercent.Text = "0%"; TorqueText.Text = "Waiting for PLC feedback...";

    }



    private void RetryButton_Click(object sender, RoutedEventArgs e)

    {

        if (_sop.IsFinished) return;

        if (_sop.Attempts >= _plcSettings.MaxAttempts)

        {

            AddEvent("Retry Limit Reached", $"Maximum {_plcSettings.MaxAttempts} attempts reached for stage {_sop.Current!.Number}.");

            MessageBox.Show("Maximum attempts reached. Please check the wiring and supervisor approval.", "Retry Limit", MessageBoxButton.OK, MessageBoxImage.Warning);

            return;

        }

        _sop.Retry(); RenderCurrentStep(); AddEvent("Retry", $"Stage {_sop.Current!.Number}, attempt {_sop.Attempts}.");

    }



    private void ExcelButton_Click(object sender, RoutedEventArgs e) => ExportReport(true);

    private void WordButton_Click(object sender, RoutedEventArgs e) => ExportReport(false);

    private void ExportReport(bool excel)

    {

        try

        {

            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WiringKitSOPReports");

            var path = excel ? ReportService.ExportExcel(_events, _sop.Steps, folder) : ReportService.ExportWord(_events, _sop.Steps, folder);

            AddEvent("Report Generated", path);

            MessageBox.Show($"Report saved successfully:\n{path}", "Report", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        catch (Exception ex) { MessageBox.Show(ex.Message, "Report Error", MessageBoxButton.OK, MessageBoxImage.Error); }

    }



    private void ResetAnimation()

    {

        _connectionAnimated = _tieAnimated = _tiedAnimated = false;

        WirePath.BeginAnimation(Shape.StrokeDashOffsetProperty, null);

        WirePath.StrokeDashOffset = 90;

        ConnectionDot.Visibility = Visibility.Hidden;

        TieGun.Visibility = Visibility.Hidden;

        ConnectionStatus.Text = "● Waiting for wire";

        TieStatus.Text = "● Tie gun idle";

        CompleteStatus.Text = "● Stage pending";

        TorqueProgress.Value = 0; TorquePercent.Text = "0%"; TorqueText.Text = "Waiting for PLC feedback...";

        FeedbackText.Text = "Waiting";

    }



    private void AddEvent(string name, string details)

    {

        _events.Insert(0, new EventLogItem { Time = DateTime.Now, Event = name, Details = details });

        while (_events.Count > 200) _events.RemoveAt(_events.Count - 1);

    }



    private static Brush BrushFromHex(string hex)

    {

        try { return (Brush)new BrushConverter().ConvertFromString(hex)!; }

        catch { return Brushes.Red; }

    }

}
