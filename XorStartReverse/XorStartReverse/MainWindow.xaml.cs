using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace XorStartReverse
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private object pause = new object();
        private bool interrupt = false;

        private bool isEncrypt = true;
        public bool IsEncrypt
        {
            get => isEncrypt;
            set
            {
                isEncrypt = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        private string filePath = "";
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        private string encryptKey = "";
        public string EncryptKey
        {
            get => encryptKey;
            set
            {
                encryptKey = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        private double progressValue;
        public double ProgressValue
        {
            get => progressValue;
            set
            {
                progressValue = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        private double progBarMaxVal = 100;
        public double ProgBarMaxVal
        {
            get => progBarMaxVal;
            set
            {
                progBarMaxVal = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        private bool filePathIsEnable = true;
        public bool FilePathIsEnable
        {
            get => filePathIsEnable;
            set
            {
                filePathIsEnable = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        private bool keyEncDecIsEnable = true;
        public bool KeyEncDecIsEnable
        {
            get => keyEncDecIsEnable;
            set
            {
                keyEncDecIsEnable = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        private bool chunkSizeEnable = true;
        public bool ChunkSizeEnable
        {
            get => chunkSizeEnable;
            set
            {
                chunkSizeEnable = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        private string blockSize = "4096";
        public string BlockSize
        {
            get => blockSize;
            set
            {
                blockSize = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        private string speedToolTip = "0 Kb/s";
        public string SpeedToolTip
        {
            get => speedToolTip;
            set
            {
                speedToolTip = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        OpenFileDialog OpenFile;

        Timer timer;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            int num = 0;

            timer = new Timer(, num, 0, 1000);

            OpenFile = new OpenFileDialog();
            OpenFile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

            OpenFile.RestoreDirectory = true;
        }

        //--------------------------------------------------------------------



        //--------------------------------------------------------------------
        private ICommand fileSelect;
        public ICommand FileSelect
        {
            get
            {
                if (fileSelect is null)
                {
                    fileSelect = new RelayCommand(
                        (param) =>
                        {
                            if (OpenFile.ShowDialog() == true)
                            {
                                FilePath = OpenFile.FileName;
                            }
                        });
                }

                return fileSelect;
            }
        }

        //--------------------------------------------------------------------

        string fileText;
        Task task;

        private ICommand startCom;
        public ICommand StartCom
        {
            get
            {
                if (startCom is null)
                {
                    startCom = new RelayCommand(
                        (param) =>
                        {
                            FilePathIsEnable = false;
                            KeyEncDecIsEnable = false;
                            ChunkSizeEnable = false;

                            interrupt = false;

                            task = new Task(() =>
                            {
                                EcryptDecryptFile();
                            });

                            task.Start();
                        },
                        (param) =>
                        {
                            if (EncryptKey.Length < 6 || FilePath == "")
                            {
                                return false;
                            }

                            if (task != null && !task.IsCompleted)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return startCom;
            }
        }

        //--------------------------------------------------------------------

        private ICommand cancelCom;
        public ICommand CancelCom
        {
            get
            {
                if (cancelCom is null)
                {
                    cancelCom = new RelayCommand(
                        (param) =>
                        {
                            if (task == null)
                                return;

                            lock (pause)
                            {
                                var result = MessageBox.Show("Do u want to cancel ecrypt?", "Alert", MessageBoxButton.YesNo);

                                if (result == MessageBoxResult.Yes)
                                {
                                    interrupt = true;
                                    //this.task = null;
                                }
                            }
                        },
                        (param) =>
                        {
                            if (task == null)
                                return false;

                            if (!task.IsCompleted)
                                return true;
                            else
                                return false;
                        });
                }

                return cancelCom;
            }
        }

        //--------------------------------------------------------------------

        int point = 0;

        Queue<byte[]> chunkList = new Queue<byte[]>();

        void EcryptDecryptFile()
        {
            var att = File.GetAttributes(FilePath);
            
            if ((att == FileAttributes.Archive && IsEncrypt == true) || (att == FileAttributes.Normal && IsEncrypt == false))
            {
                using (FileStream fstream = File.OpenRead(FilePath))
                {
                    var bytes = Encoding.UTF8.GetBytes(EncryptKey);
                    var fileLength = fstream.Length;
                    var parts = (int)Math.Ceiling(fstream.Length * 1.0 / Convert.ToDouble(BlockSize));
                    var toRead = (int)Math.Min(fstream.Length - fstream.Position, Convert.ToDouble(BlockSize));
                    while (toRead > 0)
                    {
                        var chunk = new byte[toRead];
                        fstream.Read(chunk, 0, toRead);
                        chunkList.Enqueue(chunk);
                        toRead = (int)Math.Min(fstream.Length - fstream.Position, toRead);
                    }

                    ProgBarMaxVal = fileLength;

                    foreach (var item in chunkList)
                    {
                        var array = new byte[item.Length];

                        for (int i = 0; i < array.Length; i++)
                        {
                            array[i] = (byte)(array[i] ^ bytes[i % bytes.Length]);

                            lock (pause)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ProgressValue += 1;
                                });
                            }

                            point++;
                            Thread.Sleep(7);

                            if (interrupt)
                            {
                                for (int j = point; j >= 0; j--)
                                {
                                    array[j] = (byte)(array[j] ^ bytes[j % bytes.Length]);

                                    lock (pause)
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            ProgressValue -= 1;
                                        });
                                    }

                                    Thread.Sleep(7);
                                }

                                FilePathIsEnable = true;
                                KeyEncDecIsEnable = true;
                                ChunkSizeEnable = true;

                                DefaultState();

                                return;
                            }

                        }

                        //stopwatch.Stop();

                        fileText += Encoding.Default.GetString(array);
                    }

                    fstream.Seek(0, SeekOrigin.Begin);
                }

                // File.WriteAllText(FilePath, String.Empty);

                using (FileStream fstream = new FileStream(FilePath, FileMode.OpenOrCreate))
                {
                    byte[] array = System.Text.Encoding.Default.GetBytes(fileText);

                    fstream.Write(array, 0, array.Length);
                }


                if (IsEncrypt)
                {
                    att = RemoveAttribute(att, FileAttributes.Archive);
                    File.SetAttributes(FilePath, att);

                    File.SetAttributes(FilePath, FileAttributes.Normal);
                    MessageBox.Show("File is encrypted");
                }
                else
                {
                    att = RemoveAttribute(att, FileAttributes.Normal);
                    File.SetAttributes(FilePath, att);

                    File.SetAttributes(FilePath, FileAttributes.Archive);
                    MessageBox.Show("File is decrypted");
                }

                DefaultState();

            }
            else
            if (att == FileAttributes.Normal && IsEncrypt == true)
            {
                MessageBox.Show("File already encrypted");
            }
            else
            if (att == FileAttributes.Archive && IsEncrypt == false)
            {
                MessageBox.Show("File is not encrypted");
            }

            FilePathIsEnable = true;
            KeyEncDecIsEnable = true;
            ChunkSizeEnable = true;
        }

        //--------------------------------------------------------------------

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        //--------------------------------------------------------------------

        void DefaultState()
        {
            EncryptKey = "";
            FilePath = "";
            BlockSize = "4096";
            SpeedToolTip = "0 Kb/s";
            point = 0;

            Dispatcher.Invoke(() =>
            {
                ProgressValue = 0;
            });
        }

        //--------------------------------------------------------------------

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        //--------------------------------------------------------------------

    }
}
