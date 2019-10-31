using System;
using System.Linq;
using System.Windows.Forms;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2.Visualizer.Winforms
{
	public partial class TrackletOutput
	{
		/// <summary>
		/// Toggle blinking of images.
		/// </summary>
		private void BlinkOnDetection()
		{
			int BlinkSpeed = 500;
			BlinkID = 0;
			if (BlinkTimer == null)
			{
				BlinkTimer = new Timer() { Enabled = false, Interval = BlinkSpeed };
				BlinkTimer.Tick += (sender, e) => BlinkNext();
			}
			BlinkTimer.Enabled = !BlinkTimer.Enabled;
		}

		/// <summary>Changes to the next image to blink.</summary>
		private void BlinkNext()
		{
			var ImageS = (ImageSet)ImageSet[BlinkID].GetProperty<ImageSource>();
			Images = ImageS.FetchVariants();
			BlinkID = (BlinkID + 1) % ImageSet.Count;
			UpdateImage();
		}

		/// <summary>Filters tracklets dependent on a <paramref name="Detection"/> from the list.</summary>
		private void Filter(ImageDetection Detection)
		{
			for (int i = 0; i < Tracklets.Count; i++)
				if (Tracklets[i].Detections.Contains(Detection) && Tracklets[i].Detections.Length == 3)
				{ Tracklets.RemoveAt(i); i--; }

			RefreshTrackletList();
		}

		/// <summary>Represents a predicate for filtering <see cref="ImageDetection"/>s.</summary>
		delegate bool DetectionFilteringCondition(ImageDetection Filterer, ImageDetection Filtered, double Parameter);

		/// <summary>
		/// Filters detections matching a condition.
		/// </summary>
		/// <param name="Detection">Model detection.</param>
		/// <param name="Filter">Filtering predicate.</param>
		/// <param name="Parameter">Predicate parameter.</param>
		private void FilterByDetection(ImageDetection Detection, DetectionFilteringCondition Filter, double Parameter)
		{
			for (int i = 0; i < Tracklets.Count; i++)
				if (Tracklets[i].Detections.Any((x) => Filter(Detection, x, Parameter)))
				{
					if (Tracklets[i].Detections.Length == 3)
					{ Tracklets.RemoveAt(i); i--; }
					else
					{
						var r = Tracklets[i].Detections.Where((x) => !Filter(Detection, x, Parameter)).ToArray();
						if (r.Length < 3) Tracklets.RemoveAt(i);
					}
				}

			RefreshTrackletList();
		}

		/// <summary>Filters all detections within a <paramref name="RadRadius"/> radius from the <paramref name="Detection"/>.</summary>
		private static bool ConditionRadius(ImageDetection Detection, ImageDetection x, double RadRadius) => (x.Barycenter.EP ^ Detection.Barycenter.EP) < RadRadius;
		/// <summary>Filters all detection with X coordinates within <paramref name="XDelta"/> from the <paramref name="Detection"/>.</summary>
		private static bool ConditionX(ImageDetection Detection, ImageDetection x, double XDelta) => Math.Abs(x.Barycenter.PP.X - Detection.Barycenter.PP.X) < XDelta;
		/// <summary>Filters all detection with Y coordinates within <paramref name="YDelta"/> from the <paramref name="Detection"/>.</summary>
		private static bool ConditionY(ImageDetection Detection, ImageDetection x, double YDelta) => Math.Abs(x.Barycenter.PP.Y - Detection.Barycenter.PP.Y) < YDelta;
	}
}
