using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Group7_FinalProject.DAL;
using Group7_FinalProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Group7_FinalProject.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ReviewsController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reviews
        public async Task<IActionResult> Index(int? id)
        {
            //Set up a list of registrations to display
            List<Review> reviews;
            if ((User.IsInRole("Admin") || User.IsInRole("Employee")) && id == null)
            {
                reviews = _context.Reviews
                                .Include(r => r.User)
                                .Include(r => r.MovieTitle)
                                .ToList();
            }
            else if (User.IsInRole("Customer") && id == null) //user is a customer, so only display their records
            {
                reviews = _context.Reviews
                                .Where(r => r.User.UserName == User.Identity.Name)
                                .Include(r => r.User)
                                .Include(r => r.MovieTitle)
                                .ToList();
            }

            else
            {
                reviews = _context.Reviews
                                .Where(r => r.MovieTitle.MovieID == id)
                                .Include(r => r.User)
                                .Include(r => r.MovieTitle)
                                .ToList();
            }

            //
            return View(reviews);
        }

        // GET: Reviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a review to view!" });
            }

            var review = await _context.Reviews
                                      .Include(r => r.User)
                                      .Include(r => r.MovieTitle)
                                      .FirstOrDefaultAsync(m => m.ReviewID == id);

            if (review == null)
            {
                return View("Error", new String[] { "This review was not found!" });
            }

            return View(review);
        }

        // GET: Reviews/Create
        [Authorize(Roles = "Customer")]
        public IActionResult Create(int MovieID)
        {
            //create a new instance of the RegistrationDetail class
            Review rd = new Review();

            //find the registration that should be associated with this registration
            Movie dbMovie = _context.Movies.Find(MovieID);

            //set the new registration detail's registration equal to the registration you just found
            rd.MovieTitle = dbMovie;

            //pass the newly created registration detail to the view
            return View(rd);
        }

        // POST: Reviews/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReviewID,Stars,ReviewDescription,ApprovalStatus")] Review review, int? id)
        {
            if (review.Stars < 1 || review.Stars > 5)
            {
                return View("Error", new String[] { "You can only rate a movie from 1 to 5!" });
            }

            if (_context.Reviews.Include(r => r.MovieTitle).Include(r => r.User).Where(r => r.MovieTitle.MovieID == id).FirstOrDefault(u => u.User.UserName == User.Identity.Name) != null)
            {
                return View("Error", new String[] { "You've already reviewed this movie!" });
            }

            //Associate the registration with the logged-in customer
            review.User = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            review.MovieTitle = _context.Movies.FirstOrDefault(u => u.MovieID == id);
            review.ApprovalStatus = ReviewStatus.Pending;
            //make sure all properties are valid
            if (ModelState.IsValid == false)
            {
                return View(review);
            }

            //if code gets this far, add the registration to the database
            _context.Add(review);
            await _context.SaveChangesAsync();

            //send the user on to the action that will allow them to 
            //create a registration detail.  Be sure to pass along the RegistrationID
            //that you created when you added the registration to the database above
            return RedirectToAction("Index", "Movies");
        }

        [Authorize(Roles = "Employee, Admin")]
        // GET: Reviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //user did not specify a registration to edit
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a review to edit" });
            }

            //find the registration in the database, and be sure to include details
            Review review = _context.Reviews
                                       .Include(r => r.MovieTitle)
                                       .ThenInclude(r => r.Reviews)
                                       .Include(r => r.User)
                                       .FirstOrDefault(r => r.ReviewID == id);

            //registration was nout found in the database
            if (review == null)
            {
                return View("Error", new String[] { "This review was not found in the database!" });
            }

            //registration does not belong to this user
            if (User.IsInRole("Customer") && review.User.UserName != User.Identity.Name)
            {
                return View("Error", new String[] { "You are not authorized to edit this review!" });
            }

            //send the user to the registration edit view
            return View(review);
        }

        // POST: Reviews/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReviewID,Stars,ReviewDescription,ApprovalStatus")] Review review)
        {
            //this is a security measure to make sure the user is editing the correct registration
            if (id != review.ReviewID)
            {
                return View("Error", new String[] { "There was a problem editing this review. Try again!" });
            }

            //if there is something wrong with this order, try again
            if (ModelState.IsValid == false)
            {
                return View(review);
            }

            //if code gets this far, update the record
            try
            {
                //find the record in the database
                Review dbReview = _context.Reviews.Include(r => r.MovieTitle).FirstOrDefault(r => r.ReviewID == review.ReviewID);

                //update the notes
                dbReview.Stars = review.Stars;
                dbReview.ReviewDescription = review.ReviewDescription;
                dbReview.ApprovalStatus = review.ApprovalStatus;

                if (dbReview.ApprovalStatus == ReviewStatus.Approved)
                {
                    dbReview.ApprovalUserName = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name).UserName;
                }

                _context.Update(dbReview);

                await _context.SaveChangesAsync();

                Decimal avgRating = GetAverageRating(dbReview.MovieTitle.MovieID);
                Movie dbMovie = _context.Movies.FirstOrDefault(r => r.MovieID == dbReview.MovieTitle.MovieID);
                dbMovie.CustomerRating = avgRating;

                _context.Update(dbMovie);
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was an error updating this review!", ex.Message });
            }

            //send the user to the Registrations Index page.
            return RedirectToAction(nameof(Review.ApprovalStatus)); //double check this one? 
        }

        // Helper method to check if a Review exists
        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.ReviewID == id);
        }

        // GET: Reviews/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .FirstOrDefaultAsync(m => m.ReviewID == id);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public Decimal GetAverageRating(int id)
        {
            Int32 NumberOfReviews = _context.Reviews.Where(u => u.MovieTitle.MovieID == id).Where(u => u.ApprovalStatus == ReviewStatus.Approved).Count();
            Decimal RatingSum = _context.Reviews.Where(u => u.MovieTitle.MovieID == id).Where(u => u.ApprovalStatus == ReviewStatus.Approved).Sum(u => u.Stars);
            if (NumberOfReviews > 0)
            {
                return RatingSum / NumberOfReviews;
            }

            else
            {
                return 0;
            }
        }
    }
}
