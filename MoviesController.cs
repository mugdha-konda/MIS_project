using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Group7_FinalProject.DAL;
using Group7_FinalProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace Group7_FinalProject.Controllers
{
    public class MoviesController : Controller
    {
        private readonly AppDbContext _context;

        public MoviesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Movies

        public async Task<IActionResult> Index(string searchString)
        {
            var movies = from m in _context.Movies
                         select m;

            if (!string.IsNullOrEmpty(searchString))
            {
                movies = movies.Where(s => s.MovieTitle.Contains(searchString));
            }

            var movieSearchViewModel = new MovieSearchViewModel
            {
                Results = await movies.ToListAsync()
            };

            return View(movieSearchViewModel);
        }

        public IActionResult Details(int? id)
        {
            if (id == null) //JobPostingID not specified
            {
                return View("Error", new String[] { "MovieID not specified - which job posting do you want to view?" });
            }

            Movie movie = _context.Movies.Include(j => j.Genre).FirstOrDefault(j => j.MovieID == id);

            if (movie == null) //Job posting does not exist in database
            {
                return View("Error", new String[] { "Movie not found in database" });
            }

            //if code gets this far, all is well
            return View(movie);

        }

        public async Task<IActionResult> Schedule(int id)
        {
            // Retrieve the schedule including related data from the database
            var schedule = await _context.Schedules
                .Include(s => s.MovieTitle) // Include the MovieTitle navigation property
                .Include(s => s.TicketType) // Include the Price navigation property assuming Price is the ticket type
                .FirstOrDefaultAsync(s => s.ScheduleID == id);

            // Check if the schedule exists
            if (schedule == null)
            {
                return View("Error", new String[] { "Schedule not found." });
            }

            // Return a view named "Schedule" that expects a Schedule model
            return View("~/Views/Schedule/Details.cshtml", schedule);
        }


        // GET: Movies/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null || _context.Movies == null)
        //    {
        //        return NotFound();
        //    }

        //    var movie = await _context.Movies
        //        .FirstOrDefaultAsync(m => m.MovieID == id);
        //    if (movie == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(movie);
        //}

        // GET: Movies/Create
        [Authorize(Roles = "Manager")]

        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MovieID,MovieTitle,MovieTime,MPAARating,CustomerRating,ReleaseYear")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        [Authorize(Roles = "Manager")]
        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Movies == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MovieID,MovieTitle,MovieTime,MPAARating,CustomerRating,ReleaseYear")] Movie movie)
        {
            if (id != movie.MovieID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.MovieID))
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
            return View(movie);
        }

        // GET: Movies/Delete/5
        [Authorize(Roles = "Manager")]

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Movies == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.MovieID == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Movies == null)
            {
                return Problem("Entity set 'AppDbContext.Movies'  is null.");
            }
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movies.Any(e => e.MovieID == id);
        }
    }
}
