using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// FluentAPI configuration for the <see cref="FriendRequest"/> entity.
/// </summary>
public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder
            .HasKey(fr => fr.Id);

        builder
            .HasOne(fr => fr.Requester)
            .WithMany()
            .HasForeignKey(fr => fr.RequesterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(fr => fr.Addressee)
            .WithMany()
            .HasForeignKey(fr => fr.AddresseeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent duplicate pending/accepted requests between the same pair
        builder
            .HasIndex(fr => new { fr.RequesterId, fr.AddresseeId })
            .IsUnique();

        builder
            .HasIndex(fr => fr.AddresseeId);

        builder
            .HasIndex(fr => fr.Status);
    }
}
