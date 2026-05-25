public class OrderNotFoundException(Guid orderId)
    : Exception($"Order '{orderId}' was not found.");
