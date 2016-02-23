using System;
using System.Collections.Generic;
using System.Data.Entity;
using CookBookCommon;

namespace CookBookCommon
{
    public class CookBookContext : DbContext
    {
        public CookBookContext() : base("name=CookBookContext")
        {
        }

        public CookBookContext(string connString) : base(connString)
        {
        }

        public System.Data.Entity.DbSet<Recipe> Recipes { get; set; }
    
    }
}
