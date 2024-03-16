using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tinterest.Data;
using Tinterest.Domain;

namespace Tinterest.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public UsersController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        [Route("api/users/create")]
        public ActionResult Create([FromBody] Users userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newUser = new Users
            {
                Name = userDto.Name,
                LastName = userDto.LastName,
                Gender = userDto.Gender,
                Email = userDto.Email,
                Password = userDto.Password
            };

            _dbContext.Users.Add(newUser);
            _dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        [Route("api/users/login")]
        public async Task<ActionResult> LogIn([FromQuery] string email, [FromQuery] string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return BadRequest("Email and password are required.");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                return NotFound("User not found or invalid email/password.");
            }

            return Ok(user);
        }

        [HttpGet]
        [Route("api/users")]
        public async Task<ActionResult> GetUserById([FromQuery] int id)
        {
            var user = await _dbContext.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user);
        }

        [HttpPut]
        [Route("api/users/update")]
        public async Task<ActionResult> UpdateUser([FromBody] Users userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userToUpdate = await _dbContext.Users.FindAsync(userDto.Id);

            if (userToUpdate == null)
            {
                return NotFound("User not found.");
            }

            // Обновляем только поля, которые были переданы
            if (!string.IsNullOrEmpty(userDto.Name))
            {
                userToUpdate.Name = userDto.Name;
            }

            if (!string.IsNullOrEmpty(userDto.LastName))
            {
                userToUpdate.LastName = userDto.LastName;
            }

            if (!string.IsNullOrEmpty(userDto.Gender))
            {
                userToUpdate.Gender = userDto.Gender;
            }

            if (!string.IsNullOrEmpty(userDto.Email))
            {
                userToUpdate.Email = userDto.Email;
            }

            if (!string.IsNullOrEmpty(userDto.Password))
            {
                userToUpdate.Password = userDto.Password;
            }

            if (!string.IsNullOrEmpty(userDto.City))
            {
                userToUpdate.City = userDto.City;
            }

            try
            {
                await _dbContext.SaveChangesAsync();
                return Ok("User updated successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                // Обработка исключения для конфликта параллелизма, если необходимо
                return StatusCode(500, "An error occurred while updating the user.");
            }
        }

        [HttpPost]
        [Route("api/users/addOrUpdateTags")]
        public async Task<ActionResult> AddOrUpdateUserTags([FromBody] UserTags userTags)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Предполагается, что в запросе есть поле UserId, указывающее на идентификатор пользователя
            int userId = userTags.UserId;

            // Получаем пользователя из базы данных по его идентификатору
            var user = await _dbContext.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Удаляем существующие теги пользователя
            var existingUserTags = _dbContext.UserTags.FirstOrDefault(ut => ut.UserId == userId);
            if (existingUserTags != null)
            {
                _dbContext.UserTags.Remove(existingUserTags);
            }

            // Преобразуем список тегов в JSON-массив
            var tagsArray = userTags.Tags.Split(',').Select(tag => tag.Trim()).ToList();
            var tagsJson = JsonConvert.SerializeObject(new { tags = tagsArray });

            // Создаем новую запись для тегов пользователя
            var newUserTags = new UserTags
            {
                UserId = userId,
                Tags = tagsJson // Сохраняем список тегов в виде JSON-объекта
            };

            _dbContext.UserTags.Add(newUserTags);

            await _dbContext.SaveChangesAsync();

            return Ok("User tags added or updated successfully.");
        }

        [HttpDelete]
        [Route("api/users/deleteTags")]
        public async Task<ActionResult> DeleteUserTags([FromBody] UserTags userTags)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Предполагается, что в запросе есть поле UserId, указывающее на идентификатор пользователя
            int userId = userTags.UserId;

            // Получаем теги пользователя из базы данных по его идентификатору
            var existingUserTags = await _dbContext.UserTags.FirstOrDefaultAsync(ut => ut.UserId == userId);

            if (existingUserTags == null)
            {
                return NotFound("User tags not found.");
            }

            // Десериализуем JSON-строку с тегами в объект
            var existingTagsObject = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(existingUserTags.Tags);

            // Получаем список тегов для удаления из запроса
            var tagsToRemove = userTags.Tags.Split(',').Select(tag => tag.Trim()).ToList();

            // Удаляем теги из объекта
            foreach (var tag in tagsToRemove)
            {
                existingTagsObject["tags"].Remove(tag);
            }

            // Сериализуем объект с тегами обратно в JSON-строку
            existingUserTags.Tags = JsonConvert.SerializeObject(existingTagsObject);

            await _dbContext.SaveChangesAsync();

            return Ok("User tags deleted successfully.");
        }

        [HttpGet]
        [Route("api/users/findBestMatchingUsers")]
        public ActionResult<IEnumerable<KeyValuePair<int, int>>> FindBestMatchingUsers(int userId, bool isCity)
        {
            var currentUser = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
            if (currentUser == null)
            {
                return NotFound("Пользователь с указанным Id не найден.");
            }

            var currentUserTags = _dbContext.UserTags.FirstOrDefault(ut => ut.UserId == userId);
            if (currentUserTags == null)
            {
                return BadRequest("У пользователя нет тегов.");
            }

            var currentUserTagList = JObject.Parse(currentUserTags.Tags)["tags"]
                .Select(t => t.ToString()).ToList();

            IQueryable<Users> usersQuery = _dbContext.Users.Where(u => u.Id != userId); // Формируем запрос пользователей без текущего пользователя

            if (isCity) // Если необходимо фильтровать пользователей по городу
            {
                usersQuery = usersQuery.Where(u => u.City == currentUser.City); // Фильтруем пользователей по городу
            }

            var usersInSameCity = usersQuery.ToList();
            if (usersInSameCity.Count == 0)
            {
                return NotFound("Пользователи в том же городе не найдены.");
            }

            var userMatches = new List<KeyValuePair<int, int>>(); // Список пар ключ-значение (Id пользователя, процент совпадений)

            foreach (var user in usersInSameCity)
            {
                var otherUserTags = _dbContext.UserTags.FirstOrDefault(ut => ut.UserId == user.Id);
                if (otherUserTags == null)
                {
                    continue; // Пропускаем пользователя, если у него нет тегов
                }

                var otherUserTagList = JObject.Parse(otherUserTags.Tags)["tags"]
                    .Select(t => t.ToString()).ToList();

                var matchCount = currentUserTagList.Intersect(otherUserTagList).Count();
                var percentMatch = (double)matchCount / currentUserTagList.Count * 100; // Вычисляем процент совпадений
                userMatches.Add(new KeyValuePair<int, int>(user.Id, (int)percentMatch));
            }

            var bestMatchingUsers = userMatches.OrderByDescending(pair => pair.Value).Take(100); // Выбираем 5 пользователей с наибольшим процентом совпадений

            return Ok(bestMatchingUsers);
        }

        [HttpPost]
        [Route("api/chat/create")]
        public IActionResult CreateChat([FromBody] Chat chatDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверяем, существует ли уже чат между этими пользователями
            var existingChat = _dbContext.Chats.FirstOrDefault(c =>
                (c.User1Id == chatDto.User1Id && c.User2Id == chatDto.User2Id) ||
                (c.User1Id == chatDto.User2Id && c.User2Id == chatDto.User1Id));

            if (existingChat != null)
            {
                return Conflict("Chat already exists between these users.");
            }

            var chat = new Chat
            {
                User1Id = chatDto.User1Id,
                User2Id = chatDto.User2Id
            };

            _dbContext.Chats.Add(chat);
            _dbContext.SaveChanges();

            return CreatedAtAction(nameof(GetChat), new { id = chat.ChatId }, chat);
        }

        [HttpGet]
        [Route("api/chat/get")]
        public IActionResult GetChat(int id)
        {
            var chat = _dbContext.Chats.FirstOrDefault(c => c.ChatId == id);
            if (chat == null)
            {
                return NotFound();
            }
            return Ok(chat);
        }

        [HttpGet]
        [Route("api/chat/allchats")]
        public IActionResult GetMyChats(int id)
        {
            var allChats = _dbContext.Chats
                .Where(c => c.User1Id == id)
                .ToList();
            if (allChats == null)
            {
                return NotFound();
            }
            return Ok(allChats);
        }

        [HttpPost]
        [Route("api/chat/send")]
        public IActionResult SendMessage([FromBody] Message messageDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var message = new Message
            {
                ChatId = messageDto.ChatId,
                SenderId = messageDto.SenderId,
                Content = messageDto.Content,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.Messages.Add(message);
            _dbContext.SaveChanges();

            return Ok(message);
        }

        [HttpGet]
        [Route("api/chat/messages")]
        public IActionResult GetChatMessages([FromQuery] int chatId)
        {
            var messages = _dbContext.Messages
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.Timestamp)
                .Take(200)
                .ToList();

            return Ok(messages);
        }

        [HttpPost]
        [Route("api/upload/image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, int userId, [FromServices] IWebHostEnvironment hostingEnvironment)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty");
            }

            var uploadsFolder = Path.Combine(hostingEnvironment.WebRootPath, "images");
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); // Генерируем уникальное имя файла
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Проверяем, существует ли папка, если нет, то создаем
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Добавляем имя файла в таблицу Users для пользователя с указанным userId
            var user = await _dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.Image = fileName;
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                return BadRequest("User not found");
            }

            return Ok("Image uploaded successfully");
        }

        [HttpGet]
        [Route("api/user/image/getmyimg")]
        public async Task<IActionResult> GetUserImage([FromQuery] int userId, [FromServices] IWebHostEnvironment hostingEnvironment)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (string.IsNullOrEmpty(user.Image))
            {
                return NotFound("User image not found");
            }

            var imagesFolder = Path.Combine(hostingEnvironment.WebRootPath, "images");
            var imagePath = Path.Combine(imagesFolder, user.Image);

            // Проверяем, существует ли файл с изображением пользователя
            if (!System.IO.File.Exists(imagePath))
            {
                return NotFound("User image not found");
            }

            var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
            return File(imageBytes, "image/jpeg"); // Возвращаем изображение в формате JPEG
        }
    }

}
