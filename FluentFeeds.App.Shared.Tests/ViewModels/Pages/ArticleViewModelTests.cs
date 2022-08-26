using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using FluentFeeds.App.Shared.Models;
using FluentFeeds.App.Shared.Models.Items;
using FluentFeeds.App.Shared.Models.Navigation;
using FluentFeeds.App.Shared.Tests.Mock;
using FluentFeeds.App.Shared.ViewModels.Pages;
using FluentFeeds.Documents;
using FluentFeeds.Documents.Blocks;
using FluentFeeds.Documents.Inlines;
using FluentFeeds.Feeds.Base.Items;
using FluentFeeds.Feeds.Base.Items.Content;
using Xunit;

namespace FluentFeeds.App.Shared.Tests.ViewModels.Pages;

public class ArticleItemViewModelTests
{
	private SettingsServiceMock SettingsService { get; } = new();

	[Fact]
	public void LoadArticle()
	{
		var content = new ArticleItemContent(new RichText(new GenericBlock(new TextInline("content"))));
		var item = ItemViewModelTests.CreateItem("title", null, DateTimeOffset.Now, content);
		var viewModel = new ArticleItemViewModel(SettingsService);
		viewModel.Load(FeedNavigationRoute.ArticleItem(item, content));
		Assert.Equal("title", viewModel.Title);
		Assert.Equal(content.Body, viewModel.Content);
	}

	[Fact]
	public void UpdateDisplaySettings()
	{
		SettingsService.ContentFontFamily = FontFamily.Serif;
		SettingsService.ContentFontSize = FontSize.Small;
		var viewModel = new ArticleItemViewModel(SettingsService);
		Assert.Equal(FontFamily.Serif, viewModel.FontFamily);
		Assert.Equal(FontSize.Small, viewModel.FontSize);
		SettingsService.ContentFontFamily = FontFamily.Monospace;
		Assert.Equal(FontFamily.Monospace, viewModel.FontFamily);
		SettingsService.ContentFontSize = FontSize.Large;
		Assert.Equal(FontSize.Large, viewModel.FontSize);
	}
}
