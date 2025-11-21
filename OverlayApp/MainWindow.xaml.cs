using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using OverlayApp.Models;
using OverlayApp.Services;

namespace OverlayApp
{
    public partial class MainWindow : Window
    {
        private TextAnalyzer? _analyzer;
        private DictionaryService? _dictService;
        private AnkiService _ankiService = new AnkiService();

        private IntPtr _windowHandle;
        private IntPtr _nextClipboardViewer;
        private string _lastClipboardText = "";
        private bool _isPaused = false;

        private List<DictionaryEntry>? _currentResults;

        const int WM_DRAWCLIPBOARD = 0x0308;
        const int WM_CHANGECBCHAIN = 0x0309;

        [DllImport("User32.dll")] private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);
        [DllImport("User32.dll")] private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                _analyzer = new TextAnalyzer();
                _dictService = new DictionaryService();
            }
            catch { }
        }

        // --- CRITICAL FIX FOR CRASHING WHILE MOVING ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // 1. Close the popup. Moving a window with a complex popup open often crashes WPF.
                DictPopup.IsOpen = false;

                // 2. Wrap DragMove in try/catch. 
                // WPF throws an exception if the mouse is released "too quickly" during a drag.
                try
                {
                    this.DragMove();
                }
                catch
                {
                    // Suppress the crash. The drag just won't happen for that split second, which is fine.
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OpacitySlider_ValueChanged(OpacitySlider, new RoutedPropertyChangedEventArgs<double>(0, OpacitySlider.Value));

            if (_dictService != null)
            {
                _dictService.StatusUpdate += (msg) => Dispatcher.Invoke(() => StatusText.Text = msg);
                try
                {
                    await _dictService.InitializeAsync("jitendex.zip");
                    StatusText.Text = "Ready";
                }
                catch
                {
                    StatusText.Text = "Dict Load Error";
                }
            }

            _windowHandle = new WindowInteropHelper(this).Handle;
            _nextClipboardViewer = SetClipboardViewer(_windowHandle);
            HwndSource.FromHwnd(_windowHandle)?.AddHook(WndProc);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ChangeClipboardChain(_windowHandle, _nextClipboardViewer);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DRAWCLIPBOARD) { CheckClipboard(); SendMessage(_nextClipboardViewer, msg, wParam, lParam); }
            else if (msg == WM_CHANGECBCHAIN)
            {
                if (wParam == _nextClipboardViewer) _nextClipboardViewer = lParam;
                else SendMessage(_nextClipboardViewer, msg, wParam, lParam);
            }
            return IntPtr.Zero;
        }

        private void CheckClipboard()
        {
            if (_isPaused) return;
            try
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    if (text != _lastClipboardText && !string.IsNullOrWhiteSpace(text))
                    {
                        _lastClipboardText = text;
                        if (_analyzer != null)
                        {
                            var tokens = _analyzer.Analyze(text);
                            Dispatcher.Invoke(() => TextContainer.ItemsSource = tokens);
                        }
                    }
                }
            }
            catch { }
        }

        private void Word_Click(object sender, RoutedEventArgs e)
        {
            if (_dictService == null || !_dictService.IsLoaded) return;

            if (sender is Button btn && btn.Tag is Token token)
            {
                if (!token.IsWord) return;

                var entries = _dictService.Lookup(token.OriginalForm);
                if (entries.Count == 0) entries = _dictService.Lookup(token.Surface);

                if (entries.Count > 0)
                {
                    _currentResults = entries;
                    PopEntries.ItemsSource = entries;
                    DictPopup.IsOpen = true;
                }
            }
        }

        private async void AddToAnki_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResults == null || _currentResults.Count == 0) return;

            var entry = _currentResults[0];

            var sb = new System.Text.StringBuilder();
            int sCount = 1;
            foreach (var sense in entry.Senses)
            {
                string tags = string.Join(", ", sense.PoSTags);
                if (!string.IsNullOrEmpty(tags)) sb.Append($"<div style='color:#61AFEF; font-size:0.8em'>[{tags}]</div>");

                foreach (var def in sense.Glossaries)
                {
                    sb.Append($"<div style='margin-left:5px'>{sCount}. {def}</div>");
                }
                sCount++;
                sb.Append("<br>");
            }

            string result = await _ankiService.AddNote(TxtDeck.Text, entry.Headword, entry.Reading, sb.ToString(), _lastClipboardText);

            if (!result.Contains("Error"))
            {
                DictPopup.IsOpen = false;
                StatusText.Text = "Added to Anki!";
            }
            else
            {
                StatusText.Text = "Anki Error";
            }
        }

        private async void TestAnki_Click(object sender, RoutedEventArgs e)
        {
            bool c = await _ankiService.CheckConnection();
            AnkiStatus.Text = c ? "Connected" : "Failed";
            AnkiStatus.Foreground = c ? Brushes.LightGreen : Brushes.Red;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ChkPause_Checked(object sender, RoutedEventArgs e)
        {
            _isPaused = ChkPause.IsChecked ?? false;
            StatusText.Text = _isPaused ? "Paused" : "Ready";
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            byte alpha = (byte)(e.NewValue * 255);
            this.Background = new SolidColorBrush(Color.FromArgb(alpha, 30, 30, 30));
        }
    }
}