using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace WpfApp1
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<TagInfo> _tags = new ObservableCollection<TagInfo>();
        public ObservableCollection<TagInfo> Tags
        {
            get => _tags;
            set
            {
                _tags = value;
                OnPropertyChanged(nameof(Tags));
            }
        }

        private NamedPipeClientStream pipeClient;
        private Thread pipeThread;
        private bool running = true;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            StartPipeClient();
        }

        private void StartPipeClient()
        {
            pipeThread = new Thread(() =>
            {
                try
                {
                    pipeClient = new NamedPipeClientStream(".", "rfid_multi_pipe", PipeDirection.In);
                    pipeClient.Connect(5000); // Timeout: 5 seconds

                    using (var reader = new StreamReader(pipeClient))
                    {
                        string line;
                        while (running && (line = reader.ReadLine()) != null)
                        {
                            try
                            {
                                var tag = JsonSerializer.Deserialize<TagInfo>(line);
                                if (tag != null)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        Tags.Add(tag);
                                    });
                                }
                            }
                            catch (JsonException ex)
                            {
                                Console.WriteLine("JSON error: " + ex.Message);
                            }
                        }
                    }
                }
                catch (TimeoutException)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Could not connect to the RFID pipe server (timeout).");
                    });
                }
                catch (IOException ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Pipe I/O error: " + ex.Message);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Unexpected error: " + ex.Message);
                    });
                }
            });

            pipeThread.IsBackground = true;
            pipeThread.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            running = false;

            try
            {
                pipeClient?.Dispose();
            }
            catch { }

            try
            {
                pipeThread?.Join(1000); // Wait up to 1 second
            }
            catch { }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Assuming you have a TagInfo class like this:
  
}
