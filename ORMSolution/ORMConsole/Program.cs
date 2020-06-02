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

            ORMInitialize database = new ORMInitialize(configuration);

            Users users = new Users();
            users.Fetch();

            Console.WriteLine("Hello World!");
        }
    }
}