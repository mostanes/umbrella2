using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2;
using Umbrella2.PropertyModel;

namespace Umbrella2.PropertyModel.CommonProperties
{
	/// <summary>
	/// Contains information on the identity of the object observed (i.e. which celestial body it is).
	/// </summary>
	public class ObjectIdentity : IExtensionProperty
	{
		/// <summary>Name of the observed object.</summary>
		[PropertyDescription(true)]
		public string Name;

		/// <summary>Minor planet number.</summary>
		[PropertyDescription(true)]
		public int? MPN;

		/// <summary>Packed provisional designation.</summary>
		[PropertyDescription(true)]
		public string PackedPD;
		/// <summary>Packed minor planet number.</summary>
		[PropertyDescription(true)]
		public string PackedMPN;

		/// <summary>Scores for each possible object name.</summary>
		[PropertyDescription(true)]
		public Dictionary<string, int> NameScore = new Dictionary<string, int>();

		/// <summary>Distances from object to name.</summary>
		Dictionary<string, double> Distances = new Dictionary<string, double>();
		/// <summary>Number of times the name appeared.</summary>
		Dictionary<string, int> Counts = new Dictionary<string, int>();
		/// <summary>Minor planet numbers.</summary>
		Dictionary<string, int?> ObjIDs = new Dictionary<string, int?>();

		const double Arc1Sec = Math.PI / 3600 / 180;
		/// <summary>Minimum score to name an object.</summary>
		const int NScore = 10;

		/// <summary>Adds a possible name for an object.</summary>
		public void AddName(string Name, int? ObjID, double Distance)
		{
			if (!Distances.ContainsKey(Name)) { Distances.Add(Name, 0); Counts.Add(Name, 0); ObjIDs.Add(Name, ObjID); }
			Distances[Name] += Distance;
			Counts[Name]++;
		}

		/// <summary>Computes <see cref="NameScore"/> and attempts to set <see cref="Name"/>.</summary>
		public void ComputeNamescore(Tracklet t)
		{
			string XName = null;
			double CScore = -1;
			foreach(var kvp in Distances)
			{
				double Mean = kvp.Value / Counts[kvp.Key];
				double XScore = Math.Pow(2, Counts[kvp.Key] * 0.5) / (Arc1Sec + Mean);
				double MaxScore = Math.Pow(2, 0.5 * t.Detections.Count((x) => x != null)) / Arc1Sec;
				double Score = 100 * XScore / MaxScore;
				if (Score > 1)
					NameScore.Add(kvp.Key, (int)Score);
			}
			foreach (var kvp in NameScore)
				if (kvp.Value > CScore) { CScore = kvp.Value; XName = kvp.Key; }
			if (CScore > NScore)
			{
				Name = XName;
				MPN = ObjIDs[Name];
				if (MPN.HasValue)
					PackMPN();
				if (ProvisionalDesignationMatcher.IsMatch(Name))
					PackedPD = PackPD(Name);
			}
		}

		/// <summary>
		/// Computes <see cref="NameScore"/> and attempts to set <see cref="Name"/>.
		/// Uses the provided <paramref name="Default"/> name as the lowest priority option.
		/// Can compute <paramref name="Default"/> from <paramref name="FieldName"/>, <paramref name="ObjNum"/> and optionally <paramref name="CCD"/>.
		/// If no <paramref name="Default"/> is given and it cannot be computed, skips using a default name.
		/// </summary>
		/// <remarks>
		/// If no <paramref name="CCD"/> value is given, generates object names as FieldName + ObjectNumber base 10 (3 digits).
		/// If <paramref name="CCD"/> is non-null, then generates name as FieldName + CCD base-62 (1 digit) + ObjectNumber (packed).
		/// </remarks>
		/// <param name="t">Tracklet to be named.</param>
		/// <param name="Default">Default name. If null, it is generated from the next parameters. Otherwise, the other parameters can be null.</param>
		/// <param name="FieldName">Field name. Must be 4 characters long.</param>
		/// <param name="CCD">CCD Number. If present, uses the MPC 0-9, A-Z, a-z packing for both the CCD and the object number.</param>
		/// <param name="ObjNum">Object number.</param>
		public void ComputeNamescoreWithDefault(Tracklet t, string Default = null, string FieldName = null, int? CCD = null, int? ObjNum = null)
		{
			if (Default == null & FieldName != null & ObjNum.HasValue && FieldName.Length == 4)
			{
				if (CCD.HasValue)
					try { Default = FieldName + GetB62Char(CCD.Value) + GetObjNumber(ObjNum.Value); }
					catch { Default = null; }
				else Default = FieldName + ObjNum.Value.ToString("000");
			}
			if (Default != null)
			{ NameScore.Add(Default, NScore + 1); ObjIDs[Default] = null; }

			ComputeNamescore(t);

			if (Name == Default)
				PackedPD = Default;
		}

		private static string GetObjNumber(int ObjNum)
		{
			if (ObjNum < 100) return ObjNum.ToString("00");
			ObjNum += 520;
			return "" + GetB62Char(ObjNum / 62) + GetB62Char(ObjNum % 62);
		}

		/// <summary>Packs the minor planet number to MPC packed form.</summary>
		private void PackMPN()
		{
			if (MPN.Value < 100000) { PackedMPN = MPN.Value.ToString("00000"); return; }
			if (MPN.Value < 620000)
			{
				int XC = MPN.Value / 10000;
				int RM = MPN.Value % 10000;
				PackedMPN = GetB62Char(XC) + RM.ToString("0000");
				return;
			}
			if(MPN.Value < 15396336)
			{
				int Val = MPN.Value - 620000;
				PackedMPN = "~" + GetB62Char(Val / 238328) + GetB62Char(Val / 3844 % 62) + GetB62Char(Val / 62 % 62) + GetB62Char(Val % 62);
				return;
			}

			throw new ArgumentOutOfRangeException("MPN", "Object ID too large.");
		}

		/// <summary>Provisional designation rule.</summary>
		private const string FRegex = "[0-9]{4} [A-Z]{2}[0-9]{0,3}";
		/// <summary>Matches provisional designations.</summary>
		public readonly static System.Text.RegularExpressions.Regex ProvisionalDesignationMatcher = new System.Text.RegularExpressions.Regex(FRegex,
			System.Text.RegularExpressions.RegexOptions.Compiled);

		/// <summary>Converts a provisional designation to its packed form.</summary>
		private static string PackPD(string ObjName)
		{
			if (ObjName.Length < 7) return ObjName.PadRight(7);
			char[] map = new char[7];
			int N = (ObjName[0] - '0') * 10 + (ObjName[1] - '0');
			map[0] = (char)('I' + (N - 18));
			map[1] = ObjName[2];
			map[2] = ObjName[3];
			map[3] = ObjName[5];
			map[6] = ObjName[6];

			int ObjId = int.Parse(ObjName.Substring(7));
			map[4] = GetB62Char(ObjId / 10);
			map[5] = (char)(ObjId % 10 + '0');

			return new string(map);
		}

		private static char GetB62Char(int V)
		{
			if (V < 10) return (char)(V + '0');
			V -= 10;
			if (V + 'A' <= 'Z') return (char)(V + 'A');
			V -= 26;
			if (V + 'a' <= 'z') return (char)(V + 'a');
			throw new ArgumentOutOfRangeException(nameof(V), "Not a base-62 digit.");
		}
	}
}
