using ReversibleSignatureAnalyzer.Controller;
using ReversibleSignatureAnalyzer.Model;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.CompilerServices;
using ReversibleSignatureAnalyzer.Model.Algorithm.HistogramShifting;

namespace ReversibleSignatureAnalyzer.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
        string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        private AddSignatureController addSignatureController;
        private IReversibleWatermarkingAlgorithm selectedAlgorithm;
        private bool isFileLoaded = true;
        private BitmapImage importedImage;
        private BitmapImage watermarkedImage;
        private String activityType;

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
            RbAlgorithm1.IsChecked = true;
            ImportImage(projectDirectory + "/Model/_img/lena.png");
            activityType = CbActivityType.Text;
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
                ImportImage(fileName);
            }

        }

        private void ImportImage(string fileName)
        {
            TvImportFilePath.Text = fileName;
            importedImage = new BitmapImage(new Uri(fileName));
            ImgImport.Source = importedImage;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            var secretPayload = GetTextFromRichTextBox(TvPayload);
            GetSelectedAlgorithm();
            if (isFileLoaded && CbActivityType.Text == TbAdd.Content.ToString())
            {
                watermarkedImage = addSignatureController.GetWatermarkedImage(importedImage, "Ala ma kota a kota ma ale.", selectedAlgorithm);
                ImgExport.Source = watermarkedImage;
                Console.WriteLine(watermarkedImage.UriSource);
                BtnExportFile.Visibility = Visibility.Visible;
                tv_export_file_path.Visibility = Visibility.Visible;
            }

            if (isFileLoaded && CbActivityType.Text == TbAnalyze.Content.ToString())
            {
                watermarkedImage = addSignatureController.GetDecodedImage(importedImage, selectedAlgorithm).Item1;
                SetTextRichTextBox(TvPayload ,addSignatureController.GetDecodedImage(importedImage, selectedAlgorithm).Item2);
                ImgExport.Source = watermarkedImage;
                Console.WriteLine(watermarkedImage.UriSource);
                BtnExportFile.Visibility = Visibility.Visible;
                tv_export_file_path.Visibility = Visibility.Visible;
            }
        }

        private string GetTextFromRichTextBox(RichTextBox rtb)
        {
            TextRange tr = new TextRange(
                rtb.Document.ContentStart,
                rtb.Document.ContentEnd
                );
            return tr.Text;
        }

        private void SetTextRichTextBox(RichTextBox rtb, string text)
        {
            rtb.Document.Blocks.Clear();
            rtb.Document.Blocks.Add(new Paragraph(new Run(text)));
        }

        private void GetSelectedAlgorithm()
        {
            if (RbAlgorithm1.IsChecked.Value)
            {
                selectedAlgorithm = new DifferencesExpansionAlgorithm(20, 1, Direction.Horizontal);
            }
            else if (RbAlgorithm2.IsChecked.Value)
            {
                selectedAlgorithm = new DifferencesExpansionAlgorithm(20, 1, Direction.Horizontal);
            }
            else if (RbAlgorithm3.IsChecked.Value)
            {
                selectedAlgorithm = new HistogramShiftingAlgorithm();
            }
        }

    }
}
