using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;
using FluentFeeds.Shared.RichText.Helpers;
using FluentFeeds.Shared.RichText.Json;

namespace FluentFeeds.Shared.RichText.Blocks.Table;

/// <summary>
/// Row in a table.
/// <seealso cref="TableBlock"/>
/// </summary>
[JsonConverter(typeof(TableRowJsonConverter))]
public sealed class TableRow
{
	/// <summary>
	/// Create a new default-constructed table row.
	/// </summary>
	public TableRow()
	{
		Cells = ImmutableArray<TableCell>.Empty;
	}

	/// <summary>
	/// Create a new table row from a list of cells.
	/// </summary>
	public TableRow(params TableCell[] cells)
	{
		Cells = ImmutableArray.Create(cells);
	}
	
	/// <summary>
	/// The table row's cells.
	/// </summary>
	public ImmutableArray<TableCell> Cells { get; init; }
	
	public override string ToString() => $"TableRow {{ Cells = {Cells.SequenceString()} }}";

	public bool Equals(TableRow? other)
	{
		if (ReferenceEquals(this, other))
			return true;
		if (ReferenceEquals(null, other))
			return false;
		return Cells.SequenceEqual(other.Cells);
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(this, obj))
			return true;
		return obj is TableRow other && Equals(other);
	}

	public override int GetHashCode()
	{
		return Cells.SequenceHashCode();
	}

	public static bool operator ==(TableRow? lhs, TableRow? rhs) => lhs?.Equals(rhs) ?? ReferenceEquals(rhs, null);
	public static bool operator !=(TableRow? lhs, TableRow? rhs) => !(lhs == rhs);
}
