namespace Tinterest.Data
{
    public class Chat
    {
        public int ChatId { get; set; }
        public int? User1Id { get; set; }
        public int? User2Id { get; set; }
    }

    public class Message
    {
        public int MessageId { get; set; }
        public int? ChatId { get; set; }
        public int? SenderId { get; set; }
        public string? Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
