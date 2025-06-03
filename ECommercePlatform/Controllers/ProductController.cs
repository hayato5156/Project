using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;
using ECommercePlatform.Models;
﻿using Microsoft.AspNetCore.Authorization;

namespace ECommercePlatform.Controllers
{
    [Authorize(AuthenticationSchemes = "EngineerCookie")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }
        // 取得所有產品資料
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }
        // 依照 ID 取得產品資料
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return product;
        }
        // 新增一筆產品資料
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        // 更新產品資料
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        // 刪除產品資料
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}