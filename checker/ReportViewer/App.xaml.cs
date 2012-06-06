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

			MainWindow win;

			//Start with commandline parameters
			if (e.Args.Any() && e.Args[0].EndsWith(".xml"))
			{
				string fileName = Path.ChangeExtension(e.Args[0], null);
				string reportFile = e.Args.Count() > 1 ? e.Args[1] : fileName + "-report.xml";
				string patchFile = e.Args.Count() > 2 ? e.Args[2] : fileName + "-patch.xml";
				win = new MainWindow(e.Args[0], patchFile, reportFile);

			}
 			//Start without parameters
			else
			{
				OpenDlgWindow opendWin = new OpenDlgWindow();
				while (!File.Exists(opendWin.Dump))
				{
					opendWin = new OpenDlgWindow();
					bool? result = opendWin.ShowDialog();
					if(result==false)
					{
						Shutdown();
						return;
					}
	
				}
				win = new MainWindow(opendWin.Dump,opendWin.Patch,opendWin.Report);
			}

			win.ShowDialog();

			if ((win.dumpChanged || win.patchChanged || win.reportChanged) 
				&& MessageBox.Show("Save changes?","Something changed",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)
			{
				win.SaveAll(null,null);			
			}
			
			Shutdown();
		}
	}
}
