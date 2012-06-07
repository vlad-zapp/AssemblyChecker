using System;
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
		public bool dumpChanged = false;
		public bool patchChanged = false;
		public bool reportChanged = false;

		private string dumpFile, reportFile, patchFile;

		private XElement DumpXml;

		public MainWindow(string dumpFile, string reportFile, string patchFile)
		{
			InitializeComponent();
			LoadTree(dumpFile, patchFile, reportFile);
		}

		#region TreeView

		private void TreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
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

		private void LoadTree(string dumpFile, string reportFile, string patchFile)
		{
			try
			{
				if (!File.Exists(dumpFile))
				{
					return;
				}
				
				DumpXml = XElement.Load(dumpFile);
				this.dumpFile = dumpFile;

				if (File.Exists(reportFile))
				{
					XElement inputReport = XElement.Load(reportFile);
					Dump.ApplyPatch(DumpXml, inputReport);
					this.reportFile = reportFile;
				}

				if (File.Exists(patchFile))
				{
					XElement inputPatch = XElement.Load(patchFile);
					Dump.ApplyPatch(DumpXml, inputPatch);
					this.patchFile = patchFile;
				}

				//variables setup
				showDump.IsChecked = true;
				showPatch.IsChecked = false;
				showReport.IsChecked = false;

				treeView1.ItemsSource = new List<XElement> { DumpXml };
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
			Dump.ApplyPatch((treeView1.ItemsSource as IEnumerable<XElement>).LastOrDefault(), oldsrc);

			var fullDump = (treeView1.ItemsSource as IEnumerable<XElement>).LastOrDefault();

			if (showPatch.IsChecked)
			{
				treeView1.ItemsSource = new List<XElement> { Report.GenerateReport(fullDump, true) };
			}
			else if (showReport.IsChecked)
			{
				treeView1.ItemsSource = new List<XElement> { Report.GenerateReport(fullDump, false) };
			}
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

		#region Context menu

		private void IgnoreItemClick(object sender, RoutedEventArgs e)
		{
			XElement item = (treeView1.SelectedItem as XElement);

			if (item == null)
				return;

			item.SetAttributeValue("Compatible", item.GetValue("Compatible") != "true" ? "true" : null);
			TreeViewSelectedItemChanged(treeView1, null);
			patchChanged = true;
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

			if(showDump.IsChecked)
			{
				dumpChanged = true;
			}
		}

		private void ItemContextMenuOpened(object sender, RoutedEventArgs e)
		{
			if (treeView1.SelectedItem == null)
				return;

			((sender as ContextMenu).Items[0] as MenuItem).IsChecked =
				(treeView1.SelectedItem as XElement).GetValue("Compatible") == "true";
		}

		#endregion

		#region Main menu

		private void ShowOpenDialog(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenDlgWindow();
			dialog.ShowDialog();
			LoadTree(dialog.Dump, dialog.Patch, dialog.Report);
			dumpFile = dialog.Dump;
			reportFile = dialog.Report;
			patchFile = dialog.Patch;
		}

		private void ViewMenuItemChecked(object sender, RoutedEventArgs e)
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
			deleteItem.IsEnabled = showDump.IsChecked;
			UpdateTree();
		}

		private void CloseProgram(object sender, RoutedEventArgs e)
		{
			App.Current.Shutdown();
		}

		private void SaveDump(object sender, RoutedEventArgs e)
		{
			XElement dumpForSave = new XElement(DumpXml);
			Dump.ClearPatches(dumpForSave);
			dumpForSave.ProperSave(dumpFile);
			dumpChanged = false;
		}

		private void SaveReport(object sender, RoutedEventArgs e)
		{
			if (!String.IsNullOrEmpty(reportFile))
			{
				Report.GenerateReport(DumpXml, false).ProperSave(reportFile);
				reportChanged = false;
			}
		}

		private void SavePatch(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(patchFile))
			{
				patchFile = Path.ChangeExtension(dumpFile, null) + "-patch.xml";
			}
			Report.GenerateReport(DumpXml, true).ProperSave(patchFile);
			patchChanged = false;
		}

		public void SaveAll(object sender, RoutedEventArgs e)
		{
			SaveDump(sender, e);
			SavePatch(sender, e);
			SaveReport(sender, e);
		}

		#endregion

		private void About(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("\n\tReport viewer 1.0\n\nIt is a part of Assembly checker program\nhttp://github.com/vlad-zapp/AssemblyChecker\n\n©2012 Vladislav Grutsenko\nvgrutsenko@mirantis.com","About",MessageBoxButton.OK,MessageBoxImage.Information);
		}

	}
}
