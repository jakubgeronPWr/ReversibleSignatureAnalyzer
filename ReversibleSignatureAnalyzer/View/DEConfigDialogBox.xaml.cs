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
            tbIterations.Text = _numValue.ToString();
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

        private int _numValue = 1;

        public int NumValue
        {
            get { return _numValue; }
            set
            {
                _numValue = value;
                tbIterations.Text = value.ToString();
            }
        }

        private void cmdUp_Click(object sender, RoutedEventArgs e)
        {
            NumValue++;
        }

        private void cmdDown_Click(object sender, RoutedEventArgs e)
        {
            if (_numValue > 1)
            {
                NumValue--;
            }     
        }

        private void txtNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbIterations == null)
            {
                return;
            }
            if (!int.TryParse(tbIterations.Text, out _numValue))
            {
                _numValue = 1;
                tbIterations.Text = _numValue.ToString();
            }
        }
    }
}