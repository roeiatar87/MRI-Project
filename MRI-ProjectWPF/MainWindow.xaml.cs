using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;
using WpfPostItDetector.GENERAL;
using Color = System.Drawing.Color;

namespace WpfPostItDetector
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            gen = new General();
            _motionDetector = new MotionDetector(new TwoFramesDifferenceDetector(), new MotionBorderHighlighting());
            GetVideoDevices();
            MotionThreshold = gen.Threshold_defVal;
            ThresholdMotionTime = gen.ThresholdMotionTime;
            pictureBox_NotMoving.Visibility = Visibility.Hidden;
            pictureBox_Moving.Visibility = Visibility.Hidden;
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            microphoneListener = new MicrophoneListener();
        }
        private MicrophoneListener microphoneListener;
        private DispatcherTimer sliderTimer;
        private MediaPlayer mediaPlayer;


        public ObservableCollection<FilterInfo> VideoDevices { get; } = new ObservableCollection<FilterInfo>();
        public ObservableCollection<VideoCapabilities> Resolutions { get; } = new ObservableCollection<VideoCapabilities>();
        public ObservableCollection<string> Resolutions_st { get; } = new ObservableCollection<string>();
        public General gen;
        private bool _isInMotion = false; // Track if currently in motion
        public bool MotionDetectionEnabled
        {
            get => _motionDetectionEnabled;
            set
            {
                _motionDetectionEnabled = value;
                OnPropertyChanged(nameof(MotionDetectionEnabled));
            }
        }
        private bool _isMicrophoneEnabled;
        public bool IsMicrophoneEnabled
        {
            get => _isMicrophoneEnabled;
            set
            {
                if (_isMicrophoneEnabled != value)
                {
                    _isMicrophoneEnabled = value;
                    OnPropertyChanged(nameof(IsMicrophoneEnabled));
                    if (value)
                    {
                        microphoneListener.StartListening();
                    }
                    else
                    {
                        microphoneListener.StopListening();
                    }
                }
            }
        }
        public double MotionThreshold
        {
            get => _motionThreshold * 100000;
            set
            {
                _motionThreshold = value / 100000;
                OnPropertyChanged(nameof(MotionThreshold));
            }
        }
        private double _thresholdMotionTime;
        public double ThresholdMotionTime
        {
            get => _thresholdMotionTime;
            set
            {

                _thresholdMotionTime = value;
                OnPropertyChanged(nameof(ThresholdMotionTime));

            }
        }
        public FilterInfo CurrentDevice
        {
            get => _currentDevice;
            set
            {
                _currentDevice = value;
                OnPropertyChanged(nameof(CurrentDevice));
                if (value != null)
                    GetResolutions(value);
            }
        }
        public VideoCapabilities SelectedResolution
        {
            get => _selectedResolution;
            set
            {
                _selectedResolution = value;
                if (value != null)
                {
                    StopCameraAsync();
                }
                OnPropertyChanged(nameof(SelectedResolution));
            }
        }
        public TimeSpan RemainingTime
        {
            get { return _remainingTime; }
            set
            {
                _remainingTime = value;
                OnPropertyChanged(nameof(RemainingTime));
                OnPropertyChanged(nameof(RemainingTimeFormatted)); // Notify UI of the formatted time change
            }
        }
        public string RemainingTimeFormatted
        {
            get { return RemainingTime.ToString(@"hh\:mm\:ss"); } // Format remaining time as "hh:mm:ss"
        }
        public double Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                OnPropertyChanged(nameof(Volume));
                if (mediaPlayer != null)
                    mediaPlayer.Volume = value;
                VolumePercentage = (int)(value * 100);
            }
        }
        public int SelectedRes
        {
            get => _selectedRes;
            set
            {
                _selectedRes = value;
                SelectedResolution = Resolutions.Count > 0 ? Resolutions[_selectedRes] : null;
                OnPropertyChanged(nameof(SelectedRes));
            }
        }
        private int _volumePercentage = 90;
        public int VolumePercentage
        {
            get { return _volumePercentage; }
            set
            {
                _volumePercentage = value;
                OnPropertyChanged(nameof(VolumePercentage));
            }
        }
        private double _volume = 0.9; // Default volume
        private DateTime _shiftStartTime;
        private readonly MotionDetector _motionDetector;
        private IVideoSource _videoSource;
        private bool _motionDetectionEnabled;
        private double _motionThreshold = 0.01;
        private FilterInfo _currentDevice;
        private VideoCapabilities _selectedResolution;
        private int _selectedRes;
        private int _motionNumber;
        public int MotionNumber
        {
            get => _motionNumber;
            set
            {
                _motionNumber = value;
                OnPropertyChanged(nameof(MotionNumber));
            }
        }
        private TimeSpan _remainingTime;

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            await StartCameraAsync();
        }

        private async void btnStop_Click(object sender, RoutedEventArgs e)
        {
            await StopCameraAsync();
        }

        private void btnPlaySound_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the base directory of the application
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Combine the base directory with the relative path to the sound file
                string audioFilePath = Path.Combine(baseDirectory, "mrisound.mp3");

                if (File.Exists(audioFilePath))
                {
                    // Set the media player's media source to the audio file path
                    mediaPlayer.Open(new Uri(audioFilePath));

                    // Set the position of the media player based on the slider value
                    mediaPlayer.Position = TimeSpan.FromSeconds(audioSlider.Value);

                    // Play the audio
                    mediaPlayer.Play();

                }
                else
                {
                    MessageBox.Show("The sound file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing sound: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            try
            {
                double totalDuration = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;


                audioSlider.Maximum = totalDuration;

                // Initialize and start the timer
                sliderTimer = new DispatcherTimer();
                sliderTimer.Interval = TimeSpan.FromSeconds(1);
                sliderTimer.Tick += SliderTimer_Tick;
                sliderTimer.Start();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }

        }

        private void SliderTimer_Tick(object sender, EventArgs e)
        {
            RemainingTime = mediaPlayer.NaturalDuration.TimeSpan - mediaPlayer.Position;
            audioSlider.Value = mediaPlayer.Position.TotalSeconds;

        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            // Reset the position of the slider when media playback ends
            audioSlider.Value = 0;
            mediaPlayer.Stop();
        }

        private void audioSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        private void btnStopSound_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
        }

        private async void video_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    if (MotionDetectionEnabled)
                    {
                        double motionValue = _motionDetector.ProcessFrame(bitmap);
                        await Application.Current?.Dispatcher.InvokeAsync(() =>
                        {
                            if (motionValue > _motionThreshold)
                            {
                                pictureBox_NotMoving.Visibility = Visibility.Hidden;
                                pictureBox_Moving.Visibility = Visibility.Visible;
                                TimeSpan displacementDuration = DateTime.Now - _shiftStartTime;
                                labelShiftDuration.Content = $"זמן תנועה: {displacementDuration.TotalSeconds:F4} שניות";

                                // Check if motion duration exceeds the threshold
                                if (displacementDuration.TotalSeconds > ThresholdMotionTime)
                                {
                                    // Only increment MotionNumber if it was not already counted for this motion
                                    if (!_isInMotion)
                                    {
                                        MotionNumber++;
                                        _isInMotion = true;
                                    }
                                }
                            }
                            else
                            {
                                pictureBox_NotMoving.Visibility = Visibility.Visible;
                                pictureBox_Moving.Visibility = Visibility.Hidden;
                                _shiftStartTime = DateTime.Now;
                                _isInMotion = false; // Reset motion state
                            }
                        });
                    }
                    else
                    {
                        await Application.Current?.Dispatcher.InvokeAsync(() =>
                        {
                            pictureBox_NotMoving.Visibility = Visibility.Hidden;
                            pictureBox_Moving.Visibility = Visibility.Hidden;
                        });
                    }
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    var bi = bitmap.ToBitmapImage();
                    bi.Freeze();
                    if (Application.Current != null)
                        await Application.Current?.Dispatcher.InvokeAsync(new Action(() => videoPlayer.Source = bi));
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Error on _videoSource_NewFrame:\n{exc.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                await StopCameraAsync();
            }
        }


        private void thresholdTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text) && e.Text != ".";
        }

        private void thresholdTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (double.TryParse(thresholdTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double threshold))
                    MotionThreshold = threshold < 1 ? 1 : (threshold > 1000 ? 1000 : threshold);
                else
                    thresholdTextBox.Text = MotionThreshold.ToString("F4", CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }

        }

        private bool IsNumeric(string input)
        {
            return input.All(char.IsDigit) || input.Count(c => c == '.') <= 1;
        }


        private void GetVideoDevices()
        {
            try
            {
                foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice))
                    VideoDevices.Add(filterInfo);

                if (VideoDevices.Any())
                    if (VideoDevices.Count > 1)
                        CurrentDevice = VideoDevices[gen.DefaultCamera];
                    else CurrentDevice = VideoDevices[0];
                else
                    MessageBox.Show("No video sources found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }

        }

        private async Task StartCameraAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (_videoSource == null)
                        if (CurrentDevice != null)
                        {
                            _videoSource = new VideoCaptureDevice(CurrentDevice.MonikerString);
                            SetCameraResolution(_videoSource, SelectedResolution.FrameSize.Width, SelectedResolution.FrameSize.Height);
                            _videoSource.NewFrame += video_NewFrame;
                            _videoSource.Start();
                        }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }

        }

        private void SetCameraResolution(IVideoSource videoSource, int desiredWidth, int desiredHeight)
        {
            try
            {
                if (videoSource is VideoCaptureDevice captureDevice)
                {
                    VideoCapabilities[] capabilities = captureDevice.VideoCapabilities;
                    VideoCapabilities desiredResolution = capabilities.OrderByDescending(cap => cap.FrameSize.Width * cap.FrameSize.Height).FirstOrDefault(cap => cap.FrameSize.Width <= desiredWidth && cap.FrameSize.Height <= desiredHeight);
                    if (desiredResolution != null)
                    {
                        captureDevice.VideoResolution = desiredResolution;
                    }
                    else
                    {
                        MessageBox.Show("Desired resolution not supported. Using default resolution.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }

        }

        private void GetResolutions(FilterInfo device)
        {
            try
            {
                if (device != null)
                {
                    Resolutions.Clear();
                    Resolutions_st.Clear();
                    VideoCaptureDevice videoDevice = new VideoCaptureDevice(device.MonikerString);
                    VideoCapabilities[] capabilities = videoDevice.VideoCapabilities;
                    Array.Sort(capabilities, (x, y) => (y.FrameSize.Width * y.FrameSize.Height).CompareTo(x.FrameSize.Width * x.FrameSize.Height));
                    foreach (VideoCapabilities capability in capabilities)
                    {
                        if (capability.AverageFrameRate >= 30)
                        {
                            Resolutions.Add(capability);
                            Resolutions_st.Add(capability.FrameSize.ToString());
                        }
                    }
                    if (Resolutions.Any())
                        SelectedResolution = Resolutions.First();
                    SelectedRes = 0;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);

            }

        }

        private async Task StopCameraAsync()
        {
            try
            {
                if (_videoSource != null && _videoSource.IsRunning)
                {
                    _videoSource.SignalToStop();
                    _videoSource.NewFrame -= video_NewFrame;

                    // Wait for a short delay before releasing resources
                    await Task.Delay(100);

                    // Dispose the video source
                    _videoSource.Stop();
                    _videoSource.WaitForStop();
                    _videoSource = null;

                    // Clear the video player source
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        videoPlayer.Source = null;
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName == nameof(VolumePercentage))
            {
                OnPropertyChanged(nameof(VolumePercentageWithSymbol));
            }
        }
        public string VolumePercentageWithSymbol
        {
            get { return $"{VolumePercentage}%"; }
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int val = int.Parse(MotionThreshold.ToString("F0"));
                gen.SaveThresholdValue(val);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void StartProgram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnStart_Click(sender, e);
                btnPlaySound_Click(sender, e);
                MotionDetectionEnabled = true;
                MotionNumber = 0;
            }
            catch (Exception ex)
            {

                MessageBox.Show($"{ex.Message}");
            }

        }

        private void Stop_Program_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnStop_Click(sender, e);
                btnStopSound_Click(sender, e);
                MotionDetectionEnabled = false;
                pictureBox_NotMoving.Visibility = Visibility.Hidden;
                pictureBox_Moving.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}");

            }

        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            await StopCameraAsync();
        }

        private void btnSaveCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int val = comboBox.SelectedIndex;
                gen.SaveCameraValue(val);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void thresholdMotionTime_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(thresholdMotionTime_TextBox.Text, e.Text);
        }
        private static bool IsTextAllowed(string currentText, string newText)
        {
            var fullText = currentText + newText;
            return Regex.IsMatch(fullText, @"^\d*\.?\d*$");
        }
        private void btnSaveTime_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double val = double.Parse(ThresholdMotionTime.ToString());
                gen.SaveMotionTimeValue(val);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void thresholdMotionTime_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (double.TryParse(thresholdMotionTime_TextBox.Text, out double result))
                {
                    ThresholdMotionTime = result;
                }
                else
                {
                    thresholdMotionTime_TextBox.Text = ThresholdMotionTime.ToString(CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}");

            }

        }

        // Start: Button Close | Restore | Minimize 
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        // End: Button Close | Restore | Minimize

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {

            MessageBox.Show("© התוכנה פותחה ועוצבה על ידי רועי עטר, כל הזכויות שמורות.\n", "אודות");

        }
    }
}
