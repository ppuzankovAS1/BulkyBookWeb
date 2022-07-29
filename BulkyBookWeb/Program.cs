using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//This is how dependency injection is applied/registered
//This is how a service is added to the builder container
//This is a service for MVC application
//Razor page application would have a different service.

#pragma warning restore CS0436 // Type conflicts with imported type
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ));
#pragma warning restore CS0436 // Type conflicts with imported type

builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

//my class file name

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();






builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
//builder.Services.AddRazorPages();




    // perform some parallel operation
    var app = builder.Build();






// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // if this is running in development environment (Developer Mode?)
    // user friendly exeption page
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//if the environment is not in development
// then redirect to an error page


app.UseHttpsRedirection();

//middleware that allows to use static files in wwwroot folder
app.UseStaticFiles();


//this is also middleware
app.UseRouting();
app.UseAuthentication();;

//app.UseAuthentication(); //note Authentication must be done
                         //before Authorization 
 //middleware
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();
