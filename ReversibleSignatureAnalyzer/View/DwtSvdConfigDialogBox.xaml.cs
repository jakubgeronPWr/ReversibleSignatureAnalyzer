using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd;
using ReversibleSignatureAnalyzer.Model;

namespace ReversibleSignatureAnalyzer.View
{
    /// <summary>
    /// Interaction logic for DwtSvdConfigDialogBox.xaml
    /// </summary>
    public partial class DwtSvdConfigDialogBox : Window
    {
        public bool isFileLoaded = false;
        public string fileName = "";

        public DwtSvdConfigDialogBox(HashSet<EmbeddingChanel> embeddingChanmels, HashSet<DwtDctSvdAlgorithm.QuarterSymbol> quarter)
        {
            InitializeComponent();
            cbR.IsChecked = embeddingChanmels.Contains(EmbeddingChanel.R);
            cbG.IsChecked = embeddingChanmels.Contains(EmbeddingChanel.G);
            cbB.IsChecked = embeddingChanmels.Contains(EmbeddingChanel.B);
            qHH.IsChecked = quarter.Contains(DwtDctSvdAlgorithm.QuarterSymbol.HH);
            qHL.IsChecked = quarter.Contains(DwtDctSvdAlgorithm.QuarterSymbol.HL);
            qLH.IsChecked = quarter.Contains(DwtDctSvdAlgorithm.QuarterSymbol.LH);
            qLL.IsChecked = quarter.Contains(DwtDctSvdAlgorithm.QuarterSymbol.LL);
        }



        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void getOrigin_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter =
                "Txt Filses (*.txt)|*.txt";
            Nullable<bool> result = dlg.ShowDialog();

            if (result.HasValue && result.Value)
            {
                isFileLoaded = true;
                fileName = dlg.FileName;
            }
        }
    }
}
