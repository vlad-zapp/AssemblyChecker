using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace AsmChecker.ReportViewer
{
	public class AsmCheckerNode : TreeViewItem
	{
		public bool? Compatible { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }

		private static Dictionary<string, BitmapImage> Images;
		public static DataTemplate AllHeadersTemplate;

		public AsmCheckerNode(XElement source)
		{
			Name = source.Attribute("Name") != null ? source.Attribute("Name").Value : "Compatibility Info";
			if(source.Elements("Parameter").Count()>0)
			{
				Name = String.Format("{0}({1})", Name,
				                     String.Join(", ", source.Elements("Parameter").Select(p => p.GetValue("Type",false))));
			}

			HeaderTemplate = AllHeadersTemplate;
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

			Header = new
				{
					Name,
					Type,
					Compatible,
				};
			ToolTip = String.Join("\n", source.Attributes());
		}
	}
}
