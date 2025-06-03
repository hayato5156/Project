using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ECommercePlatform.Models
{
    public class Letter
    {
        public required string content { get; set; }
        public bool harassment { get; set; } = false;//騷擾
        public bool pornography { get; set; } = false;//色情內容
        public bool threaten { get; set; } = false;//威脅內容
        public bool Hatred { get; set; } = false;//仇恨或歧視內容
        public string detail { get; set; } = "無描述";
    }
    public class Messages
    {
        public int messageID { get; set; } = 0;
        public int replyID { get; set; } = 0;
        public int userID { get; set; }
        public int productID { get; set; }
        public string userName { get; set; } = "0202";
        public string main { get; set; }
        public DateTime date { get; set; } = DateTime.Now;
        public int score { get; set; }
        public byte[]? imageData { get; set; } = null;
    }
    public class account
    {
        public int ID { get; set; }
        public string userName { get; set; } = "visitor";
        public string Password { get; set; }
        public string buyOrSell { get; set; } = "visitor";
    }
}
