using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Linq;

namespace AsmChecker.ReportViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow(string filePath)
		{
			InitializeComponent();
			try
			{
				XElement xml = XElement.Load(filePath);

				Dump.ApplyPatch(xml, XElement.Load(Path.ChangeExtension(filePath,null)+"-report.xml"));

				treeView1.ItemsSource = xml.Elements();


			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error);
				App.Current.Shutdown();
			}
		}

	}
}
