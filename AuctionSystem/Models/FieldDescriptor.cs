using System;
using System.Linq;

namespace AuctionSystem.Models;

public enum FieldKind
{
    Text,
    Multiline,
    Int,
    Decimal,
    Date,
    Time,
    Bool,
    Combo
}

public sealed class FieldDescriptor
{
    public required string Column { get; init; }
    public required string Header { get; init; }
    public FieldKind Kind { get; init; } = FieldKind.Text;
    public bool Required { get; init; }
    public bool Editable { get; init; } = true;
    public bool Identity { get; init; }
    public bool ShowInGrid { get; init; } = true;
    public double GridWidth { get; init; } = 130;
    public bool IsPassword { get; init; }
    public bool Auto { get; init; }
    public string? ComboSql { get; init; }
    public string? DefaultValue { get; init; }
}

public sealed class SectionDescriptor
{
    public required string Title { get; init; }
    public required string Table { get; init; }
    public required string IdColumn { get; init; }
    public required FieldDescriptor[] Fields { get; init; }
    public string OrderBy { get; init; } = string.Empty;
    public bool CanAdd { get; init; } = true;
    public bool CanEdit { get; init; } = true;
    public bool CanDelete { get; init; } = true;
    public string? RowFilter { get; init; }
    public string[] SearchColumns { get; init; } = Array.Empty<string>();

    public FieldDescriptor? IdField => Fields.FirstOrDefault(f => f.Column == IdColumn);
}

public sealed class ComboItem
{
    public object Value { get; init; } = 0;
    public string Display { get; init; } = string.Empty;
}
