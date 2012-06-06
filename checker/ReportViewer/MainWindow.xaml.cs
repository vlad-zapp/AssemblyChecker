using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;

namespace AsmChecker.ReportViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public bool DataChanged = false;
		private List<XElement> elements;

		public MainWindow(XElement dump, XElement report, XElement patch)
		{
			InitializeComponent();
			try
			{
				//variables setup
				showDump.IsChecked = true;
				showPatch.IsChecked = false;
				showReport.IsChecked = false;

				elements = new List<XElement> { dump, patch, report };
				
				//load tree content
				LoadTree();
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				App.Current.Shutdown();
			}
		}

		private void LoadTree(bool updatePatchAndReport=false)
		{
			if (elements.Count > 0)
			{
				if (updatePatchAndReport)
				{
					Dump.ApplyPatch(elements[0],elements[1]);
					Dump.ApplyPatch(elements[0], elements[2]);
					elements = elements.Take(1).ToList();
					elements.Add(Report.GenerateReport(elements[0], true));
					elements.Add(Report.GenerateReport(elements[0], false));
				}

				if (!showDump.IsChecked)
				{
					treeView1.ItemsSource = showPatch.IsChecked==true && showDump.IsChecked!=true? new List<XElement> {elements[1]} : new List<XElement> {elements[2]};
				}
				else
				{
					treeView1.ItemsSource = elements.Take(1);
					Dump.ApplyPatch(elements[0], elements[1]);
					Dump.ApplyPatch(elements[0], elements[2]);
				}

			}
		}

		private void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			XElement item = (sender as TreeView).SelectedItem as XElement;
			if (item == null)
			{
				infoPanel.Width = new GridLength(0);
				return;
			}

			Type.Content = item.Name.LocalName;
			Name.Text = item.GetValue("Name", false);
			attributesGrid.ItemsSource = item.Attributes();
			if (infoPanel.Width == new GridLength(0))
			{
				infoPanel.Width = new GridLength(350);
			}
		}

		#region Context menu and TreeView right click handling

		private void IgnoreItemClick(object sender, RoutedEventArgs e)
		{
			XElement item = (treeView1.SelectedItem as XElement);

			if (item == null)
				return;

			item.SetAttributeValue("Compatible", item.GetValue("Compatible") != "true" ? "true" : null);
			treeView1_SelectedItemChanged(treeView1, null);
			DataChanged = true;
		}

		private void DeleteItemClick(object sender, RoutedEventArgs e)
		{
			XElement item = (treeView1.SelectedItem as XElement);

			if (item == null)
				return;

			item.Descendants().Remove();
			if (item.Parent != null)
			{
				item.Remove();
			}

			DataChanged = true;
		}

		private void ContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			if (treeView1.SelectedItem == null)
				return;

			((sender as ContextMenu).Items[0] as MenuItem).IsChecked =
				(treeView1.SelectedItem as XElement).GetValue("Compatible") == "true";
		}

		private void treeView1_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (sender is TreeViewItem)
			{
				(sender as TreeViewItem).IsSelected = true;
				e.Handled = true;
			}
		}
		#endregion

		private void MenuItem_Checked(object sender, RoutedEventArgs e)
		{
			showDump.IsChecked = false;
			showReport.IsChecked = false;
			showPatch.IsChecked = false;

			if(elements!=null)
			{
				(sender as MenuItem).IsChecked = true;
				LoadTree(true);
			}
		}
	}
}
