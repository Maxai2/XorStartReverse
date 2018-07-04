﻿using Microsoft.Win32;
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
        private bool finish = false;


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

        private string speedTool;
        public string SpeedTool
        {
            get => speedTool;
            set
            {
                speedTool = value;
                OnPropertyChanged();
            }
        }

        //--------------------------------------------------------------------

        OpenFileDialog OpenFile;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            OpenFile = new OpenFileDialog();
            OpenFile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

            OpenFile.RestoreDirectory = true;
        }

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

        Thread task;

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

                            this.interrupt = false;

                            task = new Thread(() =>
                            {
                                EcryptDecryptFile(IsEncrypt);
                            });

                            task.Start();

                        },
                        (param) =>
                        {
                            if (EncryptKey.Length < 6 || FilePath == "")
                            {
                                return false;
                            }

                            if (task != null && task.IsAlive)
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
                                    // task.Interrupt();
                                    this.interrupt = true;
                                    this.task = null;
                                }
                            }
                            DefaultState();

                        },
                        (param) =>
                        {
                            if (task == null)
                                return false;

                            if (task.IsAlive)
                                return true;
                            else
                                return false;
                        });
                }

                return cancelCom;
            }
        }

        //--------------------------------------------------------------------

        Stopwatch stopwatch = new Stopwatch();

        void EcryptDecryptFile(bool mode)
        {
            var att = File.GetAttributes(FilePath);

            if ((att == FileAttributes.Archive && IsEncrypt == true) || (att == FileAttributes.Normal && IsEncrypt == false))
            {
                using (FileStream fstream = File.OpenRead(FilePath))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(EncryptKey);

                    byte[] array = new byte[fstream.Length];

                    fstream.Read(array, 0, array.Length);

                    ProgBarMaxVal = array.Length;

                    stopwatch.Start();

                    for (int i = 0; i < array.Length; i++)
                    {
                        if (interrupt)
                        {
                            FilePathIsEnable = true;
                            KeyEncDecIsEnable = true;
                            ChunkSizeEnable = true;

                            DefaultState();

                            return;
                        }
                        lock (pause)
                        {
                            array[i] = (byte)(array[i] ^ bytes[i % bytes.Length]);

                            Dispatcher.Invoke(() =>
                            {
                                ProgressValue += 1;
                            });

                            SpeedTool = $"{stopwatch.ElapsedTicks / 1000} Kb/s";
                        }

                        Thread.Sleep(5);
                    }

                    stopwatch.Stop();

                    fileText = Encoding.Default.GetString(array);

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
            SpeedTool = "0 Kb/s";

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