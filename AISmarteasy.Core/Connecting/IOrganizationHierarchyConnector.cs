﻿namespace AISmarteasy.Core.Connecting;

public interface IOrganizationHierarchyConnector
{
    Task<IEnumerable<string>> GetDirectReportsEmailAsync(CancellationToken cancellationToken = default);

    Task<string> GetManagerEmailAsync(CancellationToken cancellationToken = default);

    Task<string> GetManagerNameAsync(CancellationToken cancellationToken = default);
}
