namespace HangfireCqrsOutbox.Domain;

public class User
{
    public User(string forename, string surename, string email)
    {
        Forename = forename;
        Surename = surename;
        Email = email;
    }

    public int Id { get; set; }
    public string Forename { get; set; }
    public string Surename { get; set; }
    public string Email { get; set; }
}
