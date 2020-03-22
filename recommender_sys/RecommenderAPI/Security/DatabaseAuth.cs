/// Database authentication information.
public class DatabaseAuth : IDatabaseAuth
{
    public DatabaseAuth(string usr, string psw)
    {
        Username = usr ?? throw new System.ArgumentNullException(nameof(usr));
        Password = psw ?? throw new System.ArgumentNullException(nameof(psw));
    }

    public string Username { get; set; }
    public string Password { get; set; }
}