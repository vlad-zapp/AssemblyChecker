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
		public bool DataChanged = false;

		public MainWindow(XElement dump, XElement report, XElement patch)
		{
			InitializeComponent();
			try
			{
				Dump.ApplyPatch(dump, report);
				Dump.ApplyPatch(dump, patch);
				treeView1.ItemsSource = dump.Elements();
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error);
				App.Current.Shutdown();
			}
		}

		#region Context menu and TreeView right click handling

		private void MenuItemClick(object sender, RoutedEventArgs e)
		{
			XElement item = (treeView1.SelectedItem as XElement);

			if (item == null)
				return;

			item.SetAttributeValue("Compatible",item.GetValue("Compatible")!="true"?"true":null);
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
				DataChanged = true;
				(sender as TreeViewItem).IsSelected = true;
				e.Handled = true;
			}
		}
		#endregion
	}
}
