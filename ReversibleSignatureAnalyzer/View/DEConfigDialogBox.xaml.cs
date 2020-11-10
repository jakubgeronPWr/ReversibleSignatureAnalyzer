using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace ConfigurationDialogBox
{
    public partial class DifferencesExpansionConfiguraitonDialogBox : Window
    {
        public DifferencesExpansionConfiguraitonDialogBox()
        {
            InitializeComponent();
            tbIterations.Text = iterationsNumber.ToString();
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

        private int iterationsNumber = 1;

        public int IterationsNumber
        {
            get { return iterationsNumber; }
            set
            {
                iterationsNumber = value;
                tbIterations.Text = value.ToString();
            }
        }

        private void cmdUp_Click(object sender, RoutedEventArgs e)
        {
            IterationsNumber++;
        }

        private void cmdDown_Click(object sender, RoutedEventArgs e)
        {
            if (iterationsNumber > 1)
            {
                IterationsNumber--;
            }     
        }

        private void txtNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbIterations == null)
            {
                return;
            }
            if (!int.TryParse(tbIterations.Text, out iterationsNumber))
            {
                iterationsNumber = 1;
                tbIterations.Text = iterationsNumber.ToString();
            }
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
                iterationsNumber = 1;
                tbThreshold.Text = threshold.ToString();
            }
        }
    }
}