using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace ReportViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow(string filePath)
		{
			InitializeComponent();
			BuildTree(treeView1,XDocument.Load(filePath));
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
			foreach (XNode child in element.Nodes())
			{
				switch (child.NodeType)
				{
					case XmlNodeType.Element:
						XElement childElement = child as XElement;
						TreeViewItem childTreeNode = new TreeViewItem();

						var textItem = new TextBlock {Text = childElement.Name + " " + childElement.Attribute("Name") + " "
							+string.Join(" ",childElement.Attributes().Where(a=>a.Name!="Name").Select(m=>m.ToString()))};

						if (Compatible(childElement,"false")==true)
						{
							textItem.Foreground = new SolidColorBrush(Colors.DarkRed);
						} 
						else if(Compatible(childElement,"true")==true)
						{
							textItem.Foreground = new SolidColorBrush(Colors.Green);
						}

						childTreeNode.Header = textItem;

						treeNode.Items.Add(childTreeNode);
						BuildNodes(childTreeNode, childElement);
						break;
				}
			}
		}

		private bool? Compatible(XElement node, string value)
		{
			if (node.Attribute("Compatible") != null)
				return node.Attribute("Compatible").Value.ToLowerInvariant() == value.ToLowerInvariant();

			if (node.Parent != null) 
				return Compatible(node.Parent, value);
			
			return false;
		}
	}
}
