namespace OrderApi.DTO;

public record OrderResponseDTO(Guid Id, Guid UserId, decimal Amount, string Asset, string Type, string Status);