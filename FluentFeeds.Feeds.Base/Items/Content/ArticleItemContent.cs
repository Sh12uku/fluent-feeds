using System;
using FluentFeeds.Documents;

namespace FluentFeeds.Feeds.Base.Items.Content;

/// <summary>
/// <para>Content of an article item.</para>
///
/// <para>This object only stores a rich text object hosting the article's body.</para>
/// </summary>
public sealed class ArticleItemContent : ItemContent
{
	public ArticleItemContent(RichText body, bool isReloadable = false) : base(isReloadable)
	{
		Body = body;
	}
	
	public override void Accept(IItemContentVisitor visitor) => visitor.Visit(this);

	public override ItemContentType Type => ItemContentType.Article;
	
	/// <summary>
	/// Body of the article.
	/// </summary>
	public RichText Body { get; }
	
	public override bool Equals(ItemContent? other)
	{
		if (ReferenceEquals(this, other))
			return true;
		if (!base.Equals(other))
			return false;
		return other is ArticleItemContent casted && Body == casted.Body;
	}
	
	public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Body);
	
	public override string ToString() => $"ArticleItemContent {{ Body = {Body} }}";
}
