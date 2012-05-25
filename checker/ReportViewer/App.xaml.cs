using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace ReportViewer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			if (e.Args[0].EndsWith(".xml"))
			{
				var win = new MainWindow(e.Args[0]).ShowDialog();
			}

			this.Shutdown();
		}
	}
}
