using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AsmChecker
{
	static class Program
	{
		static int Main(string[] args)
		{
			try
			{
				return Work(args);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return -1;
			}
		}

		public static void Usage()
		{
			Console.WriteLine("Assembly compatibility checker for .NET");
			Console.WriteLine();
			Console.WriteLine("Usage: AsmChecker.exe -dump|-check files|directories [dump file] [options]");
			Console.WriteLine();
			Console.WriteLine("First argument specifies the action:");
			Console.WriteLine("\tDUMP assembly members signatures to a file, or\n\tCHECK assemblies compatibility with some existing dump");
			Console.WriteLine();
			Console.Write("Then you must specify at least one assembly file name, or directory,\nwhere it can be located. ");
			Console.Write("Directory path should always end with \\ symbol.\nIf you specify \"-d\" right berore it - the program will search for the files\nin subdirectories too.\n\n");
			Console.Write("After that you can supply a path to xml file, where the dump will be stored, or where to read it from (depends on specified action). ");
			Console.Write("If no file supplied - \nthe default will be used (prototypes.xml in the application folder)\n\n");
			Console.WriteLine("Options are for checking only.");
			Console.WriteLine("patch:file.xml - path to patch file. Default is %dump%-patch.xml");
			Console.WriteLine("report:file.xml - path to report file. Default is %dump%-report.xml");
			Console.WriteLine("Where %dump% - is a full name of dump file without extension");
		}

		static int Work(string[] args)
		{
			if (args.Length > 0 && (args[0].ToLowerInvariant() == "-dump" || args[0].ToLowerInvariant() == "-check"))
			{
				//TODO: improve detection of dirs
				List<string> files = args.Where(a => a.ToLowerInvariant().EndsWith(".dll") || a.ToLowerInvariant().EndsWith(".exe")).ToList();
				IEnumerable<string> dirs = args.Where(a => a == "." || a.EndsWith(@"\") /*|| a.IndexOf('\\') > a.IndexOf('.')*/);
				string xmlSrc = args.FirstOrDefault(a => a.ToLowerInvariant().EndsWith(".xml")) ?? "prototypes.xml";

				foreach (string dir in dirs)
				{
					bool recursive = dir.ToLowerInvariant().StartsWith("-r");
					string dirPath = Path.GetFullPath(recursive ? dir.Substring(2) : dir);
					files.AddRange(Directory.GetFiles(dirPath, "*.dll", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
					files.AddRange(Directory.GetFiles(dirPath, "*.exe", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
				}

				if (files.Count() <= 0)
				{
					Usage();
				}

				XElement assemblies = Dump.MakeDumps(files);

				if (args[0].ToLowerInvariant() == "-dump")
				{
					assemblies.ProperSave(xmlSrc);
					return 0;
				}

				if (args[0].ToLowerInvariant() == "-check")
				{
					string reportFile = args.SingleOrDefault(a => a.ToLowerInvariant().StartsWith("report:"));
					string patchFile = args.SingleOrDefault(a => a.ToLowerInvariant().StartsWith("patch:"));

					string xmlSrcFileName = Path.ChangeExtension(xmlSrc, null);
					string defaultReportFile = Path.GetFullPath(String.Format(@"{0}-report.xml", xmlSrcFileName));
					string defaultPatchFile = Path.GetFullPath(String.Format(@"{0}-patch.xml", xmlSrcFileName));

					XElement storedAssemblies = XElement.Load(xmlSrc);

					patchFile = String.IsNullOrEmpty(patchFile)
										? defaultPatchFile
										: patchFile.Substring("patch:".Length);

					if (File.Exists(patchFile))
					{
						XElement xmlPatch = XElement.Load(patchFile);
						Dump.ApplyPatch(storedAssemblies, xmlPatch);
					}

					Check.CheckAssemblies(storedAssemblies.Elements("Assembly"), assemblies.Elements("Assembly"));
					XElement report = Report.GenerateReport(storedAssemblies);
					report.Name = "CompaTibilityInfo";
					report.ProperSave(String.IsNullOrEmpty(reportFile) ? defaultReportFile : reportFile.Substring("report:".Length));

					if (report.HasElements)
					{
						Console.WriteLine("Compatibility test failed!");
						Console.WriteLine("Problems are:");
						Console.WriteLine(report);
						return 1;
					}
					else
					{
						Console.WriteLine("Compatibility test passed!");
						return 0;
					}
				}
			}

			Usage();
			return -1;
		}
	}
}