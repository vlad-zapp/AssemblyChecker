using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AsmChecker
{
	/// <summary>
	/// Interaction logic for OpenDlgWindow.xaml
	/// </summary>
	public partial class OpenDlgWindow : Window
	{
		public string Dump = null;
		public string Report = null;
		public string Patch = null;

		public OpenDlgWindow()
		{
			InitializeComponent();
		}

		private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
			if(File.Exists(dumpFile.Text))
			{
				string bareName = Path.ChangeExtension(dumpFile.Text, null);
				string defaultPatch = bareName + "-patch.xml";
				string defaultReport = bareName + "-report.xml";
				if (File.Exists(defaultPatch))
				{
					patchFile.Text = defaultPatch;
				}
				if (File.Exists(defaultReport))
				{
					reportFile.Text = defaultReport;
				}
				patchFile.IsEnabled = true;
				selectPatchFile.IsEnabled = true;
				reportFile.IsEnabled = true;
				selectReportFile.IsEnabled = true;
			} 
				else
			{
				patchFile.Text = string.Empty;
				patchFile.IsEnabled = false;
				selectPatchFile.IsEnabled = false;
				reportFile.Text = string.Empty;
				reportFile.IsEnabled = false;
				selectReportFile.IsEnabled = false;
			}
		}

		private void ButtonClick(object sender, RoutedEventArgs e)
		{
			Dump = dumpFile.Text;
			Report = reportFile.Text;
			Patch = patchFile.Text;
			DialogResult = true;
			Close();
		}

		private string openFileDialog()
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "xml files|*.xml";
			dlg.Multiselect = false;
			dlg.ShowDialog();
			return dlg.FileName;
		}

		private void selectDumpFile_Click(object sender, RoutedEventArgs e)
		{
			dumpFile.Text = openFileDialog();
		}

		private void selectPatchFile_Click(object sender, RoutedEventArgs e)
		{
			patchFile.Text = openFileDialog();
		}

		private void selectReportFile_Click(object sender, RoutedEventArgs e)
		{
			reportFile.Text = openFileDialog();
		}
	}
}
