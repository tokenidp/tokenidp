using IDP.Domain.AggregateRoots.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDP.Infrastructure.Config;

internal class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(p => new { p.Id });

        builder.ToTable("RefreshTokens");
    }
}