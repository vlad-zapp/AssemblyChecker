using System;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

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
				BuildTree(treeView1, XDocument.Load(filePath));
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error);
				App.Current.Shutdown();
			}
		}

		private void BuildTree(TreeView treeView, XDocument doc)
		{
			TreeViewItem treeNode = new TreeViewItem
			{
				//Should be Root
				Header = doc.Root.Name.LocalName,
				IsExpanded = true
			};
			treeView.Items.Add(treeNode);
			BuildNodes(treeNode, doc.Root);
		}

		private void BuildNodes(TreeViewItem treeNode, XElement element)
		{
			foreach (XElement childElement in element.Elements())
			{
				TreeViewItem childTreeNode = new AsmCheckerNode(element);
				treeNode.Items.Add(childTreeNode);
				BuildNodes(childTreeNode, childElement);
			}
		}
	}
}
