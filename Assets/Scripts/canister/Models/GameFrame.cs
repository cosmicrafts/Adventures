using EdjCase.ICP.Candid.Mapping;
using System.Collections.Generic;
using Cosmicrafts.Cosmicrafts.Models;

namespace Cosmicrafts.Cosmicrafts.Models
{
	public class GameFrame
	{
		[CandidName("entities")]
		public List<Entity> Entities { get; set; }

		[CandidName("timestamp")]
		public ulong Timestamp { get; set; }

		[CandidName("frame_number")]
		public ulong FrameNumber { get; set; }

		public GameFrame(List<Entity> entities, ulong timestamp, ulong frameNumber)
		{
			this.Entities = entities;
			this.Timestamp = timestamp;
			this.FrameNumber = frameNumber;
		}

		public GameFrame()
		{
		}
	}
}