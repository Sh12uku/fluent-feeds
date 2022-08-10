using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using FluentFeeds.Feeds.Base.Items;

namespace FluentFeeds.Feeds.Base;

/// <summary>
/// <para>A feed which manages a set of items and can be synchronized with a remote server.</para>
///
/// <para>The items in the feed are not sorted. This is to allow for easy chaining of feeds without needing to keep
/// track of the order. The set is sorted and converted to a list only in the final stage.</para>
/// </summary>
public abstract class Feed
{
	/// <summary>
	/// Event called when <see cref="Items"/> has been updated. This event is usually raised after calling either
	/// <see cref="LoadAsync"/> or <see cref="SynchronizeAsync"/>.
	/// </summary>
	public event EventHandler<EventArgs>? ItemsUpdated;

	/// <summary>
	/// Event called when <see cref="Metadata"/> has been updated.
	/// </summary>
	public event EventHandler<EventArgs>? MetadataUpdated;

	/// <summary>
	/// Current snapshot of items provided by the feed.
	/// </summary>
	public ImmutableHashSet<IReadOnlyStoredItem> Items
	{
		get => _items;
		protected set
		{
			_items = value;
			ItemsUpdated?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// Metadata for the feed.
	/// </summary>
	public FeedMetadata? Metadata
	{
		get => _metadata;
		protected set
		{
			_metadata = value;
			MetadataUpdated?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// <para>Asynchronously load the initial selection of items. The result might be out of date and need to be
	/// synchronized.</para>
	///
	/// <para>This class ensures that there is only one load request for the whole lifetime of the feed. Subsequent
	/// calls either return the same task or a completed task.</para>
	/// </summary>
	public Task LoadAsync()
	{
		if (_isLoaded)
			return Task.CompletedTask;
		
		_loadTask ??= LoadAsyncCore();
		return _loadTask;
	}

	/// <summary>
	/// <para>Fetch an up to date list of items from a remote server and update <see cref="Items"/>.</para>
	///
	/// <para>This class ensures that there is only one synchronization operation at a time.</para>
	/// </summary>
	public Task SynchronizeAsync()
	{
		if (_synchronizeTask == null || !_isSynchronizing)
		{
			_isSynchronizing = true;
			_synchronizeTask = SynchronizeAsyncCore();
		}
		
		return _synchronizeTask;
	}

	/// <summary>
	/// Asynchronously return the initial selection of items which might be out of date.
	/// </summary>
	protected abstract Task<IEnumerable<IReadOnlyStoredItem>> DoLoadAsync();
	
	/// <summary>
	/// Return a list of up to date items fetched from a remote server. It is ensured that <see cref="DoLoadAsync"/> has
	/// been called before this method.
	/// </summary>
	protected abstract Task<IEnumerable<IReadOnlyStoredItem>> DoSynchronizeAsync();

	private async Task LoadAsyncCore()
	{
		Items = (await DoLoadAsync()).ToImmutableHashSet();
		
		_isLoaded = true;
	}

	private async Task SynchronizeAsyncCore()
	{
		await LoadAsync();

		Items = (await DoSynchronizeAsync()).ToImmutableHashSet();

		_isSynchronizing = false;
	}

	private ImmutableHashSet<IReadOnlyStoredItem> _items = ImmutableHashSet<IReadOnlyStoredItem>.Empty;
	private FeedMetadata? _metadata;
	private bool _isLoaded;
	private bool _isSynchronizing;
	private Task? _loadTask;
	private Task? _synchronizeTask;
}