using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace AsmChecker.ReportViewer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			if (e.Args.Count()>0 && e.Args[0].EndsWith(".xml"))
			{
				string fileName = Path.ChangeExtension(e.Args[0], null);
				string reportFile = e.Args.Count() > 1 ? e.Args[1] : fileName + "-report.xml";
				string patchFile = e.Args.Count() > 2 ? e.Args[2] : fileName + "-patch.xml";

				XElement inputReport=null,
						 inputPatch=null,
						 inputDump = XElement.Load(e.Args[0]);

				if(File.Exists(reportFile))
				{
					inputReport = XElement.Load(reportFile);
				}

				if(File.Exists(patchFile))
				{
					inputPatch = XElement.Load(patchFile);
				}

				var win = new MainWindow(inputDump,inputReport,inputPatch);
				win.ShowDialog();

				if (win.DataChanged && MessageBox.Show("Save changes?","Something changed",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)
				{
					inputReport = Report.GenerateReport(inputDump, false);
					inputReport.Name = "Report";

					inputPatch = Report.GenerateReport(inputDump, true);
					inputPatch.Name = "Patch";

					inputReport.ProperSave(reportFile);
					inputPatch.ProperSave(patchFile);
				}
			}

			this.Shutdown();
		}
	}
}
