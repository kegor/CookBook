using System;
using System.Collections.Generic;
using System.Data.Entity;
using CookBookCommon;

namespace CookBookCommon
{
    public class CookBookContext : DbContext
    {
        // for creation from WebRole controllers
        public CookBookContext() : base("name=CookBookContext")
        {
        }

        // constructor for creation from WorkerRole class
        // because it does not have web.config with connString
        public CookBookContext(string connString) : base(connString)
        {
        }

        public System.Data.Entity.DbSet<Recipe> Recipes { get; set; }
    
    }
}
