﻿namespace designPattern
{
    /// <summary>
    /// Basic static factory example: 
    /// Not a "design pattern"
    /// Static factory is similar to constructor and could have some advantages
    /// 1: factory can reutrn object from subclass
    /// 2: static factory as constructor allows "constructor" with same signature
    /// 3: control limited resource e.g., HTTP connection pool
    /// </summary>
    public class UserStaticFactory
    {
        public string Name { get; private set; }
        public bool IsAdmin { get; private set; }

        private UserStaticFactory(string name, bool isAdmin)
        {
            Name = name;
            IsAdmin = isAdmin;
        }

        public static UserStaticFactory CreateAdminUser(string name)
        {
            return new UserStaticFactory(name, true);
        }

        public static UserStaticFactory CreateNormalUser(string name)
        {
            return new UserStaticFactory(name, false);
        }
    }
}
