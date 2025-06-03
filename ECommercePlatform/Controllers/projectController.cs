using Microsoft.AspNetCore.Mvc;
using System.Linq;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting.Server;
using MimeKit;
using MailKit.Net.Smtp;
using MimeKit.Cryptography;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using ECommercePlatform.Models;

namespace ECommercePlatform.Controllers
{
    public class projectController : Controller
    {
        //讀取資料庫中的留言
        public IActionResult Index(int? ID = null, string userName = "", string buyOrSell = "", string sort = "latest", string keyword = "", int? scoreFilter = null)
        {
            DBmanager db = new DBmanager();
            List<Messages> m = db.getMessages();

            if (!string.IsNullOrEmpty(keyword))
                m = m.Where(x => x.main.Contains(keyword)).ToList();

            if (scoreFilter.HasValue)
                m = m.Where(x => x.score == scoreFilter.Value).ToList();

            m = sort switch
            {
                "latest" => m.OrderByDescending(x => x.date).ToList(),
                "oldest" => m.OrderBy(x => x.date).ToList(),
                "highscore" => m.OrderByDescending(x => x.score).ToList(),
                "lowscore" => m.OrderBy(x => x.score).ToList(),
                _ => m
            };

            ViewBag.ID = ID;
            ViewBag.name = userName;
            ViewBag.identity = buyOrSell;
            ViewBag.messages = m;
            ViewBag.totalMessages = m?.Count ?? 0;
            ViewBag.averageScore = m?.Count > 0 ? m.Average(x => x.score) : 0;


            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_MessageListPartial", m);

            return View(m);

        }
        //寄檢舉留言給管理員
        public void complainSend(Letter l)
        {
            string body = $"檢舉內容:{l.content}\n騷擾:{l.harassment}\n色情:{l.pornography}\n威脅:{l.threaten}\n仇恨:{l.Hatred}\n詳細描述:{l.detail}"; // 信件內容

            MimeMessage message = new();
            message.From.Add(new MailboxAddress("Server", "testproject9487@gmail.com"));//(寄信者名稱, 寄送者信箱)
            message.To.Add(MailboxAddress.Parse("testproject9487@gmail.com"));// 目標信箱
            message.Subject = "測試信件";// 信件主旨
            message.Body = new TextPart("html")
            {
                Text = body
            };

            using (SmtpClient client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);//(送信郵件主機, 送信郵件主機連接埠, )
                client.Authenticate("testproject9487@gmail.com", "kphp eeng ektm czxu");//(帳號, 密碼)
                client.Send(message);
                client.Disconnect(true);
            }
        }
        //刪除資料庫中的留言
        public void delMessage(Messages m)
        {
            DBmanager dBmanager = new DBmanager();
            dBmanager.deleteMessage(m);
        }
        //修改資料庫中的留言
        public void modMessage(Messages m)
        {
            DBmanager dBmanager = new DBmanager();
            dBmanager.fixMessage(m);
        }
        //存入留言(回覆)
        public int setMessage(Messages m)
        {
            DBmanager dBmanager = new DBmanager();
            dBmanager.keyinMessage(m);
            return dBmanager.getNewMessageID(m);
        }
        public IActionResult singin(account u)
        {
            if (Request.Method == "GET")
            {
                // 修正：定義變數 m，避免 CS0103 錯誤  
                List<Messages> m = new List<Messages>();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return PartialView("_MessageListPartial", m);

                return View(m);
            }
            else
            {
                DBmanager dbmanager = new DBmanager();
                try
                {
                    dbmanager.login(u);
                    account accounts = dbmanager.login(u);
                    return RedirectToAction("Index", "project", new account { ID = accounts.ID, userName = accounts.userName, buyOrSell = accounts.buyOrSell });
                }
                catch
                {
                    Console.WriteLine("error");
                    return RedirectToAction("singin", "project");
                }
            }
        }
        public IActionResult errorAccount()
        {
            // 修正：定義變數 m，避免 CS0103 錯誤
            List<Messages> m = new List<Messages>();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_MessageListPartial", m);

            return View(m);
        }
        [HttpPost]
        public IActionResult DeleteMessage(int messageID)
        {
            DBmanager db = new DBmanager();
            var all = db.getMessages();
            var target = all.FirstOrDefault(x => x.messageID == messageID);
            if (target == null || (target.userID != ViewBag.ID && ViewBag.identity != "admin"))
                return Unauthorized();

            db.deleteMessage(target);
            return Ok();
        }
        [HttpPost]
        public IActionResult SubmitMessage(int replyID, int userID, int productID, string userName, string main, int score, IFormFile image)
        {
            DBmanager db = new DBmanager();
            byte[]? imageData = null;
            if (image != null && image.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    image.CopyTo(ms);
                    imageData = ms.ToArray();
                }
            }
            Messages m = new Messages
            {
                replyID = replyID,
                userID = userID,
                productID = productID,
                userName = userName,
                main = main,
                score = score,
                date = DateTime.Now,
                imageData = imageData
            };
            db.keyinMessage(m);
            return Ok();
        }
    }
}