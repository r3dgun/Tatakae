namespace Tatakae.Infrastructure.Inventory;

public sealed class InventoryReservationOptions
{
    public const string SectionName = "InventoryReservations";

    public int HoldMinutes { get; set; } = 15;
    public int PaymentGraceMinutes { get; set; } = 10;
    public int MaximumLifetimeMinutes { get; set; } = 30;
    public int CleanupBatchSize { get; set; } = 200;
    public int MaxBatchesPerRun { get; set; } = 10;
    public string CleanupCron { get; set; } = "* * * * *";

    public TimeSpan HoldDuration => TimeSpan.FromMinutes(Math.Clamp(HoldMinutes, 1, 24 * 60));
    public TimeSpan PaymentGraceDuration => TimeSpan.FromMinutes(Math.Clamp(PaymentGraceMinutes, 1, 120));
    public TimeSpan MaximumLifetime => TimeSpan.FromMinutes(Math.Clamp(MaximumLifetimeMinutes, Math.Clamp(HoldMinutes, 1, 24 * 60), 24 * 60));
    public int SafeCleanupBatchSize => Math.Clamp(CleanupBatchSize, 10, 1000);
    public int SafeMaxBatchesPerRun => Math.Clamp(MaxBatchesPerRun, 1, 100);
}
