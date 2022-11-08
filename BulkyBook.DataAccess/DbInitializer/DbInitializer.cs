using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.DbInitializer
{

    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public DbInitializer(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager, 
            ApplicationDbContext db)
        {
            _db=db;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public void Initialize()
        {
            //migrations if they are not applied
            try
            {    //all pending migrations in Migrations folder will be applied here
                if(_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();

                }
            }
            catch(Exception ex)
            {

            }

            //roles if they are not created
            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_indi)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Comp)).GetAwaiter().GetResult();

                //if roles are not created also create admin user
                ApplicationUser applicationUser = new()
                {
                    UserName = "ppuzankovdev@hotmail.com",
                    Email = "ppuzankovdev@hotmail.com",
                    Name = "Paul",
                    PhoneNumber = "phonenumber",
                    StreetAddress = "streetaddress",
                    State = "State",
                    PostalCode = "zipcode",
                    City = "city"
                };
                _userManager.CreateAsync(applicationUser, "pPVZ@^X0V@E>").GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, SD.Role_Admin).GetAwaiter().GetResult();

            }

            return;
        }
    }
}
