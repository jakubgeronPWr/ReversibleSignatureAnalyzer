using ReversibleSignatureAnalyzer.Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace ConfigurationDialogBox
{
    public partial class DifferencesExpansionConfiguraitonDialogBox : Window
    {
        public DifferencesExpansionConfiguraitonDialogBox(int threshold, Direction embeddingDirection, HashSet<EmbeddingChanel> embeddingChanels)
        {
            InitializeComponent();
            this.threshold = threshold;
            tbThreshold.Text = threshold.ToString();
            cbEmbeddingDirection.SelectedIndex = (int) embeddingDirection;
            cbR.IsChecked = embeddingChanels.Contains(EmbeddingChanel.R);
            cbG.IsChecked = embeddingChanels.Contains(EmbeddingChanel.G);
            cbB.IsChecked = embeddingChanels.Contains(EmbeddingChanel.B);
        }

        public Thickness DocumentMargin
        {
            get { return (Thickness)DataContext; }
            set { DataContext = value; }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValid(this))
            {
                return;
            }
            DialogResult = true;
        }

        private bool IsValid(DependencyObject node)
        {
            if (node != null)
            {
                var isValid = !Validation.GetHasError(node);
                if (!isValid)
                {
                    if (node is IInputElement) 
                    { 
                        Keyboard.Focus((IInputElement)node); 
                    }
                    return false;
                }
            }
            return LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>().All(IsValid);
        }

        private int threshold = 1;

        public int Threshold
        {
            get { return threshold; }
            set
            {
                threshold = value;
                tbThreshold.Text = value.ToString();
            }
        }

        private void thresholdUp_Click(object sender, RoutedEventArgs e)
        {
            Threshold++;
        }

        private void thresholdDown_Click(object sender, RoutedEventArgs e)
        {
            if (Threshold > 1)
            {
                Threshold--;
            }
        }

        private void txtThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbThreshold == null)
            {
                return;
            }
            if (!int.TryParse(tbThreshold.Text, out threshold))
            {
                tbThreshold.Text = threshold.ToString();
            }
        }
    }
}