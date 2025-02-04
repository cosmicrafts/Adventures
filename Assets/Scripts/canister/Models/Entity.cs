using EdjCase.ICP.Candid.Mapping;
using Cosmicrafts.Cosmicrafts.Models;
using EdjCase.ICP.Candid.Models;

namespace Cosmicrafts.Cosmicrafts.Models
{
	public class Entity
	{
		[CandidName("id")]
		public ulong Id { get; set; }

		[CandidName("speed")]
		public double Speed { get; set; }

		[CandidName("entity_type")]
		public EntityType EntityType { get; set; }

		[CandidName("position")]
		public Position Position { get; set; }

		[CandidName("target_position")]
		public OptionalValue<Position> TargetPosition { get; set; }

		public Entity(ulong id, double speed, EntityType entityType, Position position, OptionalValue<Position> targetPosition)
		{
			this.Id = id;
			this.Speed = speed;
			this.EntityType = entityType;
			this.Position = position;
			this.TargetPosition = targetPosition;
		}

		public Entity()
		{
		}
	}
}