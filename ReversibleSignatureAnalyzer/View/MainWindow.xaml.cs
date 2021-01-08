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
using ReversibleSignatureAnalyzer.Controller.Algorithm.DifferenceExpansion;
using ReversibleSignatureAnalyzer.Controller.Algorithm;
using System.Collections.Generic;
using ReversibleSignatureAnalyzer.Controller.Algorithm.HistogramShifting;

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
        private bool isFileLoaded = true;
        private BitmapImage importedImage;
        private BitmapImage resultImage;
        private string activityType;
        private AlgorithmConfiguration currentDeConfiguration = new DifferencesExpansionConfiguration(1, 20, Direction.Horizontal, new HashSet<EmbeddingChanel>(){ EmbeddingChanel.R });
        private IReversibleWatermarkingAlgorithm deAlgorithm = new DifferencesExpansionAlgorithm();
        private IReversibleWatermarkingAlgorithm dwtDctSvdAlgotithm = new DwtDctSvdAlgorithm();
        private IReversibleWatermarkingAlgorithm hsAlgorithm = new HistogramShiftingAlgorithm();


        public MainWindow()
        {
            InitializeComponent();
            HideStartUp();
            SetStartup();
        }

        private void HideStartUp()
        {
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
            IReversibleWatermarkingAlgorithm selectedAlgorithm = GetSelectedAlgorithm();
            AlgorithmConfiguration configuration = GetConfigurationForSelectedAlgorithm();
            if (isFileLoaded && CbActivityType.Text == TbAdd.Content.ToString())
            {
                resultImage = addSignatureController.GetWatermarkedImage(importedImage, secretPayload, selectedAlgorithm, configuration);
            }

            if (isFileLoaded && CbActivityType.Text == TbAnalyze.Content.ToString())
            {
                Tuple<BitmapImage, string> imageAndPayload = addSignatureController.GetDecodedImage(importedImage, selectedAlgorithm, configuration);
                resultImage = imageAndPayload.Item1;
                SetTextRichTextBox(TvPayload, imageAndPayload.Item2);
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

        private IReversibleWatermarkingAlgorithm GetSelectedAlgorithm()
        {
            if (RbAlgorithm1.IsChecked.Value)
            {
                return deAlgorithm;
            }
            else if (RbAlgorithm2.IsChecked.Value)
            {
                return dwtDctSvdAlgotithm;
            }
            else if (RbAlgorithm3.IsChecked.Value)
            {
                return hsAlgorithm;
            }
            throw new Exception("No algorithm selected");
        }

        private AlgorithmConfiguration GetConfigurationForSelectedAlgorithm()
        {
            if (RbAlgorithm1.IsChecked.Value)
            {
                return currentDeConfiguration;
            }
            else if (RbAlgorithm2.IsChecked.Value)
            {
                return currentDeConfiguration;
            }
            else if (RbAlgorithm3.IsChecked.Value)
            {
                return currentDeConfiguration;
            }
            throw new Exception("No algorithm selected");
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

        private void BtnConfigDE_Click(object sender, RoutedEventArgs e)
        {
            DifferencesExpansionConfiguration config = (DifferencesExpansionConfiguration) currentDeConfiguration;
            var dialogBox = new ConfigurationDialogBox.DifferencesExpansionConfiguraitonDialogBox(
                config.Iterations,
                config.Threeshold,
                config.EmbeddingDirection,
                config.EmbeddingChanels)
            {
                Owner = this,
            };
            dialogBox.ShowDialog();
            if (dialogBox.DialogResult == true)
            {
                Direction direction;
                Enum.TryParse(dialogBox.cbEmbeddingDirection.Text, out direction);
                HashSet<EmbeddingChanel> embeddingChanels = new HashSet<EmbeddingChanel>();
                if (dialogBox.cbR.IsChecked == true)
                {
                    embeddingChanels.Add(EmbeddingChanel.R);
                }
                if (dialogBox.cbG.IsChecked == true)
                {
                    embeddingChanels.Add(EmbeddingChanel.G);
                }
                if (dialogBox.cbB.IsChecked == true)
                {
                    embeddingChanels.Add(EmbeddingChanel.B);
                }
                currentDeConfiguration = new DifferencesExpansionConfiguration(dialogBox.IterationsNumber, dialogBox.Threshold, direction, embeddingChanels);
            }
        }

        private void BtnConfigSVD_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnConfigHS_Click(object sender, RoutedEventArgs e)
        {
            HashSet<EmbeddingChanel> set = new HashSet<EmbeddingChanel>();
            set.Add(EmbeddingChanel.R);
            HistogramShiftingConfiguration config = new HistogramShiftingConfiguration(1, set);
            var dialogBox = new ConfigurationDialogBox.HistogramShiftingConfiguraitonDialogBox(
                config.EmbeddingChanels)
            {
                Owner = this,
            };
            dialogBox.ShowDialog();
            if (dialogBox.DialogResult == true)
            {
                if (dialogBox.cbR.IsChecked == true)
                {
                    HashSet<EmbeddingChanel> embeddingChanels = new HashSet<EmbeddingChanel>();
                    if (dialogBox.cbR.IsChecked == true)
                    {
                        embeddingChanels.Add(EmbeddingChanel.R);
                    }
                    if (dialogBox.cbG.IsChecked == true)
                    {
                        embeddingChanels.Add(EmbeddingChanel.G);
                    }
                    if (dialogBox.cbB.IsChecked == true)
                    {
                        embeddingChanels.Add(EmbeddingChanel.B);
                    }
                    currentDeConfiguration = new HistogramShiftingConfiguration(1, embeddingChanels);
                }
            }
        }

    }
}
