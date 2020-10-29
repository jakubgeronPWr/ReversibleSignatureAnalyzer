using ReversibleSignatureAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
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

namespace ReversibleSignatureAnalyzer.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private bool isFileLoaded = false;
        private BitmapImage importedImage;

        public MainWindow()
        {
            InitializeComponent();
            HideStartUp();
            SetStartup();
        }

        private void HideStartUp()
        {
            //btn_import_file.Visibility = Visibility.Collapsed;
            //tv_import_file_path.Visibility = Visibility.Collapsed;

            BtnExportFile.Visibility = Visibility.Collapsed;
            tv_export_file_path.Visibility = Visibility.Collapsed;
        }

        private void SetStartup()
        {
            RbAlgorithm1.IsChecked = true;
            CbActivityType.SelectedIndex = 0;
        }

        private void BtnImportFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".png";
            dlg.Filter =
                "PNG Filses (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif|JPEG Files (*.jpeg)|*.jpeg";
            Nullable<bool> result = dlg.ShowDialog();

            if (result.HasValue && result.Value)
            {
                isFileLoaded = true;
                string fileName = dlg.FileName;
                TvImportFilePath.Text = fileName;
                importedImage = new BitmapImage(new Uri(fileName));
                ImgImport.Source = importedImage;

            }

        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (isFileLoaded)
            {
                DifferencesExpansionAlgorithm algo = new DifferencesExpansionAlgorithm(20, 1, Direction.Horizontal);
                Bitmap embedded = algo.Encode(BitmapImage2Bitmap(importedImage), "123");
                BitmapImage bitmapImage = new BitmapImage();
                using (MemoryStream memory = new MemoryStream())
                {
                    embedded.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                }
                ImgExport.Source = bitmapImage;
                BtnExportFile.Visibility = Visibility.Visible;
                tv_export_file_path.Visibility = Visibility.Visible;
            }
        }

        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }
    }
}
