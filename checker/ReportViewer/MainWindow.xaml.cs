using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
		public XElement DumpXml;

		public MainWindow(XElement dump, XElement report, XElement patch)
		{
			InitializeComponent();
			try
			{
				DumpXml = dump;

				//variables setup
				showDump.IsChecked = true;
				showPatch.IsChecked = false;
				showReport.IsChecked = false;

				Dump.ApplyPatch(dump, report);
				Dump.ApplyPatch(dump, patch);
				treeView1.ItemsSource = new List<XElement> { dump };
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				App.Current.Shutdown();
			}
		}

		private void UpdateTree()
		{
			var oldsrc = (treeView1.ItemsSource as IEnumerable<XElement>).LastOrDefault();
			treeView1.ItemsSource = new List<XElement> { DumpXml };
			Dump.ApplyPatch((treeView1.ItemsSource as IEnumerable<XElement>).LastOrDefault(),oldsrc);
			var t = (treeView1.ItemsSource as IEnumerable<XElement>).LastOrDefault();

			if (showPatch.IsChecked)
			{
				treeView1.ItemsSource = new List<XElement> { Report.GenerateReport(t, true) };
			}
			else if (showReport.IsChecked)
			{
				treeView1.ItemsSource = new List<XElement> { Report.GenerateReport(t, false) };
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

		private void MenuItem_Checked(object sender, RoutedEventArgs e)
		{
			showDump.IsChecked = false;
			showReport.IsChecked = false;
			showPatch.IsChecked = false;

			MenuItem senderItem = (sender as MenuItem);
			if (senderItem == null)
			{
				return;
			}

			senderItem.IsChecked = true;
			UpdateTree();
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
	}
}
