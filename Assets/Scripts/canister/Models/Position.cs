using EdjCase.ICP.Candid.Mapping;

namespace Cosmicrafts.Cosmicrafts.Models
{
	public class Position
	{
		[CandidName("x")]
		public double X { get; set; }

		[CandidName("y")]
		public double Y { get; set; }

		public Position(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}

		public Position()
		{
		}
	}
}