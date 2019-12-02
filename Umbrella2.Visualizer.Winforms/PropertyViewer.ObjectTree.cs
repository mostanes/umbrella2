using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Umbrella2.PropertyModel;

namespace Umbrella2.Visualizer.Winforms
{
	public partial class PropertyViewer
	{
		private const string PlaceHolderText = "_xPHD";
		Dictionary<TreeNode, MemberInfo> Members;

		/// <summary>
		/// Creates the property tree.
		/// </summary>
		public void ShowProperties()
		{
			Members = new Dictionary<TreeNode, MemberInfo>();
			foreach (var kvp in MultiObjectProperties)
			{
				TreeNodeCollection tnc = treeView1.Nodes;
				if (kvp.Key != string.Empty)
					tnc = treeView1.Nodes.Add(kvp.Key).Nodes;

				foreach (var prop in kvp.Value)
				{
					TreeNode Parent = tnc.Add(prop.GetType().Name);
					var membinfos = prop.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
					foreach (var minfo in membinfos)
					{
						var attrs = minfo.GetCustomAttributes(typeof(PropertyDescriptionAttribute), true);
						if (attrs.Length == 0) continue;

						DrawMember(minfo, Parent, prop);
					}
				}
			}
		}

		/// <summary>Draws a field/property.</summary>
		void DrawMember(MemberInfo minfo, TreeNode Parent, object Obj)
		{
			if (GetValue(minfo, Obj, out object value))
			{
				TreeNode tn = Parent.Nodes.Add(Format(minfo.Name, value));
				Members.Add(tn, minfo);
				var tp = GetType(minfo);
				if (tp.IsPrimitive)
					return;
				if (value == null)
					return;

				if ((typeof(System.Collections.IEnumerable)).IsAssignableFrom(tp))
				{
					foreach (var w in ((System.Collections.IEnumerable)value))
					{
						TreeNode tx = tn.Nodes.Add(w == null ? string.Empty : w.ToString());
						tx.Tag = w;
						tx.Nodes.Add(PlaceHolderText);
					}
				}

				tn.Tag = value;
				tn.Nodes.Add(PlaceHolderText);
			}
		}

		/// <summary>Dynamic tree generator.</summary>
		void TreeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			if (e.Node.Tag != null)
			{
				object obj = e.Node.Tag;
				e.Node.Tag = null;
				foreach (TreeNode tn in e.Node.Nodes) if (tn.Text == PlaceHolderText) tn.Remove();
				DrawObject(obj, e.Node, UmbrellaBindingPolicy);
			}
		}

		/// <summary>If the input is field/property, gets the value and returns true.</summary>
		static bool GetValue(MemberInfo minfo, object Obj, out object Value)
		{
			if (minfo is PropertyInfo pi)
				try { Value = pi.GetValue(Obj, null); return true; } catch { Value = null; return false; }
			if (minfo is FieldInfo fi)
				try { Value = fi.GetValue(Obj); return true; } catch { Value = null; return false; }
			Value = null;
			return false;
		}

		/// <summary>Returns the type of the field/property.</summary>
		static System.Type GetType(MemberInfo minfo)
		{
			if (minfo is PropertyInfo pi) return pi.PropertyType;
			if (minfo is FieldInfo fi) return fi.FieldType;
			return null;
		}

		/// <summary>
		/// Draws the object in the tree.
		/// </summary>
		/// <param name="Obj">Object to draw.</param>
		/// <param name="Parent">Parent node.</param>
		/// <param name="Policy">The policy for selecting displayed members.</param>
		void DrawObject(object Obj, TreeNode Parent, BindingPolicy Policy)
		{
			if (Obj == null) return;
			var membinfos = Obj.GetType().GetMembers(Policy(Obj.GetType()));
			foreach (var minfo in membinfos)
			{
				DrawMember(minfo, Parent, Obj);
			}
		}

		/// <summary>Formats the given <paramref name="Name"/> - <paramref name="Value"/> pair for display in the tree.</summary>
		static string Format(string Name, object Value)
		{
			string Vstr = Value == null ? string.Empty : Value.ToString();
			return Name + ":" + Vstr;
		}

		private static BindingFlags UmbrellaFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
		private static BindingFlags ExternalFlags = BindingFlags.Public | BindingFlags.Instance;

		/// <summary>
		/// Umbrella default binding policy. Shows all members of Umbrella2 objects and public ones of external objects.
		/// </summary>
		static BindingFlags UmbrellaBindingPolicy(Type t) => t.FullName.StartsWith("Umbrella2") ? UmbrellaFlags : ExternalFlags;

		/// <summary>Selects which members should be shown for a given type.</summary>
		delegate BindingFlags BindingPolicy(Type t);
	}
}
