using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ECommercePlatform.Models;
using ECommercePlatform.Services;


namespace ECommercePlatform.Controllers
{
    public class ProjectController : Controller
    {
        private readonly DBmanager _db;
        private readonly EmailService _emailService;

        public ProjectController(DBmanager db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        // 顯示留言列表
        public IActionResult Index(int? ID = null, string userName = "", string buyOrSell = "", string sort = "latest", string keyword = "", int? scoreFilter = null)
        {
            var m = _db.GetMessages();

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

        // 寄送檢舉信件
        public void ComplainSend(Letter l)
        {
            string body = $"檢舉內容:{l.content}\n騷擾:{l.harassment}\n色情:{l.pornography}\n威脅:{l.threaten}\n仇恨:{l.Hatred}\n詳細描述:{l.detail}";
            _emailService.SendComplainMail("留言檢舉通知", body);
        }

        // 刪除留言
        [HttpPost]
        public IActionResult DeleteMessage(int messageID)
        {
            var messages = _db.GetMessages();
            var target = messages.FirstOrDefault(x => x.messageID == messageID);
            if (target == null || (target.userID != (int?)ViewBag.ID && (string)ViewBag.identity != "admin"))
                return Unauthorized();

            _db.DeleteMessage(messageID);
            return Ok();
        }

        // 修改留言
        [HttpPost]
        public IActionResult UpdateMessage(Messages m)
        {
            _db.UpdateMessage(m);
            return Ok();
        }

        // 新增留言
        [HttpPost]
        public IActionResult SubmitMessage(int replyID, int userID, int productID, string userName, string main, int score, IFormFile image)
        {
            byte[]? imageData = null;
            if (image != null && image.Length > 0)
            {
                using var ms = new MemoryStream();
                image.CopyTo(ms);
                imageData = ms.ToArray();
            }

            var m = new Messages
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

            _db.AddMessage(m);
            return Ok();
        }

        // 登入頁
        [HttpGet]
        public IActionResult SingIn() => View();

        [HttpPost]
        public IActionResult SingIn(string username, string password)
        {
            var user = _db.Login(username, password);
            if (user == null)
            {
                ViewBag.Message = "登入失敗";
                return RedirectToAction("SingIn");
            }

            return RedirectToAction("Index", "Project", new { ID = user.Id, userName = user.Username, buyOrSell = user.Role });
        }

        // 註冊頁
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(User u)
        {
            var all = _db.GetUsers();
            if (all.Any(x => x.Username == u.Username))
            {
                ViewBag.Message = "帳號已存在";
                return View();
            }

            _db.AddUser(u);
            ViewBag.Message = "註冊成功，請登入";
            return RedirectToAction("SingIn");
        }

        // 錯誤頁
        public IActionResult ErrorAccount()
        {
            var m = new List<Messages>();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_MessageListPartial", m);
            return View(m);
        }
    }
}