namespace OrderApi.DTO;

public record OrderResponseDTO(Guid Id, decimal Amount, string Asset, string Type, string Status);