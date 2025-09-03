using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using CarelessWhisperV2.Models;
using CarelessWhisperV2.Services.Clipboard;
using CarelessWhisperV2.Services.Logging;

namespace CarelessWhisperV2.Views;

public partial class TranscriptionHistoryWindow : Window
{
    private readonly ITranscriptionLogger _logger;
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<TranscriptionHistoryWindow> _systemLogger;
    
    private ObservableCollection<TranscriptionHistoryItem> _transcriptions;
    private ICollectionView _transcriptionsView;
    private string _searchFilter = "";

    public TranscriptionHistoryWindow(
        ITranscriptionLogger logger,
        IClipboardService clipboardService,
        ILogger<TranscriptionHistoryWindow> systemLogger)
    {
        InitializeComponent();
        _logger = logger;
        _clipboardService = clipboardService;
        _systemLogger = systemLogger;
        
        _transcriptions = new ObservableCollection<TranscriptionHistoryItem>();
        _transcriptionsView = CollectionViewSource.GetDefaultView(_transcriptions);
        _transcriptionsView.Filter = FilterTranscriptions;
        
        TranscriptionListView.ItemsSource = _transcriptionsView;
        
        Loaded += TranscriptionHistoryWindow_Loaded;
    }

    private async void TranscriptionHistoryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadTranscriptionHistory();
    }

    private async Task LoadTranscriptionHistory()
    {
        try
        {
            StatusTextBlock.Text = "Loading transcription history...";
            
            var entries = await _logger.GetTranscriptionHistoryAsync();
            
            _transcriptions.Clear();
            
            foreach (var entry in entries.OrderByDescending(e => e.Timestamp))
            {
                _transcriptions.Add(new TranscriptionHistoryItem(entry));
            }
            
            UpdateCountDisplay();
            StatusTextBlock.Text = "Ready";
        }
        catch (Exception ex)
        {
            _systemLogger.LogError(ex, "Failed to load transcription history");
            StatusTextBlock.Text = "Error loading history";
            MessageBox.Show($"Failed to load transcription history: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateCountDisplay()
    {
        var totalCount = _transcriptions.Count;
        var filteredCount = _transcriptionsView.Cast<object>().Count();
        
        if (!string.IsNullOrEmpty(_searchFilter))
        {
            CountTextBlock.Text = $"{filteredCount} of {totalCount} entries";
        }
        else
        {
            CountTextBlock.Text = $"{totalCount} entries";
        }
    }

    private bool FilterTranscriptions(object item)
    {
        if (string.IsNullOrEmpty(_searchFilter))
            return true;
            
        if (item is TranscriptionHistoryItem transcription)
        {
            return transcription.FullText.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                   transcription.TimestampFormatted.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase);
        }
        
        return false;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchFilter = SearchTextBox.Text;
        _transcriptionsView.Refresh();
        UpdateCountDisplay();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadTranscriptionHistory();
    }

    private void TranscriptionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TranscriptionListView.SelectedItem is TranscriptionHistoryItem selected)
        {
            ShowTranscriptionDetails(selected);
            CopyTextButton.IsEnabled = true;
            DeleteEntryButton.IsEnabled = true;
            
            // Show play audio button if audio file exists
            if (!string.IsNullOrEmpty(selected.AudioFilePath) && File.Exists(selected.AudioFilePath))
            {
                PlayAudioButton.Visibility = Visibility.Visible;
                PlayAudioButton.IsEnabled = true;
            }
            else
            {
                PlayAudioButton.Visibility = Visibility.Collapsed;
                PlayAudioButton.IsEnabled = false;
            }
        }
        else
        {
            ClearTranscriptionDetails();
            CopyTextButton.IsEnabled = false;
            DeleteEntryButton.IsEnabled = false;
            PlayAudioButton.IsEnabled = false;
            PlayAudioButton.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowTranscriptionDetails(TranscriptionHistoryItem item)
    {
        FullTextTextBlock.Text = item.FullText;
        TimestampDetail.Text = $"Recorded: {item.TimestampFormatted}";
        DurationDetail.Text = $"Duration: {item.DurationFormatted}";
        ModelDetail.Text = $"Model: {item.ModelUsed}";
        ProcessingTimeDetail.Text = $"Processing: {item.ProcessingDuration:F1}s";
        LanguageDetail.Text = $"Language: {item.Language}";
        CharacterCountDetail.Text = $"Characters: {item.CharacterCount}";
        SegmentCountDetail.Text = $"Segments: {item.SegmentCount}";
    }

    private void ClearTranscriptionDetails()
    {
        FullTextTextBlock.Text = "";
        TimestampDetail.Text = "";
        DurationDetail.Text = "";
        ModelDetail.Text = "";
        ProcessingTimeDetail.Text = "";
        LanguageDetail.Text = "";
        CharacterCountDetail.Text = "";
        SegmentCountDetail.Text = "";
    }

    private async void CopyText_Click(object sender, RoutedEventArgs e)
    {
        if (TranscriptionListView.SelectedItem is TranscriptionHistoryItem selected)
        {
            try
            {
                await _clipboardService.SetTextAsync(selected.FullText);
                StatusTextBlock.Text = "Text copied to clipboard";
            }
            catch (Exception ex)
            {
                _systemLogger.LogError(ex, "Failed to copy text to clipboard");
                StatusTextBlock.Text = "Failed to copy text";
            }
        }
    }

    private void PlayAudio_Click(object sender, RoutedEventArgs e)
    {
        if (TranscriptionListView.SelectedItem is TranscriptionHistoryItem selected && 
            !string.IsNullOrEmpty(selected.AudioFilePath) && 
            File.Exists(selected.AudioFilePath))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = selected.AudioFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _systemLogger.LogError(ex, "Failed to play audio file");
                MessageBox.Show("Failed to play audio file. Make sure you have an audio player installed.", 
                    "Audio Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void DeleteEntry_Click(object sender, RoutedEventArgs e)
    {
        if (TranscriptionListView.SelectedItem is TranscriptionHistoryItem selected)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete this transcription?\n\nDate: {selected.TimestampFormatted}\nText: {selected.TextPreview}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _logger.DeleteTranscriptionAsync(selected.Id);
                    _transcriptions.Remove(selected);
                    UpdateCountDisplay();
                    StatusTextBlock.Text = "Entry deleted";
                }
                catch (Exception ex)
                {
                    _systemLogger.LogError(ex, "Failed to delete transcription entry");
                    MessageBox.Show($"Failed to delete entry: {ex.Message}", "Delete Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Export Transcription History",
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv",
                DefaultExt = "json",
                FileName = $"transcription_history_{DateTime.Now:yyyyMMdd}"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                var visibleItems = _transcriptionsView.Cast<TranscriptionHistoryItem>().ToList();
                
                switch (Path.GetExtension(saveDialog.FileName).ToLower())
                {
                    case ".json":
                        await ExportAsJson(saveDialog.FileName, visibleItems);
                        break;
                    case ".txt":
                        await ExportAsText(saveDialog.FileName, visibleItems);
                        break;
                    case ".csv":
                        await ExportAsCsv(saveDialog.FileName, visibleItems);
                        break;
                }
                
                StatusTextBlock.Text = $"Exported {visibleItems.Count} entries";
                MessageBox.Show($"Successfully exported {visibleItems.Count} transcriptions to {saveDialog.FileName}", 
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _systemLogger.LogError(ex, "Export failed");
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportAsJson(string fileName, List<TranscriptionHistoryItem> items)
    {
        var exportData = items.Select(item => new
        {
            item.Id,
            item.Timestamp,
            item.FullText,
            item.ModelUsed,
            item.Language,
            item.ProcessingDuration,
            Segments = item.SegmentCount,
            Characters = item.CharacterCount
        });
        
        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(fileName, json);
    }

    private async Task ExportAsText(string fileName, List<TranscriptionHistoryItem> items)
    {
        using var writer = new StreamWriter(fileName);
        
        await writer.WriteLineAsync("Transcription History Export");
        await writer.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync($"Total Entries: {items.Count}");
        await writer.WriteLineAsync(new string('=', 50));
        
        foreach (var item in items)
        {
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"Date/Time: {item.TimestampFormatted}");
            await writer.WriteLineAsync($"Model: {item.ModelUsed}");
            await writer.WriteLineAsync($"Language: {item.Language}");
            await writer.WriteLineAsync($"Duration: {item.DurationFormatted}");
            await writer.WriteLineAsync($"Characters: {item.CharacterCount}");
            await writer.WriteLineAsync("Text:");
            await writer.WriteLineAsync(item.FullText);
            await writer.WriteLineAsync(new string('-', 30));
        }
    }

    private async Task ExportAsCsv(string fileName, List<TranscriptionHistoryItem> items)
    {
        using var writer = new StreamWriter(fileName);
        
        // Header
        await writer.WriteLineAsync("Timestamp,Model,Language,Duration,Characters,Text");
        
        // Data
        foreach (var item in items)
        {
            var escapedText = item.FullText.Replace("\"", "\"\"");
            await writer.WriteLineAsync($"{item.Timestamp:yyyy-MM-dd HH:mm:ss},{item.ModelUsed},{item.Language},{item.DurationFormatted},{item.CharacterCount},\"{escapedText}\"");
        }
    }

    private async void Cleanup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CleanupDialog();
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var deletedCount = await _logger.CleanupOldTranscriptionsAsync(dialog.DaysToKeep);
                await LoadTranscriptionHistory(); // Refresh the list
                
                StatusTextBlock.Text = $"Deleted {deletedCount} old entries";
                MessageBox.Show($"Successfully deleted {deletedCount} old transcription entries.", 
                    "Cleanup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _systemLogger.LogError(ex, "Cleanup failed");
                MessageBox.Show($"Cleanup failed: {ex.Message}", "Cleanup Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is GridViewColumnHeader headerClicked)
        {
            var direction = ListSortDirection.Ascending;
            
            if (_transcriptionsView.SortDescriptions.Count > 0)
            {
                var currentSort = _transcriptionsView.SortDescriptions[0];
                if (currentSort.Direction == ListSortDirection.Ascending)
                    direction = ListSortDirection.Descending;
            }
            
            var propertyName = headerClicked.Column.Header.ToString() switch
            {
                "Date/Time" => nameof(TranscriptionHistoryItem.Timestamp),
                "Duration" => nameof(TranscriptionHistoryItem.Duration),
                "Model" => nameof(TranscriptionHistoryItem.ModelUsed),
                "Characters" => nameof(TranscriptionHistoryItem.CharacterCount),
                _ => nameof(TranscriptionHistoryItem.Timestamp)
            };
            
            _transcriptionsView.SortDescriptions.Clear();
            _transcriptionsView.SortDescriptions.Add(new SortDescription(propertyName, direction));
        }
    }
}

public class TranscriptionHistoryItem
{
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string FullText { get; set; }
    public string ModelUsed { get; set; }
    public string Language { get; set; }
    public double ProcessingDuration { get; set; }
    public int SegmentCount { get; set; }
    public string AudioFilePath { get; set; }
    public TimeSpan Duration { get; set; }

    public TranscriptionHistoryItem(TranscriptionEntry entry)
    {
        Id = entry.Id;
        Timestamp = entry.Timestamp;
        FullText = entry.FullText;
        ModelUsed = entry.ModelUsed ?? "Unknown";
        Language = entry.Language ?? "auto";
        ProcessingDuration = entry.ProcessingDuration;
        SegmentCount = entry.Segments?.Count ?? 0;
        AudioFilePath = entry.AudioFilePath ?? "";
        
        // Calculate duration from segments if available
        if (entry.Segments?.Count > 0)
        {
            var lastSegment = entry.Segments.LastOrDefault();
            Duration = lastSegment?.End ?? TimeSpan.Zero;
        }
    }

    public string TimestampFormatted => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    public string DurationFormatted => Duration.TotalSeconds > 0 ? $"{Duration.TotalSeconds:F1}s" : "N/A";
    public string TextPreview => FullText.Length > 100 ? FullText.Substring(0, 100) + "..." : FullText;
    public int CharacterCount => FullText.Length;
}

public partial class CleanupDialog : Window
{
    public int DaysToKeep { get; private set; } = 30;

    public CleanupDialog()
    {
        Width = 400;
        Height = 200;
        Title = "Cleanup Old Transcriptions";
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = "Delete transcriptions older than:",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(titleText, 0);

        var daysPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var daysTextBox = new TextBox { Text = "30", Width = 60, Margin = new Thickness(0, 0, 10, 0) };
        var daysLabel = new TextBlock { Text = "days", VerticalAlignment = VerticalAlignment.Center };
        daysPanel.Children.Add(daysTextBox);
        daysPanel.Children.Add(daysLabel);
        Grid.SetRow(daysPanel, 1);

        var warningText = new TextBlock
        {
            Text = "Warning: This action cannot be undone!",
            Foreground = System.Windows.Media.Brushes.Red,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(warningText, 2);

        var buttonPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };
        
        var okButton = new Button 
        { 
            Content = "Delete", 
            Width = 80, 
            Margin = new Thickness(0, 0, 10, 0),
            Background = System.Windows.Media.Brushes.LightCoral
        };
        
        var cancelButton = new Button { Content = "Cancel", Width = 80 };
        
        okButton.Click += (s, e) =>
        {
            if (int.TryParse(daysTextBox.Text, out var days) && days > 0)
            {
                DaysToKeep = days;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid number of days.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        
        cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        Grid.SetRow(buttonPanel, 4);

        grid.Children.Add(titleText);
        grid.Children.Add(daysPanel);
        grid.Children.Add(warningText);
        grid.Children.Add(buttonPanel);

        Content = grid;
    }
}
