using ReversibleSignatureAnalyzer.Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ConfigurationDialogBox
{
    /// <summary>
    /// Interaction logic for HSConfigDialogBox.xaml
    /// </summary>
    public partial class HistogramShiftingConfiguraitonDialogBox : Window
    {
        public HistogramShiftingConfiguraitonDialogBox(HashSet<EmbeddingChanel> embeddingChanels)
        {
            InitializeComponent();
            cbR.IsChecked = embeddingChanels.Contains(EmbeddingChanel.R);
            cbG.IsChecked = embeddingChanels.Contains(EmbeddingChanel.G);
            cbB.IsChecked = embeddingChanels.Contains(EmbeddingChanel.B);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
