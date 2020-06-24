﻿using Microsoft.Extensions.Configuration;
using ORM;
using System;

namespace ORMConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            new ORMInitialize(configuration);

            Users users = new Users();
            users.Fetch();
            ShowOutput(users);

            users = new Users();
            users.Fetch(1);
            ShowOutput(users);

            users = new Users();
            users.Where(x => x.Id == 1 || x.Id == 2);
            users.Fetch();
            ShowOutput(users);

            users = new Users();
            users.Where(x => x.Id == 1 || x.Id == 5);
            users.Fetch(1);
            ShowOutput(users);

            users = new Users();
            users.Where(x => (((x.Id == 2 || x.Id == 3) || (x.Id == 3 && x.Id == 4)) || x.Id == 5));
            users.Fetch();
            ShowOutput(users);

            Console.Read();
        }

        private static void ShowOutput(Users users)
        {
            foreach (User user in users)
            {
                Console.WriteLine($"[{ nameof(user.Id) }] { user.Id }");
                Console.WriteLine($"[{ nameof(user.Username) }] { user.Username }");
                Console.WriteLine($"[{ nameof(user.Password) }] { user.Password }");
                Console.WriteLine("-------------------");
            }

            Console.WriteLine($"Generated query: { users.GetQuery }");
            Console.WriteLine("-------------------");
        }
    }
}
