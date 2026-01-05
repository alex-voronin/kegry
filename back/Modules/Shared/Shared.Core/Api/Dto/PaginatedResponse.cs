namespace SharedCore.Api.Dto;

public record PaginatedResponse<TDto>(ICollection<TDto> Data, int TotalCount);