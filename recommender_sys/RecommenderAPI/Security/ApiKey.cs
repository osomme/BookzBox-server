using System;

public class ApiKey : IKey
{

    private readonly string _apiKey;

    public ApiKey(string key)
    {
        _apiKey = key ?? throw new System.ArgumentNullException(nameof(key));
    }

    public bool IsValid(string key)
    {
        return key == _apiKey;
    }
}