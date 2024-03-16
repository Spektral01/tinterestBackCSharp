using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tinterest.Data;
using Tinterest.Domain;
using Tinterest.Controllers;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddTransient<ApplicationDbContext>();


        builder.Logging.AddConsole();

        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        builder.Configuration.AddJsonFile("appsettings.json");
        var dbContext = new ApplicationDbContext(builder.Configuration);

/*            var newUser = new Users
            {
                Name = "John",
                LastName = "Doe",
                Gender = "Male",
                Email = "john.doe@example.com",
                Password = "password123"
            };

            dbContext.Users.Add(newUser);
            dbContext.SaveChanges();*/

        AddToDatabase addToDatabase = new AddToDatabase();
        //addToDatabase.MyTestAdd(dbContext);
        var users = dbContext.Users.ToList();

        var userId = 4; // ID пользователя, для которого вы хотите добавить теги
        var tagsJsonString = "{\"tags\": [\"tag1\", \"tag2\", \"tag133\"]}"; // Пример JSON-строки тегов

        // Преобразуйте JSON-строку в объект JObject из библиотеки Newtonsoft.Json
        var tagsJson = JObject.Parse(tagsJsonString);

        // Вызовите метод AddUserTags для добавления тегов для пользователя с заданным ID
        //addToDatabase.AddOrUpdateUserTags(userId, tagsJson, dbContext);

        Console.WriteLine(users);

        addToDatabase.FindBestMatchingUsers(31, true, dbContext);



        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}