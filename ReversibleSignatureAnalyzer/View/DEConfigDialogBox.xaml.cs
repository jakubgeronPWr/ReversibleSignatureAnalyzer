using ReversibleSignatureAnalyzer.Controller.Algorithm;
using ReversibleSignatureAnalyzer.Controller.Algorithm.DifferenceExpansion;
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
        public DifferencesExpansionConfiguraitonDialogBox(DifferencesExpansionConfiguration standardDEConfig,
                                                          DifferenceExpansionBruteForceConfiguration bruteForceDEConfig,
                                                          AlgorithmConfiguration currentConfiguration)
        {
            InitializeComponent();

            if (standardDEConfig != null)
            {
                this.threshold = standardDEConfig.Threeshold;
                tbThreshold.Text = threshold.ToString();
                cbEmbeddingDirection.SelectedIndex = (int)standardDEConfig.EmbeddingDirection;
                cbR.IsChecked = standardDEConfig.EmbeddingChanels.Contains(EmbeddingChanel.R);
                cbG.IsChecked = standardDEConfig.EmbeddingChanels.Contains(EmbeddingChanel.G);
                cbB.IsChecked = standardDEConfig.EmbeddingChanels.Contains(EmbeddingChanel.B);
                ComboBoxItem standardComboBoxItem = new ComboBoxItem();
                standardComboBoxItem.Content = "Standard";
                standardComboBoxItem.IsSelected = currentConfiguration is DifferencesExpansionConfiguration;
                cbConfigurationType.Items.Add(standardComboBoxItem);
            }

            if (bruteForceDEConfig != null)
            {
                cbHorizontal_BF.IsChecked = bruteForceDEConfig.EmbeddingDirections.Contains(Direction.Horizontal);
                cbVertical_BF.IsChecked = bruteForceDEConfig.EmbeddingDirections.Contains(Direction.Vertical);
                cbR_BF.IsChecked = bruteForceDEConfig.EmbeddingChanels.Contains(EmbeddingChanel.R);
                cbG_BF.IsChecked = bruteForceDEConfig.EmbeddingChanels.Contains(EmbeddingChanel.G);
                cbB_BF.IsChecked = bruteForceDEConfig.EmbeddingChanels.Contains(EmbeddingChanel.B);
                ComboBoxItem bruteForceItem = new ComboBoxItem();
                bruteForceItem.Content = "Brute force";
                bruteForceItem.IsSelected = currentConfiguration is DifferenceExpansionBruteForceConfiguration;
                cbConfigurationType.Items.Add(bruteForceItem);
            }
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

        private void cbConfigurationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbConfigurationType.SelectedItem != null)
            {
                string selection = (e.AddedItems[0] as ComboBoxItem).Content as string;
                if (selection == "Standard")
                {
                    GdStandardConfiguration.Visibility = Visibility.Visible;
                    GdBruteForceConfiguration.Visibility = Visibility.Collapsed;
                }
                if (selection == "Brute force")
                {
                    GdBruteForceConfiguration.Visibility = Visibility.Visible;
                    GdStandardConfiguration.Visibility = Visibility.Collapsed;
                }
                
            }
        }

        private void okButton_BF_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValid(this))
            {
                return;
            }
            DialogResult = true;
        }
    }
}