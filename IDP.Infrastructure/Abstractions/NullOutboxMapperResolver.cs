using IDP.Domain.AggregateRoots.Outbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDP.Infrastructure.Abstractions;

internal class NullOutboxMapperResolver : IOutboxMapperResolver
{
    public static readonly NullOutboxMapperResolver Instance = new();

    public OutboxEvent Resolve(IDomainEvent evt)
    {
        throw new NotImplementedException();
    }
}
