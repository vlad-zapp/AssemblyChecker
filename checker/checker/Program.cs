using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Mono.Cecil;

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
				IEnumerable<string> dirs = args.Where(d => Directory.Exists(d.StartsWith("-r", true, CultureInfo.InvariantCulture) ? d.Substring(2) : d));
				List<string> files = args.Where(a => (a.EndsWith(".dll", true, CultureInfo.InvariantCulture) || a.EndsWith(".exe", true, CultureInfo.InvariantCulture)) && File.Exists(a)).ToList();
				string xmlSrc = args.FirstOrDefault(a => a.EndsWith(".xml", true, CultureInfo.InvariantCulture)) ?? "prototypes.xml";

				foreach (string dir in dirs)
				{
					bool recursive = dir.StartsWith("-r", true, CultureInfo.InvariantCulture);
					string dirPath = Path.GetFullPath(recursive ? dir.Substring(2) : dir);
					files.AddRange(Directory.GetFiles(dirPath, "*.dll", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
					files.AddRange(Directory.GetFiles(dirPath, "*.exe", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
				}

				if (files.Count() <= 0)
				{
					Usage();
				}

				XElement assemblies = Dump.MakeDumps(files, args[0].ToLowerInvariant() == "-check");

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
					report.Name = "CompatibilityInfo";
					report.ProperSave(String.IsNullOrEmpty(reportFile) ? defaultReportFile : reportFile.Substring("report:".Length));

					if (report.HasElements)
					{
						Console.WriteLine("Compatibility test failed!");
						Console.WriteLine("Problems are:");
						Console.WriteLine(report);
					}

					bool policyProblems = false;
					if (!args.Any(a => a == "-skipPolicy"))
					{
						foreach (string file in files)
						{
							AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(file);
							string asmModule = Path.GetFileNameWithoutExtension(asm.MainModule.Name);
							IEnumerable<string> policyFiles = Directory.GetFiles(Path.GetDirectoryName(file), String.Format("Policy.?.?.{0}.config", asmModule), SearchOption.TopDirectoryOnly);

							foreach (string policyFile in policyFiles)
							{
								XElement policyXml = XElement.Load(policyFile);
								try
								{
									XElement dependencyAssembly =
										policyXml.Element("runtime").Elements().FirstOrDefault(e => e.Name.LocalName == "assemblyBinding").Elements().FirstOrDefault(e => e.Name.LocalName == "dependentAssembly");
									XElement asmIdentity = dependencyAssembly.Element(dependencyAssembly.Name.Namespace + "assemblyIdentity");
									XElement bindingRedirect = dependencyAssembly.Element(dependencyAssembly.Name.Namespace + "bindingRedirect");

									string correctName = asmModule;
									string correctPublicKeyToken = asm.FullName.Split(',')[3].Split('=')[1];
									string correctCulture = asm.FullName.Split(',')[2].Split('=')[1];
									string correctNewVersion = asm.FullName.Split(',')[1].Split('=')[1];

									policyProblems = policyProblems |
										checkPolicyAttribute(asmIdentity.Attribute("name"), correctName, policyFile) |
										checkPolicyAttribute(asmIdentity.Attribute("publicKeyToken"), correctPublicKeyToken, policyFile) |
										checkPolicyAttribute(asmIdentity.Attribute("culture"), correctCulture, policyFile) |
										checkPolicyAttribute(bindingRedirect.Attribute("newVersion"), correctNewVersion, policyFile);
								}
								catch (Exception ex)
								{
									policyProblems = true;
									Console.WriteLine(String.Format("Can't parse policy file: {0}", policyFile));
								}
							}
						}
					}

					if (report.HasElements || policyProblems)
					{
						return 1;
					}

					Console.WriteLine("Compatibility test passed!");
					return 0;
				}
			}

			Usage();
			return -1;
		}

		private static bool checkPolicyAttribute(XAttribute src, string expectedValue, string policyFile)
		{
			if (src.Value != expectedValue)
			{
				Console.WriteLine(String.Format("Wrong {0} '{1}'\n in {2} in file: {3}", src.Name.LocalName, src.Value, src.Parent.Name.LocalName, policyFile));
				Console.WriteLine(String.Format("Should be: {0}", expectedValue));
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}