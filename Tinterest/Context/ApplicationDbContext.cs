using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Tinterest.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tinterest.Domain
{
    public class ApplicationDbContext : DbContext
    {
        protected readonly IConfiguration Configuration;

        public ApplicationDbContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole()); // Создаем фабрику логов с выводом в консоль
            options.UseLoggerFactory(loggerFactory); // Подключаем фабрику логов к контексту базы данных
            options.EnableSensitiveDataLogging(); // Включаем логирование чувствительных данных (например, параметры SQL-запросов)
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")); // Настройка подключения к PostgreSQL
        }


        public DbSet<Users> Users { get; set; }
        public DbSet<UserTags> UserTags { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }

    }

    public class AddToDatabase
    {
        public void AddOrUpdateUserTags(int userId, JObject tags, ApplicationDbContext dbContext)
        {
            var existingUserTags = dbContext.UserTags.FirstOrDefault(ut => ut.UserId == userId);

            if (existingUserTags != null)
            {
                // Если запись для пользователя уже существует, обновляем его теги
                var existingTags = JObject.Parse(existingUserTags.Tags);
                existingTags.Merge(tags);
                existingUserTags.Tags = existingTags.ToString();
            }
            else
            {
                // Если запись для пользователя не существует, создаем новую запись
                var newUserTags = new UserTags
                {
                    UserId = userId,
                    Tags = tags.ToString() // Преобразование JSON в строку для хранения в базе данных
                };

                dbContext.UserTags.Add(newUserTags);
            }

            dbContext.SaveChanges();
        }

        public void FindBestMatchingUsers(int userId, bool isCity, ApplicationDbContext dbContext)
        {
            var currentUser = dbContext.Users.FirstOrDefault(u => u.Id == userId);
            if (currentUser == null)
            {
                Console.WriteLine("Пользователь с указанным Id не найден.");
                return;
            }

            var currentUserTags = dbContext.UserTags.FirstOrDefault(ut => ut.UserId == userId);

            var currentUserTagList = JObject.Parse(currentUserTags.Tags)["tags"]
                .Select(t => t.ToString()).ToList();

            IQueryable<Users> usersQuery = dbContext.Users.Where(u => u.Id != userId); // Формируем запрос пользователей без текущего пользователя

            if (isCity) // Если необходимо фильтровать пользователей по городу
            {
                usersQuery = usersQuery.Where(u => u.City == currentUser.City); // Фильтруем пользователей по городу
            }

            var usersInSameCity = usersQuery.ToList();

            if (currentUserTags == null || usersInSameCity.Count == 0)
            {
                Console.WriteLine("Пользователи не найдены.");
                return;
            }

            var userMatches = new Dictionary<int, int>(); // Ключ - Id пользователя, значение - процент совпадений по тэгам

            foreach (var user in usersInSameCity)
            {
                var otherUserTags = dbContext.UserTags.FirstOrDefault(ut => ut.UserId == user.Id);
                if (otherUserTags == null)
                {
                    continue; // Пропускаем пользователя, если у него нет тегов
                }

                var otherUserTagList = JObject.Parse(otherUserTags.Tags)["tags"]
                    .Select(t => t.ToString()).ToList();

                var matchCount = currentUserTagList.Intersect(otherUserTagList).Count();
                var percentMatch = (double)matchCount / currentUserTagList.Count * 100; // Вычисляем процент совпадений
                userMatches.Add(user.Id, (int)percentMatch);
            }

            var bestMatchingUsers = userMatches.OrderByDescending(pair => pair.Value).Take(5); // Выбираем 5 пользователей с наибольшим процентом совпадений

            foreach (var userMatch in bestMatchingUsers)
            {
                Console.WriteLine($"Пользователь: {userMatch.Key}, процент совпадений: {userMatch.Value}%");
            }
        }



    }




}
