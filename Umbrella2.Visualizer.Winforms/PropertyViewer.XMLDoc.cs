using System;
using System.IO;
using System.Xml.XPath;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Umbrella2.Visualizer.Winforms
{
	public partial class PropertyViewer
	{
		/// <summary>Regex for matching XML see nodes in documentation.</summary>
		readonly static Regex SeeNode = new Regex("<see .*/>", RegexOptions.Compiled);

		/// <summary>Tries to compute the path to documentation for a given type.</summary>
		static string GetXMLDocFilePath(Type t) => Path.ChangeExtension(t.Assembly.Location, ".xml");

		/// <summary>XML strings for the given documented element.</summary>
		struct ShownDocumentation
		{
			public string SummaryXML;
			public string RemarksXML;
		}

		/// <summary>Attempts to find documentation for a given type member.</summary>
		ShownDocumentation OpenDocumentation(System.Reflection.MemberInfo mi)
		{
			ShownDocumentation shd = new ShownDocumentation();
			try
			{
				string Path = GetXMLDocFilePath(mi.DeclaringType);
				XDocument xdoc = XDocument.Load(Path);
				var fldXel = xdoc.XPathSelectElement("/doc/members/member[@name='F:" + mi.DeclaringType.FullName + "." + mi.Name + "']");
				if (fldXel == null)
					fldXel = xdoc.XPathSelectElement("/doc/members/member[@name='P:" + mi.DeclaringType.FullName + "." + mi.Name + "']");
				try { shd.SummaryXML = fldXel.Element("summary").Value; } catch { }
				try { shd.RemarksXML = fldXel.Element("remarks").Value; } catch { }
			}
			catch (Exception ex) {; }
			return shd;
		}

		/// <summary>Attempts to find documentation for a given type.</summary>
		ShownDocumentation OpenDocumentation(Type t)
		{
			ShownDocumentation shd = new ShownDocumentation();
			try
			{
				string Path = GetXMLDocFilePath(t);
				XDocument xdoc = XDocument.Load(Path);
				var fldXel = xdoc.XPathSelectElement("/doc/members/member[@name='T:" + t.FullName + "']");
				try { shd.SummaryXML = fldXel.Element("summary").Value; } catch { }
				try { shd.RemarksXML = fldXel.Element("remarks").Value; } catch { }
			}
			catch (Exception ex) {; }
			return shd;
		}

		/// <summary>Evaluates replacements for see nodes.</summary>
		string SeeReplaceEvaluator(Match m)
		{
			string Fval = XDocument.Parse(m.Value).Root.Attribute("cref").Value;
			int idx = Fval.IndexOf(':');
			if (idx != -1) return Fval.Substring(idx + 1);
			else return Fval;
		}

		/// <summary>Replaces sees from XML input to RTF output.</summary>
		string RTFReplaceSee(string Input) => SeeNode.Replace(Input, SeeReplaceEvaluator);
	}
}
