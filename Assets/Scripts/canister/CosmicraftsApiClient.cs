#nullable enable
using EdjCase.ICP.Agent.Agents;
using EdjCase.ICP.Candid.Models;
using EdjCase.ICP.Candid;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cosmicrafts.Cosmicrafts;
using EdjCase.ICP.Agent.Responses;

namespace Cosmicrafts.Cosmicrafts
{
	public class CosmicraftsApiClient
	{
		public IAgent Agent { get; }
		public Principal CanisterId { get; }
		public CandidConverter? Converter { get; }

		public CosmicraftsApiClient(IAgent agent, Principal canisterId, CandidConverter? converter = default)
		{
			this.Agent = agent;
			this.CanisterId = canisterId;
			this.Converter = converter;
		}

		public async Task<List<Models.Entity>> ExportEntities()
		{
			CandidArg arg = CandidArg.FromCandid();
			QueryResponse response = await this.Agent.QueryAsync(this.CanisterId, "export_entities", arg);
			CandidArg reply = response.ThrowOrGetReply();
			return reply.ToObjects<List<Models.Entity>>(this.Converter);
		}

		public async Task<List<Models.GameFrame>> GetFramesSince(ulong arg0)
		{
			CandidArg arg = CandidArg.FromCandid(CandidTypedValue.FromObject(arg0, this.Converter));
			QueryResponse response = await this.Agent.QueryAsync(this.CanisterId, "get_frames_since", arg);
			CandidArg reply = response.ThrowOrGetReply();
			return reply.ToObjects<List<Models.GameFrame>>(this.Converter);
		}

		public async Task<ulong> GetLatestFrameNumber()
		{
			CandidArg arg = CandidArg.FromCandid();
			QueryResponse response = await this.Agent.QueryAsync(this.CanisterId, "get_latest_frame_number", arg);
			CandidArg reply = response.ThrowOrGetReply();
			return reply.ToObjects<ulong>(this.Converter);
		}

		public async Task<OptionalValue<Models.Player>> GetPlayer()
		{
			CandidArg arg = CandidArg.FromCandid();
			QueryResponse response = await this.Agent.QueryAsync(this.CanisterId, "get_player", arg);
			CandidArg reply = response.ThrowOrGetReply();
			return reply.ToObjects<OptionalValue<Models.Player>>(this.Converter);
		}

		public async Task<Models.Result> MoveEntity(ulong arg0, double arg1, double arg2)
		{
			CandidArg arg = CandidArg.FromCandid(CandidTypedValue.FromObject(arg0, this.Converter), CandidTypedValue.FromObject(arg1, this.Converter), CandidTypedValue.FromObject(arg2, this.Converter));
			CandidArg reply = await this.Agent.CallAsync(this.CanisterId, "move_entity", arg);
			return reply.ToObjects<Models.Result>(this.Converter);
		}

		public async Task<Models.Result1> Signup(string arg0, uint arg1, OptionalValue<string> arg2, string arg3)
		{
			CandidArg arg = CandidArg.FromCandid(CandidTypedValue.FromObject(arg0, this.Converter), CandidTypedValue.FromObject(arg1, this.Converter), CandidTypedValue.FromObject(arg2, this.Converter), CandidTypedValue.FromObject(arg3, this.Converter));
			CandidArg reply = await this.Agent.CallAsync(this.CanisterId, "signup", arg);
			return reply.ToObjects<Models.Result1>(this.Converter);
		}

		public async Task<ulong> SpawnEntity(Models.EntityType arg0)
		{
			CandidArg arg = CandidArg.FromCandid(CandidTypedValue.FromObject(arg0, this.Converter));
			CandidArg reply = await this.Agent.CallAsync(this.CanisterId, "spawn_entity", arg);
			return reply.ToObjects<ulong>(this.Converter);
		}

		public async Task<List<ulong>> SpawnMultipleEntities(Models.EntityType arg0, ulong arg1)
		{
			CandidArg arg = CandidArg.FromCandid(CandidTypedValue.FromObject(arg0, this.Converter), CandidTypedValue.FromObject(arg1, this.Converter));
			CandidArg reply = await this.Agent.CallAsync(this.CanisterId, "spawn_multiple_entities", arg);
			return reply.ToObjects<List<ulong>>(this.Converter);
		}

		public async Task StartGameLoop()
		{
			CandidArg arg = CandidArg.FromCandid();
			await this.Agent.CallAsync(this.CanisterId, "start_game_loop", arg);
		}
	}
}