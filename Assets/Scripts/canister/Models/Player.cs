using EdjCase.ICP.Candid.Mapping;
using EdjCase.ICP.Candid.Models;
using System.Collections.Generic;

namespace Cosmicrafts.Cosmicrafts.Models
{
	public class Player
	{
		[CandidName("id")]
		public Principal Id { get; set; }

		[CandidName("elo")]
		public double Elo { get; set; }

		[CandidName("title")]
		public string Title { get; set; }

		[CandidName("registration_date")]
		public ulong RegistrationDate { get; set; }

		[CandidName("username")]
		public string Username { get; set; }

		[CandidName("description")]
		public string Description { get; set; }

		[CandidName("level")]
		public uint Level { get; set; }

		[CandidName("language")]
		public string Language { get; set; }

		[CandidName("associated_entities")]
		public List<Principal> AssociatedEntities { get; set; }

		[CandidName("avatar")]
		public ulong Avatar { get; set; }

		public Player(Principal id, double elo, string title, ulong registrationDate, string username, string description, uint level, string language, List<Principal> associatedEntities, ulong avatar)
		{
			this.Id = id;
			this.Elo = elo;
			this.Title = title;
			this.RegistrationDate = registrationDate;
			this.Username = username;
			this.Description = description;
			this.Level = level;
			this.Language = language;
			this.AssociatedEntities = associatedEntities;
			this.Avatar = avatar;
		}

		public Player()
		{
		}
	}
}