using ReversibleSignatureAnalyzer.Controller;
using ReversibleSignatureAnalyzer.Model;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

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
                watermarkedImage = addSignatureController.GetWatermarkedImage(importedImage, "Ala ma kota a kot ma ale gsdgos dds gojsdf gdsf gsdfhg dsg sdf gsdhgdg gdh jdj hd jhd jgfg esrgesrg sgh srtg ers gseh sf hsgsdg gdfsg hsg dfg sdfg sdf gdsf ghsgh esghsh sgh sdgh sdfh ersg erghrshdrth rthertge a gesr ge aa rgseg sdgeagt shgs hrthtwae rhrts erthres rhrts taerjrtsya eshrystyerdt yhjtdrsyeatw eyshyjhjytrsyeat dghjdytrsyehjhmkit76ryrhtnhm nbvxghtdyr brthnyytrhgb ghdrt yrhg nyrtshjfyu srm,ytr jmghtdr dfgrd", selectedAlgorithm);
                ImgExport.Source = watermarkedImage;
                BtnExportFile.Visibility = Visibility.Visible;
                tv_export_file_path.Visibility = Visibility.Visible;
            }
        }
    }
}
