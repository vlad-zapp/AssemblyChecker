using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;

namespace AsmChecker.ReportViewer
{
	public class AsmCheckerNode : TreeViewItem
	{
		public bool? Compatible { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }

		public AsmCheckerNode(XElement source)
		{
			Name = source.GetValue("Name");
			Type = source.Name.LocalName;

			bool comp;
			if (bool.TryParse(source.GetValue("Compatible"), out comp))
			{
				Compatible = comp;
			}
			else
			{
				Compatible = null;
			}
		}
	}
}
