namespace VenueModule.API.B2B.Tags.Dto;

public record TagCategoryDto(
    string Category,
    bool IsSingleSelect,
    TagDto[] Tags
);

public record TagDto(
    Guid Id,
    string Value,
    Guid? ParentId
);
