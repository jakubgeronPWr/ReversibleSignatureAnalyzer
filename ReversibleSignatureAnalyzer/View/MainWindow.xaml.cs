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
using ReversibleSignatureAnalyzer.Controller.Algorithm.GlobalDecoding;
using ConfigurationDialogBox;

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
        private AlgorithmConfiguration activeEncodingDEConfig;
        private AlgorithmConfiguration activeDecodingDEConfig;
        private AlgorithmConfiguration standardEncodingDEConfig = new DifferencesExpansionConfiguration(20, Direction.Horizontal, new HashSet<EmbeddingChanel>(){ EmbeddingChanel.R });
        private AlgorithmConfiguration standardDecodingDEConfig = new DifferencesExpansionConfiguration(20, Direction.Horizontal, new HashSet<EmbeddingChanel>() { EmbeddingChanel.R });
        private AlgorithmConfiguration bruteForceDecodingDEConfig = new DifferenceExpansionBruteForceConfiguration(new HashSet<EmbeddingChanel>() { EmbeddingChanel.R }, new HashSet<Direction>() { Direction.Horizontal, Direction.Vertical });
        private AlgorithmConfiguration currentEncodingHsConfiguration = new HistogramShiftingConfiguration(false, new HashSet<EmbeddingChanel>() { EmbeddingChanel.R });
        private AlgorithmConfiguration currentDecodingHsConfiguration = new HistogramShiftingConfiguration(true, new HashSet<EmbeddingChanel>() { EmbeddingChanel.R });
        private string activityType;
        private AlgorithmConfiguration currentDeConfiguration = new DifferencesExpansionConfiguration(1, 20, Direction.Horizontal, new HashSet<EmbeddingChanel>(){ EmbeddingChanel.R });
        private AlgorithmConfiguration currentDwtSvdConfiguration = new DwtSvdConfiguration(1,  new HashSet<EmbeddingChanel>(){ EmbeddingChanel.R }, new HashSet<DwtDctSvdAlgorithm.QuarterSymbol>() { DwtDctSvdAlgorithm.QuarterSymbol.HH });
        private IReversibleWatermarkingAlgorithm deAlgorithm = new DifferencesExpansionAlgorithm();
        private IReversibleWatermarkingAlgorithm dwtDctSvdAlgotithm = new DwtDctSvdAlgorithm();
        private IReversibleWatermarkingAlgorithm hsAlgorithm = new HistogramShiftingAlgorithm();
        private IReversibleWatermarkingAlgorithm globalAlgorithm = new GlobalDecodingAlgorithm();


        public MainWindow()
        {
            InitializeComponent();
            SetStartup();
        }

        private void SetStartup()
        {
            RbAlgorithm1.IsChecked = true;
            CbActivityType.SelectedIndex = 0;
            addSignatureController = new AddSignatureController();
            ImportImage(projectDirectory + "/Model/_img/lena.png");
            activeEncodingDEConfig = standardEncodingDEConfig;
            activeDecodingDEConfig = standardDecodingDEConfig;
        }

        private void BtnImportFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG Filses (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif|JPEG Files (*.jpeg)|*.jpeg";
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
            SetTextRichTextBox(TvPayload, "Type in your payload");
            TvExportFileName.Text = "";
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            var secretPayload = GetTextFromRichTextBox(TvPayload);
            IReversibleWatermarkingAlgorithm selectedAlgorithm = GetSelectedAlgorithm();

            if (isFileLoaded && isWatermarkingModeSelected())
            {
                resultImage = addSignatureController.GetWatermarkedImage(importedImage, secretPayload, selectedAlgorithm, GetEncodingConfigurationForSelectedAlgorithm());
            }

            if (isFileLoaded && isAnalyzingModeSelected())
            {
                Tuple<BitmapImage, string> imageAndPayload = addSignatureController.GetDecodedImage(importedImage, selectedAlgorithm, GetDecodingConfigurationForSelectedAlgorithm());
                resultImage = imageAndPayload.Item1;
                SetTextRichTextBox(TvPayload, imageAndPayload.Item2);
                TvPayload.Visibility = Visibility.Visible;
            }

            ImgExport.Source = resultImage;
            Console.WriteLine(resultImage.UriSource);
            BtnExportFile.Visibility = Visibility.Visible;
            TvExportFileName.Visibility = Visibility.Visible;
        }

        

        private bool isWatermarkingModeSelected()
        {
            return CbActivityType.Text == TbAdd.Content.ToString();
        }

        private bool isAnalyzingModeSelected()
        {
            return CbActivityType.Text == TbAnalyze.Content.ToString();
        }

        private string GetTextFromRichTextBox(RichTextBox rtb)
        {
            TextRange tr = new TextRange(rtb.Document.ContentStart,rtb.Document.ContentEnd);
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
            else if (RbAlgorithm4.IsChecked.Value)
            {
                return globalAlgorithm;
            }
            throw new Exception("No algorithm selected");
        }

        private AlgorithmConfiguration GetEncodingConfigurationForSelectedAlgorithm()
        {
            if (RbAlgorithm1.IsChecked.Value)
            {
                return activeEncodingDEConfig;
            }
            else if (RbAlgorithm2.IsChecked.Value)
            {
                return currentDwtSvdConfiguration;
            }
            else if (RbAlgorithm3.IsChecked.Value)
            {
                return currentEncodingHsConfiguration;
            }
            throw new Exception("No algorithm selected");
        }

        private AlgorithmConfiguration GetDecodingConfigurationForSelectedAlgorithm()
        {
            if (RbAlgorithm1.IsChecked.Value)
            {
                return activeDecodingDEConfig;
            }
            else if (RbAlgorithm2.IsChecked.Value)
            {
                return null;
            }
            else if (RbAlgorithm3.IsChecked.Value)
            {
                return currentDecodingHsConfiguration;
            }
            else if (RbAlgorithm4.IsChecked.Value)
            {
                return null;
            }
            throw new Exception("No algorithm selected");
        }

        private void BtnExportFile_Click(object sender, RoutedEventArgs e)
        {
            string fileName = TvExportFileName.Text;

            if (CheckFileName(fileName))
            {
                string filePath = $"{projectDirectory}/Model/_img/{fileName}.png";
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(resultImage));
                        encoder.Save(fileStream);
                        OperationSuccessNotify("");
                    }
                }
                catch (Exception exception)
                {
                    OperationErrorNotify(BAD_FILE_NAME);
                }
            }
            else
            {
                OperationErrorNotify(BAD_FILE_NAME);
            }
        }

        private void OperationSuccessNotify(string msg)
        {
            TvOperationResult.Visibility = Visibility.Visible;
            TvOperationResult.Text = $"{SUCCESS} {msg}";
            TvOperationResult.Foreground = Brushes.ForestGreen;
        }

        private void OperationErrorNotify(string msg)
        {
            TvOperationResult.Visibility = Visibility.Visible;
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
            if (isWatermarkingModeSelected())
            {
                DifferencesExpansionConfiguration config = (DifferencesExpansionConfiguration) standardEncodingDEConfig;
                var dialogBox = new ConfigurationDialogBox.DifferencesExpansionConfiguraitonDialogBox(config, null, activeEncodingDEConfig)
                {
                    Owner = this,
                };
                dialogBox.ShowDialog();
                AlgorithmConfiguration newConfig = GetConfigurationBasedOnDialogBoxResult(dialogBox);
                if (newConfig != null)
                {
                    standardEncodingDEConfig = newConfig;
                    activeEncodingDEConfig = standardEncodingDEConfig;
                }
            }
            if(isAnalyzingModeSelected())
            {
                DifferencesExpansionConfiguration deConfig = (DifferencesExpansionConfiguration)standardDecodingDEConfig;
                DifferenceExpansionBruteForceConfiguration deBruteForceConfig = (DifferenceExpansionBruteForceConfiguration)bruteForceDecodingDEConfig;
                var dialogBox = new ConfigurationDialogBox.DifferencesExpansionConfiguraitonDialogBox(deConfig, deBruteForceConfig, activeDecodingDEConfig)
                {
                    Owner = this,
                };
                dialogBox.ShowDialog();
                AlgorithmConfiguration newConfig = GetConfigurationBasedOnDialogBoxResult(dialogBox);
                if (dialogBox.cbConfigurationType.Text == "Standard" && newConfig != null)
                {
                    standardDecodingDEConfig = newConfig;
                    activeDecodingDEConfig = standardDecodingDEConfig;
                }
                else if(dialogBox.cbConfigurationType.Text == "Brute force" && newConfig != null)
                {
                    bruteForceDecodingDEConfig = newConfig;
                    activeDecodingDEConfig = bruteForceDecodingDEConfig;
                }
            }
        }

        private AlgorithmConfiguration GetConfigurationBasedOnDialogBoxResult(DifferencesExpansionConfiguraitonDialogBox dialogBox)
        {
            if (dialogBox.DialogResult == true && dialogBox.cbConfigurationType.Text == "Standard")
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
                return new DifferencesExpansionConfiguration(dialogBox.Threshold, direction, embeddingChanels);
            }
            if (dialogBox.DialogResult == true && dialogBox.cbConfigurationType.Text == "Brute force")
            {
                HashSet<Direction> directions = new HashSet<Direction>();
                if (dialogBox.cbHorizontal_BF.IsChecked == true)
                {
                    directions.Add(Direction.Horizontal);
                }
                if (dialogBox.cbVertical_BF.IsChecked == true)
                {
                    directions.Add(Direction.Vertical);
                }
                HashSet<EmbeddingChanel> embeddingChanels = new HashSet<EmbeddingChanel>();
                if (dialogBox.cbR_BF.IsChecked == true)
                {
                    embeddingChanels.Add(EmbeddingChanel.R);
                }
                if (dialogBox.cbG_BF.IsChecked == true)
                {
                    embeddingChanels.Add(EmbeddingChanel.G);
                }
                if (dialogBox.cbB_BF.IsChecked == true)
                {
                    embeddingChanels.Add(EmbeddingChanel.B);
                }
                return new DifferenceExpansionBruteForceConfiguration(embeddingChanels, directions);
            }
            return null;
        }

        private void BtnConfigSVD_Click(object sender, RoutedEventArgs e)
        {
            DwtSvdConfiguration config = currentDwtSvdConfiguration as DwtSvdConfiguration;
            var dialogBox = new DwtSvdConfigDialogBox(config.EmbeddingChanels, config.QuarterSymbol)
            {
                Owner = this,
            };
            dialogBox.ShowDialog();
            if (dialogBox.DialogResult == true)
            {
                HashSet<EmbeddingChanel> embeddingChanel = new HashSet<EmbeddingChanel>();
                if (dialogBox.cbR.IsChecked == true)
                {
                    embeddingChanel.Add(EmbeddingChanel.R);
                }
                if (dialogBox.cbG.IsChecked == true)
                {
                    embeddingChanel.Add(EmbeddingChanel.G);
                }
                if (dialogBox.cbB.IsChecked == true)
                {
                    embeddingChanel.Add(EmbeddingChanel.B);
                }
                HashSet<DwtDctSvdAlgorithm.QuarterSymbol> quarter = new HashSet<DwtDctSvdAlgorithm.QuarterSymbol>();
                if (dialogBox.qHH.IsChecked == true)
                {
                    quarter.Add(DwtDctSvdAlgorithm.QuarterSymbol.HH);
                }
                if (dialogBox.qHL.IsChecked == true)
                {
                    quarter.Add(DwtDctSvdAlgorithm.QuarterSymbol.HL);
                }
                if (dialogBox.qLH.IsChecked == true)
                {
                    quarter.Add(DwtDctSvdAlgorithm.QuarterSymbol.LH);
                }
                if (dialogBox.qLL.IsChecked == true)
                {
                    quarter.Add(DwtDctSvdAlgorithm.QuarterSymbol.LL);
                }
                currentDwtSvdConfiguration = new DwtSvdConfiguration(1, embeddingChanel, quarter);

                if (dialogBox.isFileLoaded)
                {
                    ImportOriginValues(dialogBox.fileName);
                }
            }
        }

        private void ImportOriginValues(string fileName)
        {

        }

        private void BtnConfigHS_Click(object sender, RoutedEventArgs e)
        {
            if(isWatermarkingModeSelected())
            {
                HashSet<EmbeddingChanel> set = new HashSet<EmbeddingChanel>();
                set.Add(EmbeddingChanel.R);
                HistogramShiftingConfiguration config = new HistogramShiftingConfiguration(false, set);
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
                        currentEncodingHsConfiguration = new HistogramShiftingConfiguration(false, embeddingChanels);
                    }
                }
            }
            if(isAnalyzingModeSelected())
            {
                HashSet<EmbeddingChanel> set = new HashSet<EmbeddingChanel>();
                set.Add(EmbeddingChanel.R);
                HistogramShiftingConfiguration config = new HistogramShiftingConfiguration(false, set);
                var dialogBox = new ConfigurationDialogBox.HistogramShiftingConfiguraitonDecodingDialogBox(
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
                        currentDecodingHsConfiguration = new HistogramShiftingConfiguration(dialogBox.cbBruteforce.IsChecked == true, embeddingChanels);
                    }
                }
            }
        }

        private void TvPayload_TextChanged(object sender, TextChangedEventArgs e)
        {

        }


        private void CbActivityType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbActivityType.SelectedItem != null)
            {
                string selection = (e.AddedItems[0] as ComboBoxItem).Content as string;
                if (selection == TbAdd.Content.ToString())
                {
                    SetTextRichTextBox(TvPayload, "Type in your payload");
                    TvPayload.Visibility = Visibility.Visible;
                    RbAlgorithm4.Visibility = Visibility.Collapsed;
                }
                if (selection == TbAnalyze.Content.ToString())
                {
                    SetTextRichTextBox(TvPayload, "");
                    TvPayload.Visibility = Visibility.Collapsed;
                    RbAlgorithm4.Visibility = Visibility.Visible;
                }
                TvOperationResult.Visibility = Visibility.Collapsed;
                BtnExportFile.Visibility = Visibility.Collapsed;
                TvExportFileName.Visibility = Visibility.Collapsed;
                TvExportFileName.Text = "";
            }
        }
    }
}
