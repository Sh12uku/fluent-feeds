﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Windows.Input;
using FluentFeeds.App.Shared.EventArgs;
using FluentFeeds.App.Shared.Helpers;
using FluentFeeds.App.Shared.Models.Feeds;
using FluentFeeds.App.Shared.Models.Navigation;
using FluentFeeds.App.Shared.Services;
using FluentFeeds.App.Shared.ViewModels.Items.Navigation;
using FluentFeeds.App.Shared.ViewModels.Modals;
using FluentFeeds.Common;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace FluentFeeds.App.Shared.ViewModels.Pages;

/// <summary>
/// View model for the main page managing navigation between feeds and other pages.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
	public MainViewModel(IFeedService feedService, IModalService modalService)
	{
		_feedService = feedService;
		_modalService = modalService;
		
		_goBackCommand = new RelayCommand(HandleGoBackCommand, () => _backStack.Count > 1);
		_settingsItem =
			new NavigationItemViewModel(MainNavigationRoute.Settings, isExpandable: false, "Settings", Symbol.Settings);
		_footerItems.Add(_settingsItem);
		var overviewItem = new FeedNavigationItemViewModel(
			modalService, feedService.OverviewFeed, null, _feedItemRegistry);
		_feedItemRegistry.Add(feedService.OverviewFeed, overviewItem);
		_feedItems.Add(overviewItem);
		_feedItemTransformer = ObservableCollectionTransformer.CreateCached(
			feedService.ProviderFeeds, _feedItems, CreateRootFeedViewModel, _feedItemRegistry);
		_currentRoute = MainNavigationRoute.Feed(feedService.OverviewFeed);
		_backStack.Add(_currentRoute);
		_selectedItem = overviewItem;

		FooterItems = new ReadOnlyObservableCollection<NavigationItemViewModel>(_footerItems);
		FeedItems = new ReadOnlyObservableCollection<NavigationItemViewModel>(_feedItems);

		InitializeFeeds();
	}

	private async void InitializeFeeds()
	{
		try
		{
			await _feedService.InitializeAsync();
		}
		catch (Exception)
		{
			_modalService.Show(
				new ErrorViewModel("A database error occurred", "Fluent Feeds was unable to initialize its database."));
		}
	}

	private NavigationItemViewModel CreateRootFeedViewModel(IFeedView rootFeed)
	{
		if (rootFeed.Storage != null)
		{
			rootFeed.Storage.FeedsDeleted += HandleFeedsDeleted;
		}
		return new FeedNavigationItemViewModel(_modalService, rootFeed, rootFeed, _feedItemRegistry);
	}

	/// <summary>
	/// Go back to the previous page/feed.
	/// </summary>
	public ICommand GoBackCommand => _goBackCommand;

	/// <summary>
	/// The currently active navigation route.
	/// </summary>
	public MainNavigationRoute CurrentRoute
	{
		get => _currentRoute;
		private set => SetProperty(ref _currentRoute, value);
	}

	/// <summary>
	/// View model of the navigation item currently selected.
	/// </summary>
	public NavigationItemViewModel? SelectedItem
	{
		get => _selectedItem;
		set
		{
			if (!SetProperty(ref _selectedItem, value) || _isChangingSelection || value == null)
				// Item is not actually selectable or the selection has already changed.
				return;

			var newRoute = value.Destination;
			_backStack.Add(newRoute);
			_goBackCommand.NotifyCanExecuteChanged();
			CurrentRoute = newRoute;
		}
	}

	/// <summary>
	/// Observable collection containing items for all available feeds.
	/// </summary>
	public ReadOnlyObservableCollection<NavigationItemViewModel> FeedItems { get; }

	/// <summary>
	/// Observable collection containing items displayed at the bottom of the navigation menu.
	/// </summary>
	public ReadOnlyObservableCollection<NavigationItemViewModel> FooterItems { get; }

	private void HandleGoBackCommand()
	{
		_backStack.RemoveAt(_backStack.Count - 1);
		HandleBackStackChanged();
	}

	private void HandleBackStackChanged()
	{
		var newRoute = _backStack[^1];
		_goBackCommand.NotifyCanExecuteChanged();
		_isChangingSelection = true;
		SelectedItem =
			newRoute.Type switch
			{
				MainNavigationRouteType.Settings => _settingsItem,
				MainNavigationRouteType.Feed => _feedItemRegistry.GetValueOrDefault(newRoute.SelectedFeed!),
				_ => null
			};
		_isChangingSelection = false;
		CurrentRoute = newRoute;
	}

	private void HandleFeedsDeleted(object? sender, FeedsDeletedEventArgs e)
	{
		var feeds = e.Feeds.ToImmutableHashSet();

		// Clean up cache
		foreach (var feed in feeds)
		{
			_feedItemRegistry.Remove(feed);
		}

		// Remove feeds from the back stack
		bool changed = false;
		MainNavigationRoute? previousRoute = null;
		for (var i = _backStack.Count - 1; i >= 0; --i)
		{
			var route = _backStack[i];
			if (route == previousRoute || (route.SelectedFeed != null && feeds.Contains(route.SelectedFeed)))
			{
				_backStack.RemoveAt(i);
				changed = true;
			}
			else
			{
				previousRoute = route;
			}
		}

		if (changed)
		{
			HandleBackStackChanged();
		}
	}

	private readonly IFeedService _feedService;
	private readonly IModalService _modalService;
	private readonly List<MainNavigationRoute> _backStack = new();
	private readonly RelayCommand _goBackCommand;
	private readonly NavigationItemViewModel _settingsItem;
	private readonly ObservableCollection<NavigationItemViewModel> _feedItems = new();
	private readonly ObservableCollection<NavigationItemViewModel> _footerItems = new();
	private readonly ObservableCollectionTransformer<IFeedView, NavigationItemViewModel> _feedItemTransformer;
	private readonly Dictionary<IFeedView, NavigationItemViewModel> _feedItemRegistry = new();
	private MainNavigationRoute _currentRoute;
	private NavigationItemViewModel? _selectedItem;
	private bool _isChangingSelection;
}
