using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReversibleSignatureAnalyzer.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            HideStartUp();
        }

        private void HideStartUp()
        {
            //btn_import_file.Visibility = Visibility.Collapsed;
            //tv_import_file_path.Visibility = Visibility.Collapsed;

            btn_export_file.Visibility = Visibility.Collapsed;
            tv_export_file_path.Visibility = Visibility.Collapsed;
        }

        private void btn_import_file_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Btn_AddSignature_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Btn_AnalyzeSignature_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
