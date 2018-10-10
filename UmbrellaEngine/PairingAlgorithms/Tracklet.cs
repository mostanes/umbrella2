using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Umbrella2
{
	/// <summary>
	/// Represents a tracklet.
	/// </summary>
	/// <remarks>
	/// The interface is volatile for now.
	/// </remarks>
	public class Tracklet
	{
		MedianDetection[][] Detections;

		public Tracklet(MedianDetection[][] Detections)
		{
			this.Detections = Detections;
		}
	}
}
