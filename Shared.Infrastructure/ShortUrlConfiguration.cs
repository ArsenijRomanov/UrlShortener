using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Shared.Domain;

namespace UrlShortener.Shared.Infrastructure;

public class ShortUrlConfiguration: IEntityTypeConfiguration<ShortUrl>
{
    public void Configure(EntityTypeBuilder<ShortUrl> shortUrl)
    {
        shortUrl.ToTable("Short_Urls");
        shortUrl.HasKey(x => x.Id);
        shortUrl.Property(x => x.Id).HasColumnName("id");
        shortUrl.Property(x => x.LongUrl).HasColumnName("long_url").IsRequired();
        shortUrl.Property(x => x.ShortCode).HasColumnName("short_code").IsRequired();
        shortUrl.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        shortUrl.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        shortUrl.HasIndex(x => x.ShortCode).IsUnique();
    }
}
