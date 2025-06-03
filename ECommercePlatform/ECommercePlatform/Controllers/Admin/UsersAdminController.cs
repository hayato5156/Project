using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Data;
using ECommercePlatform.Models;

namespace ECommercePlatform.Controllers.Admin;
[ApiController]
[Route("api/admin/users")]
[Authorize(AuthenticationSchemes = "EngineerCookie")]
public class UsersAdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public UsersAdminController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpGet]
    public IActionResult GetAll()
    {
        var users = _context.Users
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.PhoneNumber,
                u.FirstName,
                u.LastName,
                u.Address
            })
            .ToList();
        return Ok(users);
    }
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null) return NotFound();
        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            user.Address
        });
    }
    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] User updated)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var user = _context.Users.Find(id);
        if (user == null) return NotFound();
        try
        {
            user.Username = updated.Username;
            user.Email = updated.Email;
            user.PhoneNumber = updated.PhoneNumber;
            user.FirstName = updated.FirstName;
            user.LastName = updated.LastName;
            user.Address = updated.Address;
            _context.SaveChanges();
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, "更新使用者失敗：" + ex.Message);
        }
    }
}