using ReversibleSignatureAnalyzer.Controller;
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

        private AddSignatureController addSignatureController;
        private IReversibleWatermarkingAlgorithm selectedAlgorithm;
        private bool isFileLoaded = false;
        private BitmapImage importedImage;
        private BitmapImage watermarkedImage;

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
            addSignatureController = new AddSignatureController();
            selectedAlgorithm = new DifferencesExpansionAlgorithm(20, 1, Direction.Horizontal);
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
                watermarkedImage = addSignatureController.GetWatermarkedImage(importedImage, "Ala ma kota", selectedAlgorithm);
                ImgExport.Source = watermarkedImage;
                BtnExportFile.Visibility = Visibility.Visible;
                tv_export_file_path.Visibility = Visibility.Visible;
            }
        }
    }
}
