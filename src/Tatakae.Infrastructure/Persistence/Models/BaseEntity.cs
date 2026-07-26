using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tatakae.Infrastructure.Persistence.Models;

public interface IBaseEntity
{
    DateTime InsertTime { get; set; }
    DateTime? UpdateTime { get; set; }
    bool IsRemoved { get; set; }
    DateTime? RemoveTime { get; set; }
}

public static class SoftDeleteExtensions
{
    public static void MarkAsRemoved(this IBaseEntity entity, DateTime? removedAt = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var timestamp = removedAt ?? DateTime.Now;
        entity.IsRemoved = true;
        entity.RemoveTime ??= timestamp;
        entity.UpdateTime = timestamp;
    }

    public static void Restore(this IBaseEntity entity, DateTime? restoredAt = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.IsRemoved = false;
        entity.RemoveTime = null;
        entity.UpdateTime = restoredAt ?? DateTime.Now;
    }
}

/// <summary>
/// Base entity for all database models. Keeps shared audit/soft-delete fields in one place.
/// </summary>
public abstract class BaseEntity<TKey> : IBaseEntity
{
    public TKey Id { get; set; } = default!;
    public DateTime InsertTime { get; set; } = DateTime.Now;
    public DateTime? UpdateTime { get; set; }
    public bool IsRemoved { get; set; } = false;
    public DateTime? RemoveTime { get; set; }

    [NotMapped]
    public DateTime? RemovedAt
    {
        get => RemoveTime;
        set => RemoveTime = value;
    }
}

public abstract class BaseEntity : BaseEntity<long>
{
}
