using System;
using System.Windows;
using System.Windows.Controls;
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
				BuildTree(treeView1, XElement.Load(filePath));
				treeView1.Items.Add("asd");
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error);
				App.Current.Shutdown();
			}
		}

		private void BuildTree(TreeView treeView, XElement xml)
		{
			xml.Elements().Select(e => treeView.Items.Add(e.ToString()));
		}
	}
}
