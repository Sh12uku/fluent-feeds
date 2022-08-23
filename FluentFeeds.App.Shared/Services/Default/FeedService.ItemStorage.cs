﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentFeeds.App.Shared.Models.Database;
using FluentFeeds.Feeds.Base;
using FluentFeeds.Feeds.Base.EventArgs;
using FluentFeeds.Feeds.Base.Items;
using FluentFeeds.Feeds.Base.Items.Content;
using FluentFeeds.Feeds.Base.Items.ContentLoaders;
using FluentFeeds.Feeds.Base.Storage;
using Microsoft.EntityFrameworkCore;

namespace FluentFeeds.App.Shared.Services.Default;

public partial class FeedService
{
	private sealed class ItemStorage : IItemStorage
	{
		public ItemStorage(IDatabaseService databaseService, FeedProvider provider, Guid identifier)
		{
			_databaseService = databaseService;
			_provider = provider;
			_identifier = identifier;

			_initialize = new Lazy<Task>(InitializeAsync);
		}

		public event EventHandler<ItemsDeletedEventArgs>? ItemsDeleted;

		/// <summary>
		/// Add an item to the cache.
		/// </summary>
		private void RegisterItem(StoredItem item)
		{
			_items.Add(item.Identifier, item);
			if (item.Url != null)
			{
				_urlItems.Add(item.Url, item);
			}
		}
		
		/// <summary>
		/// Store a collection of items in the database.
		/// </summary>
		private async Task StoreItemsAsync(AppDbContext database, IReadOnlyCollection<StoredItem> items)
		{
			var dbItems = new List<ItemDb> { Capacity = items.Count };
			foreach (var item in items)
			{
				dbItems.Add(
					new ItemDb
					{ 
						Identifier = item.Identifier, 
						ProviderIdentifier = _provider.Metadata.Identifier,
						StorageIdentifier = _identifier,
						Url = item.Url,
						ContentUrl = item.ContentUrl,
						PublishedTimestamp = item.PublishedTimestamp,
						ModifiedTimestamp = item.ModifiedTimestamp,
						Title = item.Title,
						Author = item.Author,
						Summary = item.Summary,
						Content = await _provider.StoreContentAsync(item.ContentLoader),
						IsRead = item.IsRead
					});
			}
			
			await database.Items.AddRangeAsync(dbItems);
		}

		/// <summary>
		/// Update an item in the database.
		/// </summary>
		private async Task UpdateItemAsync(AppDbContext database, Guid identifier, IReadOnlyItem updatedItem)
		{
			var dbItem = await database.Items.Where(i => i.Identifier == identifier).FirstAsync();
			dbItem.ContentUrl = updatedItem.ContentUrl;
			dbItem.ModifiedTimestamp = updatedItem.ModifiedTimestamp;
			dbItem.Title = updatedItem.Title;
			dbItem.Author = updatedItem.Author;
			dbItem.Summary = updatedItem.Summary;
			dbItem.Content = await _provider.StoreContentAsync(updatedItem.ContentLoader);
		}

		/// <summary>
		/// Load all items in this storage from the database into stored item models.
		/// </summary>
		private async Task<List<StoredItem>> LoadItemsAsync(AppDbContext database)
		{
			var providerIdentifier = _provider.Metadata.Identifier;
			var dbItems = await database.Items
				.Where(i => i.ProviderIdentifier == providerIdentifier && i.StorageIdentifier == _identifier)
				.Select(i =>
					new
					{
						i.Identifier,
						i.Url,
						i.ContentUrl,
						i.PublishedTimestamp,
						i.ModifiedTimestamp,
						i.Title,
						i.Author,
						i.Summary,
						i.IsRead
					})
				.ToListAsync();
			return dbItems
				.Select(item => new StoredItem(
					identifier: item.Identifier,
					storage: this,
					url: item.Url,
					contentUrl: item.ContentUrl,
					publishedTimestamp: item.PublishedTimestamp,
					modifiedTimestamp: item.ModifiedTimestamp,
					title: item.Title,
					author: item.Author,
					summary: item.Summary,
					contentLoader: new ItemContentLoader(_databaseService, _provider, item.Identifier),
					isRead: item.IsRead))
				.ToList();
		}

		private async Task InitializeAsync()
		{
			foreach (var item in await _databaseService.ExecuteAsync(LoadItemsAsync))
			{
				RegisterItem(item);
			}
		}

		public async Task<IEnumerable<IReadOnlyStoredItem>> GetItemsAsync()
		{
			await _initialize.Value;
			return _items.Values;
		}

