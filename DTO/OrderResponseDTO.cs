namespace OrderApi.DTO;

public record OrderResponseDTO(Guid Id, Guid userId, decimal Amount, string Asset, string Type, string Status);