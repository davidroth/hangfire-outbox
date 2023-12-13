namespace HangfireCqrsOutbox.Domain;

public class User(string forename, string surename, string email)
{
    public int Id { get; set; }
    public string Forename { get; set; } = forename;
    public string Surename { get; set; } = surename;
    public string Email { get; set; } = email;
}
