namespace IDP.Domain.Base;

public interface IBaseEntity
{
    public int CreatedBy { get; }
    public DateTime CreatedOn { get; }
    public int? UpdatedBy { get; }
    public DateTime? UpdatedOn { get; }

    void SetCreatedByAndCreatedOn(int userId);

    void SetUpdatedByAndUpdatedOn(int userId);
}
