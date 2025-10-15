using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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

        private NamedPipeServerStream pipeServer;
        private Thread pipeThread;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            StartPipeServer();
        }

        private void StartPipeServer()
        {
            pipeThread = new Thread(() =>
            {
                try
                {
                    pipeServer = new NamedPipeServerStream("rfid_pipe", PipeDirection.In);
                    pipeServer.WaitForConnection();

                    using (var reader = new StreamReader(pipeServer))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            try
                            {
                                var tag = JsonSerializer.Deserialize<TagInfo>(line);
                                if (tag != null)
                                {
                                    // Update UI thread safely
                                    Dispatcher.Invoke(() =>
                                    {
                                        Tags.Add(tag);
                                    });
                                }
                            }
                            catch (JsonException ex)
                            {
                                // handle malformed JSON
                                Console.WriteLine("JSON error: " + ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Pipe server error: " + ex.Message);
                    });
                }
            });
            pipeThread.IsBackground = true;
            pipeThread.Start();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            try
            {
                pipeServer?.Dispose();
                pipeThread?.Abort();
            }
            catch { }
        }
    }
}