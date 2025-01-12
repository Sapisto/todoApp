namespace PracticeApi.Domain.Entities
{
    public class Todo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Description { get; set; } 
        public bool IscomPleted { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
        public DateTime? CompletedAt { get; set; } 
        public DateTime? DueDate { get; set; } 
    }
}
