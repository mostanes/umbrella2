using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace Umbrella2.Pipeline.ExtraIO
{
	/// <summary>
	/// Allows Umbrella to run a local installation of digest2 over the specified detections.
	/// </summary>
	/// <remarks>
	/// Currently supports only digest2 version 0.19
	/// </remarks>
	public static class LocalDigest2
	{
		/// <summary>Path to the digest2 executable.</summary>
		static string ExecutablePath = null;

		/// <summary>
		/// Result of the digest2 for an object.
		/// </summary>
		public struct EvaluationResult
		{
			public string Designation;
			public double RMS;
			public int IntScore;
			public int NEOScore;
			public int N22Score;
			public int N18Score;
			public Dictionary<string, int> OtherScores;
		}

		/// <summary>
		/// Checks and sets the path to the executable. Also checks version compatibility.
		/// </summary>
		/// <param name="Path">The path to the executable.</param>
		public static void CheckAndSetPath(string Path)
		{
			if (!File.Exists(Path))
				throw new FileNotFoundException("Could not find executable", Path);
			Process p;
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo(Path, "--version") { RedirectStandardOutput = true };
				p = Process.Start(psi);
				p.WaitForExit();
			}
			catch (Exception ex) { throw new Exception("Could not run executable."); }
			bool version = p.StandardOutput.ReadLine().StartsWith("Digest2 version 0.19 -- Released August 16, 2017");
			if (!version)
				throw new Exception("Version not supported.");

			ExecutablePath = Path;
		}

		/// <summary>
		/// Parses the output of the digest2 algorithm.
		/// </summary>
		/// <returns>The parsed results.</returns>
		/// <param name="DigestOutput">digest2 output.</param>
		public static Dictionary<string, EvaluationResult> ParseOutput(StreamReader DigestOutput)
		{
			string hdout = DigestOutput.ReadLine();
			string[] Head = hdout.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			if (Head.Length != 8 || Head[0] != "Desig." || Head[1] != "RMS" || Head[2] != "Int" || Head[3] != "NEO" ||
				Head[4] != "N22" || Head[5] != "N18" || Head[6] != "Other" || Head[7] != "Possibilities")

				throw new FormatException("Could not parse header");

			Dictionary<string, EvaluationResult> Results = new Dictionary<string, EvaluationResult>();

 			while(!DigestOutput.EndOfStream)
			{
				string dout = DigestOutput.ReadLine();
				string[] Data = dout.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
				EvaluationResult evres = new EvaluationResult()
				{
					Designation = Data[0],
					RMS = double.Parse(Data[1]),
					IntScore = int.Parse(Data[2]),
					NEOScore = int.Parse(Data[3]),
					N22Score = int.Parse(Data[4]),
					N18Score = int.Parse(Data[5]),
					OtherScores = new Dictionary<string, int>()
				};
				for (int i = 6; i < Data.Length; i += 2)
				{
					int PV = Data[i + 1] == "<1)" ? 0 : int.Parse(Data[i + 1].Substring(0, Data[i + 1].Length - 1));
					evres.OtherScores.Add(Data[i].Substring(1), PV);
				}

				Results.Add(evres.Designation, evres);
			}
			return Results;
		}

		/// <summary>
		/// Computes scores for the observations in the given file.
		/// </summary>
		/// <returns>Observations' scores.</returns>
		/// <param name="Path">Path to the observations file.</param>
		public static Dictionary<string, EvaluationResult> FromFile(string Path)
		{
			ProcessStartInfo psi = new ProcessStartInfo(ExecutablePath, "\"" + Path + "\"") { RedirectStandardOutput = true };
			Process p = Process.Start(psi);
			return ParseOutput(p.StandardOutput);
		}

		/// <summary>
		/// Computes scores for the input <paramref name="Observations"/>.
		/// </summary>
		/// <returns>Observations's score.</returns>
		/// <param name="Observations">The observations in MPC format.</param>
		public static Dictionary<string, EvaluationResult> FromObservations(string Observations)
		{
			ProcessStartInfo psi = new ProcessStartInfo(ExecutablePath, "-") { RedirectStandardOutput = true, RedirectStandardInput = true };
			Process p = Process.Start(psi);
			p.StandardInput.Write(Observations);
			p.StandardInput.Flush();
			return ParseOutput(p.StandardOutput);
		}
	}
}
