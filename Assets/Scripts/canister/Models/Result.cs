#nullable enable
using EdjCase.ICP.Candid.Mapping;
using Cosmicrafts.Cosmicrafts.Models;
using System;

namespace Cosmicrafts.Cosmicrafts.Models
{
	[Variant]
	public class Result
	{
		[VariantTagProperty]
		public ResultTag Tag { get; set; }

		[VariantValueProperty]
		public object? Value { get; set; }

		public Result(ResultTag tag, object? value)
		{
			this.Tag = tag;
			this.Value = value;
		}

		protected Result()
		{
		}

		public static Result Ok()
		{
			return new Result(ResultTag.Ok, null);
		}

		public static Result Err(string info)
		{
			return new Result(ResultTag.Err, info);
		}

		public string AsErr()
		{
			this.ValidateTag(ResultTag.Err);
			return (string)this.Value!;
		}

		private void ValidateTag(ResultTag tag)
		{
			if (!this.Tag.Equals(tag))
			{
				throw new InvalidOperationException($"Cannot cast '{this.Tag}' to type '{tag}'");
			}
		}
	}

	public enum ResultTag
	{
		Ok,
		Err
	}
}