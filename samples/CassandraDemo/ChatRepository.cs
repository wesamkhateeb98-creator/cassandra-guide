using Cassandra;

namespace CassandraDemo;

public sealed record Message(Guid MessageId, DateTimeOffset SentAt, Guid SenderId, string Body);

public sealed record MessagePage(IReadOnlyList<Message> Messages, byte[]? PagingState);

public sealed class ChatRepository
{
    private readonly ISession _session;
    private readonly PreparedStatement _insertMessage;
    private readonly PreparedStatement _updateConversation;
    private readonly PreparedStatement _incrementUnread;
    private readonly PreparedStatement _selectMessages;
    private readonly PreparedStatement _claimEmail;

    private ChatRepository(ISession session, PreparedStatement[] ps)
    {
        _session = session;
        (_insertMessage, _updateConversation, _incrementUnread, _selectMessages, _claimEmail) =
            (ps[0], ps[1], ps[2], ps[3], ps[4]);
    }

    // Prepare once at startup, reuse for every request.
    public static async Task<ChatRepository> CreateAsync(ISession session)
    {
        var ps = await Task.WhenAll(
            session.PrepareAsync(
                "INSERT INTO chat.messages_by_conversation (conversation_id, bucket, message_id, sender_id, body) " +
                "VALUES (?, ?, ?, ?, ?)"),
            session.PrepareAsync(
                "UPDATE chat.conversations_by_user SET last_message_at = ?, last_message_preview = ? " +
                "WHERE user_id = ? AND conversation_id = ?"),
            session.PrepareAsync(
                "UPDATE chat.unread_by_user SET unread = unread + 1 WHERE user_id = ? AND conversation_id = ?"),
            session.PrepareAsync(
                "SELECT message_id, sender_id, body FROM chat.messages_by_conversation " +
                "WHERE conversation_id = ? AND bucket = ?"),
            session.PrepareAsync(
                "INSERT INTO chat.users_by_email (email, user_id) VALUES (?, ?) IF NOT EXISTS"));
        return new ChatRepository(session, ps);
    }

    public static int BucketOf(DateTimeOffset t) => t.Year * 100 + t.Month; // yyyymm

    public async Task<Guid> SendAsync(Guid conversationId, Guid senderId, Guid recipientId, string body)
    {
        var messageId = TimeUuid.NewId();
        var sentAt = messageId.GetDate();
        var preview = body.Length > 40 ? body[..40] : body;

        // Logged batch: keep the two denormalized tables in sync atomically.
        var batch = new BatchStatement()
            .Add(_insertMessage.Bind(conversationId, BucketOf(sentAt), messageId, senderId, body))
            .Add(_updateConversation.Bind(sentAt, preview, senderId, conversationId))
            .Add(_updateConversation.Bind(sentAt, preview, recipientId, conversationId));
        await _session.ExecuteAsync(batch);

        // Counters cannot share a batch with regular writes; counter increments are NOT idempotent.
        await _session.ExecuteAsync(_incrementUnread.Bind(recipientId, conversationId));

        return messageId.ToGuid();
    }

    public async Task<MessagePage> GetMessagesAsync(Guid conversationId, int bucket, int pageSize, byte[]? pagingState)
    {
        var statement = _selectMessages.Bind(conversationId, bucket)
            .SetPageSize(pageSize)
            .SetAutoPage(false)
            .SetPagingState(pagingState)
            .SetIdempotence(true);

        var rs = await _session.ExecuteAsync(statement);
        var messages = rs.Select(row =>
        {
            var id = row.GetValue<TimeUuid>("message_id");
            return new Message(id.ToGuid(), id.GetDate(), row.GetValue<Guid>("sender_id"), row.GetValue<string>("body"));
        }).ToList();

        return new MessagePage(messages, rs.PagingState);
    }

    /// <summary>LWT: returns false if the email is already owned by another user.</summary>
    public async Task<bool> TryClaimEmailAsync(string email, Guid userId)
    {
        var rs = await _session.ExecuteAsync(_claimEmail.Bind(email, userId));
        return rs.First().GetValue<bool>("[applied]");
    }
}
