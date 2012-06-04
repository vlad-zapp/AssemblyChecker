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
		static DataTemplate HeaderTemplate;

		public MainWindow(string filePath)
		{
			InitializeComponent();
			AsmCheckerNode.AllHeadersTemplate = FindResource("header_template") as DataTemplate;
			try
			{
				XElement xml = XElement.Load(filePath);

				Dump.ApplyPatch(xml, XElement.Load(Path.ChangeExtension(filePath,null)+"-report.xml"));

				var node = new AsmCheckerNode(xml);
				treeView1.Items.Add(node);

				BuildTree(xml,node);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error);
				App.Current.Shutdown();
			}
		}

		private void BuildTree(XElement xml, AsmCheckerNode parentNode)
		{
			foreach (XElement e in xml.Elements().Where(e=>e.Name!="Parameter"))
			{
				AsmCheckerNode node = new AsmCheckerNode(e);
				BuildTree(e, node);
				parentNode.Items.Add(node);
			}
		}
	}
}
