using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Group7_FinalProject.DAL;
using Group7_FinalProject.Models;
using Newtonsoft.Json;

namespace Group7_FinalProject.Controllers
{


    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }
        public ActionResult Index(string SearchString, string GenreSearch)
        {
            var query = _context.Movies.AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(m => m.MovieTitle.Contains(SearchString) || m.MovieTagline.Contains(SearchString));
            }

            if (!string.IsNullOrEmpty(GenreSearch))
            {
                query = query.Where(m => m.Genre.GenreName.Contains(GenreSearch));
            }

            List<Movie> SelectedMovies = query.Include(m => m.Genre).ToList();
            ViewBag.AllMovies = _context.Movies.Count();
            ViewBag.SelectedMovies = SelectedMovies.Count();

            return View(SelectedMovies.OrderByDescending(m => m.StartTime));
        }

        // GET: Home
        //public ActionResult Index(string SearchString, string GenreSearch)
        //{
        //    var query = from jp in _context.Movies
        //                select jp;

        //    // Search by title or tagline
        //    if (!string.IsNullOrEmpty(SearchString))
        //    {
        //        query = query.Where(jp => jp.MovieTitle.Contains(SearchString) || jp.MovieTagline.Contains(SearchString));
        //    }

        //    // Search by genre
        //    if (!string.IsNullOrEmpty(GenreSearch))
        //    {
        //        query = query.Where(jp => jp.Genre.GenreName.Contains(GenreSearch)); // Assuming Genre is a navigation property with a Name field
        //    }

        //    List<Movie> SelectedMovies = query.Include(jp => jp.Genre).ToList();

        //    // Populate the ViewBag with the count of all movies
        //    ViewBag.AllMovies = _context.Movies.Count();

        //    // Populate the ViewBag with the count of selected movies
        //    ViewBag.SelectedMovies = SelectedMovies.Count();

        //    return View(SelectedMovies.OrderByDescending(jp => jp.StartTime));
        //}

        [HttpGet]
        public async Task<IActionResult> DetailedSearch(MovieSearchViewModel searchModel)
        {
            if (!ModelState.IsValid)
            {
                return View(searchModel);
            }

            var query = _context.Movies.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchModel.MovieTitle))
            {
                query = query.Where(m => m.MovieTitle.Contains(searchModel.MovieTitle));
            }
            if (!string.IsNullOrWhiteSpace(searchModel.MovieTagline))
            {
                query = query.Where(m => m.MovieTagline.Contains(searchModel.MovieTagline));
            }
            if (!string.IsNullOrWhiteSpace(searchModel.Actors))
            {
                var actorSearch = searchModel.Actors.ToLower();
                query = query.Where(m => m.Actors.ToLower().Contains(actorSearch));

            }

            //if (!string.IsNullOrWhiteSpace(searchModel.Genre.GenreName))
            //{
            //    query = query.Where(m => m.Genre.GenreName.Contains(searchModel.Genre.GenreName));
            //}

            if (!string.IsNullOrWhiteSpace(searchModel.MPAARating))
            {
                query = query.Where(m => m.MPAARating.Equals(searchModel.MPAARating));
            }
            if (searchModel.CustomerRating.HasValue)
            {
                if (searchModel.RatingGreaterThan)
                {
                    query = query.Where(m => m.CustomerRating > searchModel.CustomerRating.Value);
                }
                else
                {
                    query = query.Where(m => m.CustomerRating < searchModel.CustomerRating.Value);
                }
            }


            query = query.Include(m => m.Genre);

            searchModel.Results = await query.ToListAsync();
            searchModel.TotalMovies = _context.Movies.Count();

            return View(searchModel);
        }






        [HttpPost]
        public IActionResult DisplaySearchResults(MovieSearchViewModel searchCriteria)
        {
            // Ensure the searchCriteria is valid
            if (searchCriteria == null)
            {
                return View("Error", new String[] { "Search criteria not provided." });
            }

            var query = _context.Movies.AsQueryable();

            // Apply filters based on search criteria
            if (!string.IsNullOrWhiteSpace(searchCriteria.MovieTitle))
            {
                query = query.Where(m => m.MovieTitle.Contains(searchCriteria.MovieTitle));
            }
            if (!string.IsNullOrWhiteSpace(searchCriteria.MovieTagline))
            {
                query = query.Where(m => m.MovieTagline.Contains(searchCriteria.MovieTagline));
            }

            if (searchCriteria.GenreID.HasValue && searchCriteria.GenreID.Value > 0)
            {
                query = query.Where(jp => jp.Genre.GenreID == searchCriteria.GenreID.Value);
            }


            if (!string.IsNullOrWhiteSpace(searchCriteria.Actors))
            {
                query = query.Where(m => m.Actors.Contains(searchCriteria.Actors));
            }

            if (!string.IsNullOrWhiteSpace(searchCriteria.MPAARating))
            {
                query = query.Where(m => m.MPAARating.Equals(searchCriteria.MPAARating));
            }
            if (searchCriteria.CustomerRating.HasValue)
            {
                query = searchCriteria.RatingGreaterThan
                    ? query.Where(m => m.CustomerRating > searchCriteria.CustomerRating.Value)
                    : query.Where(m => m.CustomerRating < searchCriteria.CustomerRating.Value);
            }

            // Include necessary navigation properties
            query = query.Include(m => m.Genre);

            var selectedMovies = query.Include(jp => jp.Genre).ToList();

            ViewBag.AllMovies = _context.Movies.Count();
            ViewBag.SelectedMovies = selectedMovies.Count();

            // Populate additional ViewModel properties if necessary
            searchCriteria.Results = selectedMovies;
            searchCriteria.TotalMovies = ViewBag.AllMovies;

            // Return the Index view with the filtered list of movies
            // You can pass the searchCriteria to the view if you want to display the search criteria as well
            return View("Index", selectedMovies);
        }

        //create select list for genre
        //make sure method for detailed search that i implement view bag (model state valid- resend viewbag if not valid)
        //padd viewbag to the view - detailed search view 
        //chnage form type to detailed search view to dropdown list 
    }
}

