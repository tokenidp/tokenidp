namespace IDP.Domain.Specifications;

public enum ControlTypes
{
    NavGroup, //Parent Group
    NavLink, //Page Link
    Action, //Add, Edit, Delete
    WorkflowAction //Approve, Reject, Submit
}