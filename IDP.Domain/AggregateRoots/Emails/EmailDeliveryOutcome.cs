namespace IDP.Domain.AggregateRoots.Emails;

public enum EmailDeliveryOutcome : byte
{
    Success = 0,
    TransientFailure = 1,
    PermanentFailure = 2
}