		public async Task<IEnumerable<IReadOnlyStoredItem>> AddItemsAsync(IEnumerable<IReadOnlyItem> items)
		{
			await _initialize.Value;
			
			// Determine added and updated items, also initializing the result list.
			var added = new List<StoredItem>();
			var updated = new List<(StoredItem, IReadOnlyItem)>();
			var result = new List<IReadOnlyStoredItem>();
			foreach (var item in items)
			{
				// Check if there is an item in this storage with this URL.
				if (item.Url != null && _urlItems.TryGetValue(item.Url, out var existing))
				{
					// Check if the item needs to be updated.
					if (existing.ModifiedTimestamp < item.ModifiedTimestamp)
					{
						updated.Add((existing, item));
					}
					result.Add(existing);
				}
				else
				{
					// This is a new item.
					var storedItem = new StoredItem(item, Guid.NewGuid(), this, isRead: false);
					added.Add(storedItem);
					result.Add(storedItem);
				}
			}

			// Update database
			await _databaseService.ExecuteAsync(
				async database =>
				{
					foreach (var (item, updatedItem) in updated)
					{
						await UpdateItemAsync(database, item.Identifier, updatedItem);
					}
					await StoreItemsAsync(database, added);
					await database.SaveChangesAsync();
				});
			
			// Update local copy
			foreach (var (item, updatedItem) in updated)
			{
				item.ContentUrl = updatedItem.ContentUrl;
				item.ModifiedTimestamp = updatedItem.ModifiedTimestamp;
				item.Title = updatedItem.Title;
				item.Author = updatedItem.Author;
				item.Summary = updatedItem.Summary;
				item.ContentLoader = updatedItem.ContentLoader;
			}
			foreach (var item in added)
			{
				RegisterItem(item);
			}

			return result;
		}

		public async Task<IReadOnlyStoredItem> SetItemReadAsync(Guid identifier, bool isRead)
		{
			var item = _items[identifier];
			
			// Update database
			await _databaseService.ExecuteAsync(
				async database =>
				{
					var dbItem = await database.Items.Where(i => i.Identifier == identifier).FirstAsync();
					dbItem.IsRead = isRead;
					await database.SaveChangesAsync();
				});
			
			// Update local copy
			item.IsRead = isRead;
			return item;
		}

		public async Task DeleteItemsAsync(IReadOnlyCollection<Guid> identifiers)
		{
			// Update database
			await _databaseService.ExecuteAsync(
				async database =>
				{
					foreach (var identifier in identifiers)
					{
						var dbItem = await database.Items.Where(i => i.Identifier == identifier).FirstAsync();
						database.Remove(dbItem);
					}
					await database.SaveChangesAsync();
				});
			
			// Update local copy
			var items = new List<IReadOnlyStoredItem> { Capacity = identifiers.Count };
			foreach (var identifier in identifiers)
			{
				if (_items.Remove(identifier, out var item) && item.Url != null)
				{
					_urlItems.Remove(item.Url);
					items.Add(item);
				}
			}
			
			ItemsDeleted?.Invoke(this, new ItemsDeletedEventArgs(items));
		}

		private readonly IDatabaseService _databaseService;
		private readonly FeedProvider _provider;
		private readonly Guid _identifier;
		private readonly Dictionary<Guid, StoredItem> _items = new();
		private readonly Dictionary<Uri, StoredItem> _urlItems = new();
		private readonly Lazy<Task> _initialize;
	}

	/// <summary>
	/// Content loader which lazily loads the serialized content from the database and then acts as a proxy for the
	/// deserialized loader.
	/// </summary>
	private sealed class ItemContentLoader : IItemContentLoader
	{
		public ItemContentLoader(IDatabaseService databaseService, FeedProvider provider, Guid identifier)
		{
			_databaseService = databaseService;
			_provider = provider;
			_identifier = identifier;
			_fetchFromDatabase = new Lazy<Task<IItemContentLoader>>(FetchFromDatabaseAsync, isThreadSafe: false);
		}
		
		private Task<IItemContentLoader> FetchFromDatabaseAsync()
		{
			return _databaseService.ExecuteAsync(
				async database =>
				{
					var serialized = await database.Items
						.Where(i => i.Identifier == _identifier)
						.Select(i => i.Content)
						.FirstAsync();
					return await _provider.LoadContentAsync(serialized);
				});
		}
		
		public Task<ItemContent> LoadAsync(bool reload = false)
		{
			return Task.Run(
				async () =>
				{
					var loader = await _fetchFromDatabase.Value;
					return await loader.LoadAsync(reload);
				});
		}

		private readonly IDatabaseService _databaseService;
		private readonly FeedProvider _provider;
		private readonly Guid _identifier;
		private readonly Lazy<Task<IItemContentLoader>> _fetchFromDatabase;
	}
}
