using ReversibleSignatureAnalyzer.Controller;
using ReversibleSignatureAnalyzer.Model;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.CompilerServices;
using ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd;
using ReversibleSignatureAnalyzer.Model.Algorithm.HistogramShifting;

namespace ReversibleSignatureAnalyzer.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string BAD_FILE_NAME = "BAD FILE NAME";
        private string SUCCESS = "SUCCESS";

        private string path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
        private string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        private AddSignatureController addSignatureController;
        private IReversibleWatermarkingAlgorithm selectedAlgorithm;
        private bool isFileLoaded = true;
        private BitmapImage importedImage;
        private BitmapImage resultImage;
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
            TvExportFileName.Visibility = Visibility.Collapsed;
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
            ImgExport.Source = null;
            SetTextRichTextBox(TvPayload, "Place for payload");
            TvExportFileName.Text = "";
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            var secretPayload = GetTextFromRichTextBox(TvPayload);
            GetSelectedAlgorithm();
            if (isFileLoaded && CbActivityType.Text == TbAdd.Content.ToString())
            {
                resultImage = addSignatureController.GetWatermarkedImage(importedImage, secretPayload, selectedAlgorithm);
            }

            if (isFileLoaded && CbActivityType.Text == TbAnalyze.Content.ToString())
            {
                resultImage = addSignatureController.GetDecodedImage(importedImage, selectedAlgorithm).Item1;
                SetTextRichTextBox(TvPayload ,addSignatureController.GetDecodedImage(importedImage, selectedAlgorithm).Item2);
            }

            ImgExport.Source = resultImage;
            Console.WriteLine(resultImage.UriSource);
            BtnExportFile.Visibility = Visibility.Visible;
            TvExportFileName.Visibility = Visibility.Visible;
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
                selectedAlgorithm = new DwtDctSvdAlgorithm();
            }
            else if (RbAlgorithm3.IsChecked.Value)
            {
                selectedAlgorithm = new HistogramShiftingAlgorithm();
            }
        }

        private void BtnExportFile_Click(object sender, RoutedEventArgs e)
        {
            string fileName = TvExportFileName.Text;

            if (CheckFileName(fileName))
            {
                string filePath = $"{projectDirectory}/Model/_img/{fileName}.png";
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(resultImage));
                    encoder.Save(fileStream);
                    OperationSuccessNotify("");
                }
            }
            else
            {
                OperationErrorNotify(BAD_FILE_NAME);
            }
        }

        private void OperationSuccessNotify(string msg)
        {
            TvOperationResult.Text = $"{SUCCESS} {msg}";
            TvOperationResult.Foreground = Brushes.ForestGreen;
        }

        private void OperationErrorNotify(string msg)
        {
            TvOperationResult.Text = msg;
            TvOperationResult.Foreground = Brushes.Red;
        }

        private bool CheckFileName(string fileName)
        {

            if (fileName.Contains(" "))
            {
                return false;
            }
            if (fileName.Replace(" ", "") == "")
            {
                return false;
            }
            if (fileName.Contains('.'))
            {
                return false;
            }
            return true;
        }

    }
}
