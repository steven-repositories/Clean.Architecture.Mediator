namespace Clean.Architecture.Mediator.Data.Abstractions {
    public interface ISoftDelete {
        DateTime? DateDeleted { get; set; }
    }
}
