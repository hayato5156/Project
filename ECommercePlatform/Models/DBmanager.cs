using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace ECommercePlatform.Models
{
    public class DBmanager
    {
        // 資料庫連線字串
        private readonly string connStr = "Data Source=(localdb)\\MSSQLLocalDB;Database=ECommercePlatform;Trusted_Connection=True";

        // 留言系統
        // 取得所有留言
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
    }
}