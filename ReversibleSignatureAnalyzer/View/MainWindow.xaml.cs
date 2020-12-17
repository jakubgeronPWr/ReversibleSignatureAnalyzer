﻿using ReversibleSignatureAnalyzer.Controller;
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
        private AlgorithmConfiguration currentEncodingDeConfiguration = new DifferencesExpansionConfiguration(20, Direction.Horizontal, new HashSet<EmbeddingChanel>(){ EmbeddingChanel.R });
        private AlgorithmConfiguration currentDecodingDeConfiguration = new DifferencesExpansionConfiguration(20, Direction.Horizontal, new HashSet<EmbeddingChanel>() { EmbeddingChanel.R });
        private AlgorithmConfiguration currentEncodingHsConfiguration = new HistogramShiftingConfiguration(false, new HashSet<EmbeddingChanel>() { EmbeddingChanel.R });
        private AlgorithmConfiguration currentDecodingHsConfiguration = new HistogramShiftingConfiguration(true, new HashSet<EmbeddingChanel>() { EmbeddingChanel.R });
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
                return currentEncodingDeConfiguration;
            }
            else if (RbAlgorithm2.IsChecked.Value)
            {
                return null;
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
                return currentDecodingDeConfiguration;
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
                DifferencesExpansionConfiguration config = (DifferencesExpansionConfiguration) currentEncodingDeConfiguration;
                var dialogBox = new ConfigurationDialogBox.DifferencesExpansionConfiguraitonDialogBox(
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
                    currentEncodingDeConfiguration = new DifferencesExpansionConfiguration(dialogBox.Threshold, direction, embeddingChanels);
                }
            }
            if(isAnalyzingModeSelected())
            {
                DifferencesExpansionConfiguration config = (DifferencesExpansionConfiguration)currentDecodingDeConfiguration;
                var dialogBox = new ConfigurationDialogBox.DifferencesExpansionConfiguraitonDialogBox(
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
                    currentDecodingDeConfiguration = new DifferencesExpansionConfiguration(dialogBox.Threshold, direction, embeddingChanels);
                }
            }
        }

        private void BtnConfigSVD_Click(object sender, RoutedEventArgs e)
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
