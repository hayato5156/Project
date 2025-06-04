using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.Data.SqlClient;

namespace ECommercePlatform.Models
{
    public class DBmanager
    {
        // 資料庫連線字串
        private readonly string connStr = "Data Source=(localdb)\\MSSQLLocalDB;Database=ECommercePlatform;Trusted_Connection=True";

        //留言系統
        //取得所有留言
        public List<Messages> GetMessages()
        {
            List<Messages> messages = new();
            using var conn = new SqlConnection(connStr);
            var cmd = new SqlCommand("SELECT * FROM message", conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                messages.Add(new Messages
                {
                    messageID = reader.GetInt32(reader.GetOrdinal("messageID")),
                    replyID = reader.GetInt32(reader.GetOrdinal("replyID")),
                    productID = reader.GetInt32(reader.GetOrdinal("productID")),
                    userID = reader.GetInt32(reader.GetOrdinal("userID")),
                    userName = reader.GetString(reader.GetOrdinal("userName")),
                    main = reader.GetString(reader.GetOrdinal("main")),
                    date = reader.GetDateTime(reader.GetOrdinal("date")),
                    score = reader.GetInt32(reader.GetOrdinal("score")),
                    imageData = reader.IsDBNull(reader.GetOrdinal("imageData")) ? null : reader.GetSqlBinary(reader.GetOrdinal("imageData")).Value
                });
            }
            return messages;
        }

        // 新增留言
        public void AddMessage(Messages m)
        {
            using var conn = new SqlConnection(connStr);
            string sql = m.imageData != null
                ? @"INSERT INTO message(replyID,userID,productID,userName,main,date,score,imageData)
                    VALUES(@replyID,@userID,@productID,@userName,@main,@date,@score,@imageData)"
                : @"INSERT INTO message(replyID,userID,productID,userName,main,date,score)
                    VALUES(@replyID,@userID,@productID,@userName,@main,@date,@score)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@replyID", m.replyID);
            cmd.Parameters.AddWithValue("@userID", m.userID);
            cmd.Parameters.AddWithValue("@productID", m.productID);
            cmd.Parameters.AddWithValue("@userName", m.userName);
            cmd.Parameters.AddWithValue("@main", m.main);
            cmd.Parameters.AddWithValue("@date", m.date);
            cmd.Parameters.AddWithValue("@score", m.score);
            if (m.imageData != null)
                cmd.Parameters.AddWithValue("@imageData", m.imageData);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // 更新留言
        public void UpdateMessage(Messages m)
        {
            using var conn = new SqlConnection(connStr);
            string sql = m.imageData != null
                ? @"UPDATE message SET main=@main, date=@date, score=@score, imageData=@imageData WHERE messageID=@id"
                : @"UPDATE message SET main=@main, date=@date, score=@score WHERE messageID=@id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", m.messageID);
            cmd.Parameters.AddWithValue("@main", m.main);
            cmd.Parameters.AddWithValue("@date", m.date);
            cmd.Parameters.AddWithValue("@score", m.score);
            if (m.imageData != null)
                cmd.Parameters.AddWithValue("@imageData", m.imageData);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // 刪除留言
        public void DeleteMessage(int messageID)
        {
            using var conn = new SqlConnection(connStr);
            var cmd = new SqlCommand("DELETE FROM message WHERE messageID = @id", conn);
            cmd.Parameters.AddWithValue("@id", messageID);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        //使用者系統
        //新增使用者（註冊）
        public void AddUser(User u)
        {
            using var conn = new SqlConnection(connStr);
            var sql = @"INSERT INTO Users(Username, Email, PasswordHash, Role, CreatedAt, IsActive)
                        VALUES(@Username, @Email, @PasswordHash, @Role, @CreatedAt, @IsActive)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", u.Username);
            cmd.Parameters.AddWithValue("@Email", u.Email);
            cmd.Parameters.AddWithValue("@PasswordHash", u.PasswordHash);
            cmd.Parameters.AddWithValue("@Role", u.Role ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", u.CreatedAt);
            cmd.Parameters.AddWithValue("@IsActive", u.IsActive);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // 取得所有使用者
        public List<User> GetUsers()
        {
            var users = new List<User>();
            using var conn = new SqlConnection(connStr);
            var cmd = new SqlCommand("SELECT * FROM Users", conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Username = reader.GetString(reader.GetOrdinal("Username")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                    Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? null : reader.GetString(reader.GetOrdinal("Role")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            return users;
        }

        // 使用者登入（比對帳號密碼）
        public User? Login(string username, string passwordHash)
        {
            using var conn = new SqlConnection(connStr);
            var cmd = new SqlCommand("SELECT * FROM Users WHERE Username=@u AND PasswordHash=@p", conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", passwordHash);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Username = reader.GetString(reader.GetOrdinal("Username")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? null : reader.GetString(reader.GetOrdinal("Role")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                };
            }
            return null;
        }
    }
}
