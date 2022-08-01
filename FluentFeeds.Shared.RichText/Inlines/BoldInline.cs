using System.Linq;
using System.Text.Json.Serialization;
using FluentFeeds.Shared.RichText.Helpers;
using FluentFeeds.Shared.RichText.Json;

namespace FluentFeeds.Shared.RichText.Inlines;

/// <summary>
/// An inline for making text bold.
/// </summary>
[JsonConverter(typeof(InlineJsonConverter<BoldInline>))]
public sealed class BoldInline : SpanInline
{
	/// <summary>
	/// Create a new default-constructed bold inline.
	/// </summary>
	public BoldInline()
	{
	}

	/// <summary>
	/// Create a new bold inline with the provided child inline elements.
	/// </summary>
	public BoldInline(params Inline[] inlines) : base(inlines)
	{
	}

	public override InlineType Type => InlineType.Bold;

	public override void Accept(IInlineVisitor visitor) => visitor.Visit(this);
	public override string ToString() => $"BoldInline {{ Inlines = {Inlines.SequenceString()} }}";

	public override bool Equals(Inline? other)
	{
		if (ReferenceEquals(this, other))
			return true;
		return base.Equals(other) && other is BoldInline;
	}
}
