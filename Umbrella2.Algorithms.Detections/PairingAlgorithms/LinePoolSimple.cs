using System;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.Algorithms.Misc;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2.Algorithms.Pairing
{
	/// <summary>
	/// A <see cref="MDPoolCore"/> algorithm that works by considering line fitting residuals.
	/// </summary>
	public class LinePoolSimple : MDPoolCore
	{
		/// <summary>The maximum sum of residuals in arcsec.</summary>
		public double MaxLinErrorArcSec = 2.0;
		/// <summary>Amount added to the search radius if no objects are found within <see cref="SearchExtraSmall"/>.</summary>
		public double SearchExtraBig = 5.0;
		/// <summary>Amount added to the search radius to ensure the detections are properly found.</summary>
		public double SearchExtraSmall = 1.0;

		/// <summary>Object pairings. To be later processed into <see cref="Tracklet"/>s.</summary>
		List<ImageDetection[][]> CandidatePairings;

		/// <summary>Checks whether a pair of detections makes reasonable sense to become a candidate object.</summary>
		bool VerifyPair(ImageDetection a, ImageDetection b)
		{
			if (a.TryFetchProperty(out PairingProperties ppa) && b.TryFetchProperty(out PairingProperties ppb))
				if (ppa.StarPolluted | ppb.StarPolluted)
					return false;

			double ErrRad = MaxLinErrorArcSec * Math.PI / 180 / 3600;

			double MsizeLD = (a.FetchProperty<ObjectSize>().PixelEllipse.SemiaxisMajor + b.FetchProperty<ObjectSize>().PixelEllipse.SemiaxisMajor) * 2;
			MsizeLD *= a.ParentImage.Transform.GetEstimatedWCSChainDerivative();
			double Mdistance = (a.Barycenter.EP ^ b.Barycenter.EP);
			double DeltaABTimeS = (b.Time.Time - a.Time.Time).TotalSeconds;
			double TExpTime = a.Time.Exposure.TotalSeconds + b.Time.Exposure.TotalSeconds;

			/* If the distance is too large for light sources too small, no need to check further */
			if ((MsizeLD + ErrRad) * DeltaABTimeS < Mdistance * TExpTime) return false;

			return true;
		}

		/// <summary>Checks whether 3 points are collinear.</summary>
		bool Line3Way(ImageDetection a, ImageDetection b, ImageDetection c)
		{
			double BSec = (b.Time.Time - a.Time.Time).TotalSeconds;
			double CSec = (c.Time.Time - a.Time.Time).TotalSeconds;
			double RAT = LineFit.ComputeResidualSqSum(new double[] { 0, BSec, CSec },
				new double[] { a.Barycenter.EP.RA, b.Barycenter.EP.RA, c.Barycenter.EP.RA });
			double DecT = LineFit.ComputeResidualSqSum(new double[] { 0, BSec, CSec },
				new double[] { a.Barycenter.EP.Dec, b.Barycenter.EP.Dec, c.Barycenter.EP.Dec });

			double ErrRad = MaxLinErrorArcSec * Math.PI / 180 / 3600;

			if (RAT + DecT > ErrRad * ErrRad) return false;
			else return true;
		}

		/// <summary>Attempts to find a tracklet given 2 image detections (from separate images).</summary>
		void AnalyzePair(ImageDetection a, ImageDetection b)
		{
			/* Figure out line vector */
			double SepSec = (b.Time.Time - a.Time.Time).TotalSeconds;

			LinearRegression.LinearRegressionParameters RAT = LinearRegression.ComputeLinearRegression(new double[] { 0, SepSec },
				new double[] { a.Barycenter.EP.RA, b.Barycenter.EP.RA });
			LinearRegression.LinearRegressionParameters DecT = LinearRegression.ComputeLinearRegression(new double[] { 0, SepSec },
				new double[] { a.Barycenter.EP.Dec, b.Barycenter.EP.Dec });

			/* Search for objects */
			List<ImageDetection[]> Dects = new List<ImageDetection[]>();
			foreach (DateTime dt in ObsTimes)
            {
                /* Compute estimated position */
                TimeSpan tsp = dt - a.Time.Time;
				double tspSeconds = tsp.TotalSeconds;
                EquatorialPoint eqp = new EquatorialPoint() { RA = RAT.Intercept + RAT.Slope * tspSeconds, Dec = DecT.Intercept + DecT.Slope * tspSeconds };
                /* Limit is given by a triangle with the maximum residuals */
                List<ImageDetection> ImDL = FindSourcesAround(a, b, dt, eqp, SearchExtraSmall);
				if (ImDL.Count == 0) ImDL = FindSourcesAround(a, b, dt, eqp, SearchExtraBig);
				Dects.Add(ImDL.ToArray());
            }

            /* If it can be found on at least 3 images, consider it a detection */
            int i, c = 0;
			for (i = 0; i < Dects.Count; i++) if (Dects[i].Length != 0) c++;
			if (c >= 3)
			{
				CandidatePairings.Add(Dects.ToArray());
				foreach (ImageDetection[] mdl in Dects) foreach (ImageDetection m in mdl) m.FetchOrCreate<PairingProperties>().IsPaired = true;
			}
		}

		/// <summary>
		/// Finds <see cref="ImageDetection"/>s around the estimated position from other detections.
		/// </summary>
		/// <param name="a">First reference detection.</param>
		/// <param name="b">Second reference detection.</param>
		/// <param name="dt">Time at which to find the new detection.</param>
		/// <param name="eqp">Estimated location of the new detection.</param>
		/// <param name="radius">Search radius around the estimated location.</param>
		/// <returns>A list of <see cref="ImageDetection"/> that match the given conditions.</returns>
        private List<ImageDetection> FindSourcesAround(ImageDetection a, ImageDetection b, DateTime dt, EquatorialPoint eqp, double radius)
        {
			double SepSec = (b.Time.Time - a.Time.Time).TotalSeconds;
			TimeSpan tsp = dt - a.Time.Time;
			double tspSeconds = tsp.TotalSeconds;

			double RadiusArcSec = tspSeconds * MaxLinErrorArcSec / SepSec + radius;
            double RadiusRad = RadiusArcSec * Math.PI / 180 / 3600;

            var ImDL = DetectionPool.Query(eqp.RA, eqp.Dec, RadiusRad);

            ImDL.RemoveAll((x) => ((x.Barycenter.EP ^ eqp) > RadiusRad) || Math.Abs((x.Time.Time - dt).TotalSeconds) > .1 || !Line3Way(a, b, x));
            return ImDL;
        }

        /// <summary>
        /// Pairs the sources into tracklets.
        /// </summary>
        /// <returns>The list of tracklets found by the algorithm.</returns>
        public override List<Tracklet> FindTracklets()
		{
			int i, j;
			CandidatePairings = new List<ImageDetection[][]>();
			for (i = 0; i < PoolList.Count; i++)
			{
				/*if (PoolList[i].TryFetchProperty(out PairingProperties pp))
					if (pp.IsPaired) continue;*/

				for (j = 0; j < PoolList.Count; j++)
				{
					if (PoolList[i].TryFetchProperty(out PairingProperties pp))
						if (pp.IsPaired)
						{
							if (PoolList[j].TryFetchProperty(out PairingProperties pp2))
								if (pp2.IsPaired) continue;
						}

					if (PoolList[i].Time.Time == PoolList[j].Time.Time)
						continue;
					if (VerifyPair(PoolList[i], PoolList[j]))
						AnalyzePair(PoolList[i], PoolList[j]);
				}
			}

			var Tracklets = CandidatePairings.Select((ImageDetection[][] x) => x.Where((y) => y.Length > 0).Select(StandardTrackletFactory.MergeStandardDetections).ToArray())
				.Select((x) => StandardTrackletFactory.CreateTracklet(x)).ToList();

			return Tracklets;
		}
	}
}
