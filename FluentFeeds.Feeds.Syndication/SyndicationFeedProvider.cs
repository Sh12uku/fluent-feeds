using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentFeeds.Common;
using FluentFeeds.Feeds.Base;
using FluentFeeds.Feeds.Base.Nodes;
using FluentFeeds.Feeds.Base.Storage;
using FluentFeeds.Feeds.Syndication.Download;

namespace FluentFeeds.Feeds.Syndication;

/// <summary>
/// Feed provider implementation for syndication feeds.
/// </summary>
public sealed class SyndicationFeedProvider : FeedProvider
{
	private record FeedDescription(Guid Identifier, Uri Url, string? Name, string? Author, string? Description);

	public SyndicationFeedProvider()
		: base(new FeedProviderMetadata(
			Identifier: Guid.Parse("8d468d92-4b69-41e3-8fac-28c99fc923a2"),
			Name: "Syndication Feed Provider",
			Description: "Feed provider for RSS and Atom feeds."))
	{
		UrlFeedFactory = new SyndicationUrlFeedFactory();
	}

	public override IReadOnlyFeedNode CreateInitialTree(IFeedStorage feedStorage)
	{
		return FeedNode.Group(title: "RSS/Atom feeds", symbol: Symbol.Feed, isUserCustomizable: true);
	}

	public override Task<Feed> LoadFeedAsync(IFeedStorage feedStorage, string serialized)
	{
		var description = JsonSerializer.Deserialize<FeedDescription>(serialized) ?? throw new JsonException();
		var downloader = new FeedDownloader(description.Url);
		return Task.FromResult<Feed>(new SyndicationFeed(
			downloader, feedStorage, description.Identifier, description.Url,
			new FeedMetadata
			{
				Name = description.Name,
				Author = description.Author,
				Description = description.Description,
				Symbol = Symbol.Web
			}));
	}

	public override Task<string> StoreFeedAsync(Feed feed)
	{
		var syndicationFeed = (SyndicationFeed)feed;
		var description = new FeedDescription(
			Identifier: syndicationFeed.Identifier,
			Url: syndicationFeed.Url,
			Name: syndicationFeed.Metadata.Name,
			Author: syndicationFeed.Metadata.Author,
			Description: syndicationFeed.Metadata.Description);
		return Task.FromResult(JsonSerializer.Serialize(description));
	}
}