using IDP.Domain.AggregateRoots;
using IDP.Domain.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDP.Infrastructure.Config;

internal class ClientConfig : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("Clients");

        builder.Property(p => p.TokenType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TokenTypes>(v));

        builder.Property(p => p.ClientType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ClientTypes>(v));

        builder.Property(p => p.AppType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AppTypes>(v));
    }
}
