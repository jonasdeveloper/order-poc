namespace OrderApi.DTO;

public record OrderRequestDTO(decimal Amount, string Asset, string Type);