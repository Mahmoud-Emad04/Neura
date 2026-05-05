using Neura.Core.Enums;

namespace Neura.Core.Contracts.Community;

public sealed record ChannelDto(
    int Id,
    string Name,
    string? Topic,
    ChannelType Type,
    int Position
);