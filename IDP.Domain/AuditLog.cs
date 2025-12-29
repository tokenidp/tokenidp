namespace IDP.Domain;

public class AuditLog : IBaseEntity
{
    public int Id { get; set; }
    public string TableName { get; private set; }
    public string ActionType { get; private set; }
    public string RecordId { get; private set; }
    public string OldValues { get; private set; }
    public string NewValues { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    private AuditLog() { }

    public AuditLog(string tableName,
        string actionType,
        string recordId,
        string oldValue,
        string newValue)
    {
        TableName = tableName;
        ActionType = actionType;
        RecordId = recordId;
        OldValues = oldValue;
        NewValues = newValue;
    }

    public void SetCreatedByAndCreatedOn(int userId)
    {
        CreatedOn = DateTime.UtcNow;
        CreatedBy = userId;
    }

    public void SetUpdatedByAndUpdatedOn(int userId)
    {
        UpdatedOn = DateTime.UtcNow;
        UpdatedBy = userId;
    }
}
