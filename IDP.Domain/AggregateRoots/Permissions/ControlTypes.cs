namespace IDP.Domain.AggregateRoots.Permissions;

public enum ControlTypes
{
    NavGroup, //Parent Group
    NavLink, //Page Link
    Action, //Add, Edit, Delete
    WorkflowAction, //Approve, Reject, Submit

    ApiResource,   // Parent: API / Resource (payments-api)
    ApiScope       // Child: Scope/Permission (payments.read, payments.write, payments.refund etc.)
}