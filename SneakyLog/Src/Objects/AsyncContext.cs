using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SneakyLog.Objects;

internal class AsyncContext
{
    public string? CurrentMethodId { get; set; }
    public string? RequestId { get; set; }
}