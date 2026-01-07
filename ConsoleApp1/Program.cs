using System;

class Program
{
    static void Main(string[] args)
    {
        // Replace with actual user id and username
        string userId = "123";
        string username = "hecinauser";

        string token = JwtTokenGenerator.GenerateToken(userId, username);

        Console.WriteLine("Generated JWT Token:");
        Console.WriteLine(token);
    }
}