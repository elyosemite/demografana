using MassTransit;

namespace Demografana.Core.Infrastructure;

public class SimpleNameEntityFormatter : IEntityNameFormatter
{
    public string FormatEntityName<T>() => typeof(T).Name;
}
