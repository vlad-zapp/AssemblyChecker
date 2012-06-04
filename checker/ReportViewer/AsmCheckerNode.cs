using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

		static AsmCheckerNode()
		{
			Images = new Dictionary<string, BitmapImage>();
			Images.Add("CompatibilityInfo", new BitmapImage(new Uri(@"pack://Application:,,/Images/Info.png")));
			Images.Add("Assembly", new BitmapImage(new Uri(@"pack://Application:,,/Images/VSOBJECT_Assembly.png")));
			Images.Add("Class", new BitmapImage(new Uri(@"pack://Application:,,/Images/VSOBJECT_Class.png")));
			Images.Add("Enum", new BitmapImage(new Uri(@"pack://Application:,,/Images/VSOBJECT_Enum.png")));
			Images.Add("Field", new BitmapImage(new Uri(@"pack://Application:,,/Images/VSOBJECT_Field.png")));
			Images.Add("Interface", new BitmapImage(new Uri(@"pack://Application:,,/Images/VSOBJECT_Interface.png")));
			Images.Add("Method", new BitmapImage(new Uri(@"pack://Application:,,/Images/VSOBJECT_Method.png")));
			Images.Add("Acessor", new BitmapImage(new Uri(@"pack://Application:,,/Images/VSOBJECT_Method.png")));
			Images.Add("Property", new BitmapImage(new Uri(@"pack://Application:,,/Images/VSOBJECT_Properties.png")));
		}

		public AsmCheckerNode(XElement source)
		{
			Name = source.Attribute("Name") != null ? source.Attribute("Name").Value : "Compatibility Info";

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
					Name = Name,
					TypeImage = Images[Type]
				};
		}
	}
}
