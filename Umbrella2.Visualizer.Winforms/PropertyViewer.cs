using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Umbrella2.PropertyModel;

namespace Umbrella2.Visualizer.Winforms
{
	/// <summary>
	/// Provides a mechanism for viewing all the <see cref="IExtensionProperty"/> attached to given objects.
	/// </summary>
	public partial class PropertyViewer : Form
	{
		/// <summary>The top level set of objects for which to display properties.</summary>
		Dictionary<string, List<IExtensionProperty>> MultiObjectProperties;

		private PropertyViewer()
		{
			InitializeComponent();
		}

		/// <summary>Initializes a new instance with the given set of properties.</summary>
		public PropertyViewer(Dictionary<string, List<IExtensionProperty>> PropSet) : this()
		{ MultiObjectProperties = PropSet; }

		/// <summary>Initializes a new instance with properties from the given <paramref name="Object"/>.</summary>
		public PropertyViewer(IExtendable Object) : this(new Dictionary<string, List<IExtensionProperty>>()
			{{ string.Empty, new List<IExtensionProperty>(Object.ExtendedProperties.Values) }})
		{ }

		/// <summary>Initializes a new instance with properties from the given <paramref name="Object"/>, with the given name.</summary>
		public PropertyViewer(string ObjName, IExtendable Object) : this(new Dictionary<string, List<IExtensionProperty>>()
			{{ ObjName, new List<IExtensionProperty>(Object.ExtendedProperties.Values) }})
		{ }

		/// <summary>Adds properties to an existing object.</summary>
		public void AddProperties(string Object, params IExtensionProperty[] Props) => MultiObjectProperties[Object].AddRange(Props);

		/// <summary>Adds an object to the list of displayed objects.</summary>
		public void AddObject(string ObjName, IExtendable Obj) =>
			MultiObjectProperties.Add(ObjName, new List<IExtensionProperty>(Obj.ExtendedProperties.Values));

		/// <summary>Shows the documentation of a given node (where possible).</summary>
		void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if(Members.ContainsKey(e.Node))
			{
				var minfo = Members[e.Node];
				ShownDocumentation FieldDoc = OpenDocumentation(minfo);
				var tp = GetType(minfo);
				ShownDocumentation TypDoc = OpenDocumentation(tp);
				richTextBox1.Text = "Field: " + minfo.Name + "\n" + GetText(FieldDoc) + "\n--------\nType: " + tp.FullName + "\n" + GetText(TypDoc) + "\n";
			}
		}

		static string GetText(ShownDocumentation doc) => "Summary:\t" + (doc.SummaryXML ?? string.Empty) + "\nRemarks" + (doc.RemarksXML ?? string.Empty);
	}
}
