using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using System.Windows.Forms;
using System.IO;
using MessageBox = System.Windows.Forms.MessageBox;
using FileDialog = System.Windows.Forms.FileDialog;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace FileThreads
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            worker = new BackgroundWorker();
            Thread = new Thread(Copy);
        }

        static public AutoResetEvent autoResetEvent = new AutoResetEvent(true);

        BackgroundWorker worker;
        public Thread Thread { get; set; }
        private delegate void delUpdateProgressBar();


        public static object Lock = new object();
        private void BrowseFrom(object sender, RoutedEventArgs e)
        {
            var rez = MessageBox.Show("U want copy file ", "Copy", MessageBoxButtons.YesNo);
            
            if(rez == System.Windows.Forms.DialogResult.Yes)
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    PathFrom.Text = fileDialog.FileName;
                }
            }
            else if(rez == System.Windows.Forms.DialogResult.No)
            {
                FolderBrowserDialog folderBrowser = new FolderBrowserDialog();

                if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    PathFrom.Text = folderBrowser.SelectedPath;
                }
            }
            
        }

        private void BrowseTo(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();

            if(folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PathTo.Text = folderBrowser.SelectedPath;
            }

        }

        [Obsolete]
        private void ResumeCopy(object sender, RoutedEventArgs e)
        {
            autoResetEvent.Set();
        }

        [Obsolete]
        private void PauseCopy(object sender, RoutedEventArgs e)
        {
            autoResetEvent.WaitOne();
        }

        [Obsolete]
        private void StopCopy(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();
        }


        private void StartCopy(object sender, RoutedEventArgs e)
        {
            //worker.DoWork += DelegateCopy;
            //worker.RunWorkerAsync();
            Thread.Start();
        }

        private int CountAllFiles(string path, int n )
        {
            

            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            var folders = directoryInfo.GetDirectories();

            n += directoryInfo.GetFiles().Length;

            for(int i = 0; i < folders.Length;i++)
            {
                CountAllFiles(folders[i].FullName, n);
            }
            return n;
        }


        private void CopyFolder(string pathFrom, string pathTo)
        {
            int percent = CountAllFiles(pathFrom, 0);

            DirectoryInfo directory = new DirectoryInfo(pathFrom);

            string newpath = pathTo + "\\" + directory.Name;
            Directory.CreateDirectory(newpath);

            var Files = directory.GetFiles();

            for (int i = 0; i < Files.Length; i++)
            {
                File.Copy(Files[i].FullName, newpath + "\\" + Files[i].Name);

                Dispatcher.Invoke(() => PrgBar.Value += 100/percent);
                Thread.Sleep(100);
            }

            var folders = directory.GetDirectories();

            for (int i = 0; i < folders.Length; i++)
            {
                CopyFolder(folders[i].FullName, newpath + "\\" + folders[i].Name);
            }

        }

        private void Copy()
        {
            bool isFile = false;

            string pathFrom = string.Empty, pathTo = string.Empty;
            Dispatcher.Invoke(() =>
            {
                pathFrom = PathFrom.Text;
                pathTo = PathTo.Text;
            });


            bool bIsFile = false;
            bool bIsDirectory = false;

            try
            {
                string[] subfolders = Directory.GetDirectories(pathFrom);

                bIsDirectory = true;
                bIsFile = false;
            }
            catch (System.IO.IOException)
            {
                bIsDirectory = false;
                bIsFile = true;
            }


            if(bIsFile)
            {
                FileInfo file = new FileInfo(pathFrom);
                if (!file.Exists)
                {
                    throw new FileNotFoundException("The file was not found.", pathFrom);
                }
                try
                {
                    File.Copy(pathFrom, pathTo + "\\" + file.Name);
                    _ = Dispatcher.Invoke(() => PrgBar.Value = 100);
                    

                    
                }
                catch (IOException exp)
                {

                }
                isFile = true;
                
            }
            if (!isFile)
            {
                CopyFolder(pathFrom,pathTo);
            }
            Thread.Sleep(100);
            Dispatcher.Invoke(() =>
            {
                PrgBar.Value = 0;
                PathFrom.Text = "";
                PathTo.Text = "";
            });

        }

        private void DelegateCopy(object sender, DoWorkEventArgs e)
        {
            Delegate del = new delUpdateProgressBar(Copy);

            Dispatcher.Invoke(del);


        }

    }
}
