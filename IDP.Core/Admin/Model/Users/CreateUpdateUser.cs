namespace IDP.Core.Admin.Model.Users;

internal class CreateUpdateUser
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string UserName { get; set; }
    public required string Phone { get; set; }
    public required string Password { get; set; }
    public required string Address1 { get; set; }
    public string? Address2 { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string Zip { get; set; }
    public required int[] Roles { get; set; }
}
