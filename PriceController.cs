using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Group7_FinalProject.DAL;
using Group7_FinalProject.Models;

namespace Group7_FinalProject.Controllers
{
    public class PriceController : Controller
    {
        private readonly AppDbContext _context;

        public PriceController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Price
        public async Task<IActionResult> Index()
        {
              return View(await _context.Prices.ToListAsync());
        }

        // GET: Price/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Prices == null)
            {
                return NotFound();
            }

            var price = await _context.Prices
                .FirstOrDefaultAsync(m => m.PriceID == id);
            if (price == null)
            {
                return NotFound();
            }

            return View(price);
        }

        // GET: Price/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Price/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PriceID,TicketTypes,TicketPrice")] Price price)
        {
            if (ModelState.IsValid)
            {
                _context.Add(price);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(price);
        }

        // GET: Price/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Prices == null)
            {
                return NotFound();
            }

            var price = await _context.Prices.FindAsync(id);
            if (price == null)
            {
                return NotFound();
            }
            return View(price);
        }

        // POST: Price/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PriceID,TicketTypes,TicketPrice")] Price price)
        {
            if (id != price.PriceID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(price);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PriceExists(price.PriceID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(price);
        }

        // GET: Price/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Prices == null)
            {
                return NotFound();
            }

            var price = await _context.Prices
                .FirstOrDefaultAsync(m => m.PriceID == id);
            if (price == null)
            {
                return NotFound();
            }

            return View(price);
        }

        // POST: Price/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Prices == null)
            {
                return Problem("Entity set 'AppDbContext.Prices'  is null.");
            }
            var price = await _context.Prices.FindAsync(id);
            if (price != null)
            {
                _context.Prices.Remove(price);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PriceExists(int id)
        {
          return _context.Prices.Any(e => e.PriceID == id);
        }
    }
}
