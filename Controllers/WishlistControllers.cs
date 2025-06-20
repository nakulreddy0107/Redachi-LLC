using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedachiApi.Data;
using RedachiApi.Models;
using RedachiApi.Dtos;


namespace RedachiApi.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WishlistController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("sync")]
    [Authorize]
    public async Task<IActionResult> SyncGuestWishlist([FromBody] List<ProductDto> guestProducts)
    {
        var userId = User.FindFirstValue(ClaimTypes.Name);
        if (userId == null) return Unauthorized();

        foreach (var product in guestProducts)
        {
            bool exists = await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == product.Id);

            if (!exists)
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId,
                    ProductId = product.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Product>>> GetUserWishlist()
    {
        var userId = User.FindFirstValue(ClaimTypes.Name);
        if (userId == null) return Unauthorized();

        var wishlistItems = await _context.WishlistItems
            .Where(w => w.UserId == userId)
            .Include(w => w.Product)
            .Select(w => w.Product)
            .ToListAsync();

        return Ok(wishlistItems);
    }

        // ✅ DELETE from wishlist
    [HttpDelete("{productId}")]
    [Authorize]
    public async Task<IActionResult> RemoveFromWishlist(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.Name);
        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

        if (item == null)
            return NotFound();

        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID claim is missing.");
        }
        return userId;
    }
}
