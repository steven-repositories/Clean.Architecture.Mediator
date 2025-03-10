namespace Clean.Architecture.Mediator.Shared.Configuration {
    public class Cors {
        public string? DefaultCorsPolicy { get; set; }
        public string[]? AllowedOrigins { get; set; }
    }
}
