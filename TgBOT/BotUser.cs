using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgBOT.Models;
using Npgsql;


namespace TgBOT
{

    public class Constants
    {
        public static string adress = "https://localhost:7079";
        public static string Connect = "Host=localhost;Username=postgres;Password=strongpass;Database=postgres";
    }
    public class User
    {
        private HttpClient _httpClient;
        private static string _adress;


        public User()
        {
            _adress = Constants.adress;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_adress);

        }
        public async Task<List<Holiday>> GetPublicHolidaysAsync(string countryCode)
        {

            var responce = await _httpClient.GetAsync($"/Holidays/{countryCode}/publicholidays");
            responce.EnsureSuccessStatusCode();
            var content = responce.Content.ReadAsStringAsync().Result;

            List<Holiday> holidays = JsonConvert.DeserializeObject<List<Holiday>>(content);

            return holidays;
        }
        public async Task<List<Countries>> GetCountriesAsync()
        {

            var responce = await _httpClient.GetAsync($"/countries");
            responce.EnsureSuccessStatusCode();
            var content = responce.Content.ReadAsStringAsync().Result;

            List<Countries> countries = JsonConvert.DeserializeObject<List<Countries>>(content);

            return countries;
        }
        public async Task<UserEvents> PutUserEventsAsync(string date, string name, string notes, long id)
        {
            Database database = new Database();
            UserEvents userEvents = new UserEvents();
            userEvents.Date = date;
            userEvents.Name = name;
            userEvents.Notes = notes;
            userEvents.Id = id;
            await database.InsertUserEventsAsync(userEvents, date, name, notes, id);
            return userEvents;
        }
        public async Task<UserEventDB> PutUpdateUserEventsAsync(string date, string name, string notes, long id)
        {
            Database database = new Database();
            UserEventDB userEvents = new UserEventDB();
            userEvents.Date = date;
            userEvents.Name = name;
            userEvents.Notes = notes;
            userEvents.Id = id;
            await database.UpdateUserEventAsync(userEvents);
            return userEvents;
        }
        public class Database
        {
            NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect);

            public async Task InsertHolidaysAsync(List<Holiday> holidays, string countryCode)
            {

                var sql = "insert into public.\"Holidays\"(\"date\", \"localName\", \"name\", \"countryCode\", \"Time\")"
                    + $"values (@date,@localName,@name,@countryCode,@Time)";

                NpgsqlCommand command = new NpgsqlCommand(sql, connection);

                foreach (var result in holidays)
                {
                    command.Parameters.AddWithValue("date", result.date);
                    command.Parameters.AddWithValue("localName", result.localName);
                    command.Parameters.AddWithValue("name", result.name);
                    command.Parameters.AddWithValue("countryCode", result.countryCode);


                }

                command.Parameters.AddWithValue("Time", DateTime.Now);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();

            }
            public async Task InsertUserEventsAsync(UserEvents userEvents, string date, string name, string notes, long id)
            {

                var sql = "insert into public.\"UserEvents\"(\"Date\", \"Name\", \"Notes\", \"Time\", \"Id\" )"
                    + $"values (@Date,@Name,@Notes,@Time, @Id)";

                NpgsqlCommand command = new NpgsqlCommand(sql, connection);


                command.Parameters.AddWithValue("Date", date);
                command.Parameters.AddWithValue("Name", name);
                command.Parameters.AddWithValue("Notes", notes);
                command.Parameters.AddWithValue("Time", DateTime.Now);
                command.Parameters.AddWithValue("Id", id);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();

            }
            public async Task UpdateUserEventAsync(UserEventDB userEventDB)
            {

                await connection.OpenAsync();

                var sql = "update public.\"UserEvents\" set \"Date\" = @Date, \"Notes\" = @Notes where \"Name\" = @Name";
                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("Date", userEventDB.Date);
                command.Parameters.AddWithValue("Name", userEventDB.Name);
                command.Parameters.AddWithValue("Notes", userEventDB.Notes);

                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
            public async Task DeleteUserEventAsync(string name)
            {
                await connection.OpenAsync();

                var sql = "delete from public.\"UserEvents\" where \"Name\" = @Name";
                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("Name", name);
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }


        }
    }
}

