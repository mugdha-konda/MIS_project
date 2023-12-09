using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Group7_FinalProject.DAL;
using Group7_FinalProject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

//TODO: it won't let me actually create an order. i also  need to consider  how to model bind in schedule as well! start here.
namespace Group7_FinalProject.Controllers
{
    public class OrderDetailsController : Controller
    {
        private readonly AppDbContext _context;

        public OrderDetailsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RegistrationDetails
        public IActionResult Index(int? orderID)
        {
            if (orderID == null)
            {
                return View("Error", new String[] { "Please specify a order to view!" });
            }

            //limit the list to only the registration details that belong to this registration
            List<OrderDetail> ods = _context.OrderDetails
                                          .Include(od => od.MovieTitle)
                                          .Where(od => od.Order.OrderID == orderID)
                                          .ToList();

            return View(ods);
        }

        // GET: OrderDetails/Details/5  - FIGURE OUT
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.OrderDetails == null)
            {
                return NotFound();
            }

            var orderDetail = await _context.OrderDetails
                .FirstOrDefaultAsync(m => m.OrderDetailID == id);
            if (orderDetail == null)
            {
                return NotFound();
            }

            return View(orderDetail);
        }

        // GET: OrderDetails/Create
        public IActionResult Create(int orderID)
        {
            //create a new instance of the RegistrationDetail class
            OrderDetail rd = new OrderDetail();

            //find the registration that should be associated with this registration
            Order dbOrder = _context.Orders.Find(orderID);

            if (dbOrder.OrderStatus != OrderStatus.Pending)
            {
                return View("Error", new String[] { "You can only add a movie if you've started the order!" });
            }
            //set the new registration detail's registration equal to the registration you just found
            rd.Order = dbOrder;

            //populate the ViewBag with a list of existing courses
            ViewBag.AllMovies = GetAllMovies();

            //pass the newly created registration detail to the view
            return View(rd);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Order,OrderDetailID,Quantity")] OrderDetail orderDetail, DateTime SelectedSchedule)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AllMovies = GetAllMovies();
                return View(orderDetail);
            }

            // Find the schedule to be associated with this order detail
            Schedule dbSchedule = await _context.Schedules
                                                .Include(s => s.TicketType) // Include the TicketType navigation property
                                                .FirstOrDefaultAsync(s => s.StartTime == SelectedSchedule);

            //if (dbSchedule == null)
            //{
            //    return View("Error", new String[] { "The selected schedule was not found." });
            //}

            // Find the order in the database
            Order dbOrder = await _context.Orders.FindAsync(orderDetail.Order.OrderID);

            if (dbOrder == null)
            {
                return View("Error", new String[] { "The order was not found." });
            }

            if (dbOrder.OrderStatus != OrderStatus.Pending)
            {
                return View("Error", new String[] { "You can only add movies to pending orders." });
            }

            // Set the order detail's schedule and order

            orderDetail.Schedule = dbSchedule;
            orderDetail.Order = dbOrder;
            orderDetail.Order.User = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);

            // Calculate the ticket price based on the schedule and any applicable discounts
            //orderDetail.TicketPrice = dbSchedule.TicketType.TicketPrice;

            // Calculate the extended price
            //orderDetail.TotalPrice = orderDetail.Order.Quantity * orderDetail.TicketPrice;

            // Add the order detail to the database
            _context.Add(orderDetail);
            await _context.SaveChangesAsync();



            // Redirect to the details page for the order
            return RedirectToAction("Checkout", "OrderDetails", new { id = orderDetail.Order.OrderID });
        }

        //GET
        public IActionResult Checkout(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Order order = _context.Orders
                                  .Include(r => r.OrderDetails)
                                  .ThenInclude(r => r.Schedule)
                                  .Include(r => r.User)
                                  .FirstOrDefault(m => m.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderDetails.Count == 0)
            {
                return View("Error", new String[] { "You cannot checkout with an empty order!" });
            }

            if (order.OrderStatus != OrderStatus.Pending)
            {
                return View("Error", new String[] { "This order has already been placed!" });
            }
            return View(order);


        }

        //POST
        [HttpPost, ActionName("Checkout")]
        [ValidateAntiForgeryToken]
        public IActionResult CheckoutInfo(int id, Order order, OrderDetail orderDetails, Schedule dbSchedule, PaymentMethod paymentMethod, TicketType ticketType)
        {
            if (id != order.OrderID)
            {
                return View("Error", new String[] { "There was a problem checking out this order. Try again!" });
            }

            //Calculate the ticket price based on the schedule and any applicable discounts


            if (ModelState.IsValid == false)
            {
                return View(order);
            }

            try
            {
                //find the record in the database
                Order dbOrder = _context.Orders
                      .Include(r => r.OrderDetails)
                      .ThenInclude(r => r.Schedule)
                      .Include(r => r.User)
                      .FirstOrDefault(m => m.OrderID == order.OrderID);
                if (User.IsInRole("Customer") && dbOrder.User.UserName != User.Identity.Name)
                {
                    return View("Error", new String[] { "You are not authorized to checkout this order!" });
                }

                //dbOrder.OrderDetails.PaymentMethod = _context.OrderDetails.FirstOrDefault(c => c.PaymentMethod == PaymentMethod.CashCredit);
                _context.Update(dbOrder);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was an error updating this order!", ex.Message });
            }

            return RedirectToAction("CheckoutConfirmation", new { id = order.OrderID });
        }

        //GET
        public IActionResult CheckoutConfirmation(int id)
        {
            if (id == null)
            {
                return NotFound();
            }


            Order order = _context.Orders
                                  .Include(r => r.OrderDetails)
                                  .ThenInclude(r => r.Schedule)
                                  .Include(r => r.User)
                                  .FirstOrDefault(m => m.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderDetails.Count == 0)
            {
                return View("Error", new String[] { "You cannot checkout with an empty order!" });
            }

            if (order.OrderStatus != OrderStatus.Pending)
            {
                return View("Error", new String[] { "This order has already been placed!" });
            }
            return View(order);
        }

        //POST
        [HttpPost, ActionName("CheckoutConfirmation")]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteCheckout(int id)
        {
            try
            {
                //find the record in the database
                Order dbOrder = _context.Orders
                      .Include(r => r.OrderDetails)
                      .ThenInclude(r => r.Schedule)
                      .Include(r => r.User)
                      .FirstOrDefault(m => m.OrderID == id);

                dbOrder.OrderStatus = OrderStatus.Purchased;

                foreach (OrderDetail od in dbOrder.OrderDetails)
                {
                    Schedule dbSchedule = _context.Schedules.Find(od.Schedule.StartTime);

                    _context.Update(dbOrder);
                    _context.SaveChanges();

                }
            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was an error updating this order!", ex.Message });
            }

            return RedirectToAction("AfterCheckout", new { orderID = id });
        }
        //private decimal CalculateTicketPrice(Schedule schedule, AppUser user)
        //{
        //    // Calculate age from birthdate
        //    // Calculate age from birthdate
        //    int? age = null; // Age is nullable, in case we don't have a birthday to calculate it from

        //    // Calculate age from birthdate, only if Birthday has a value
        //    if (user.Birthday.HasValue)
        //    {
        //        DateTime today = DateTime.Today;
        //        DateTime birthday = user.Birthday.Value;
        //        age = today.Year - birthday.Year;

        //        // Subtract another year if the current date is before the birthday this year
        //        if (birthday > today.AddYears(-age.Value))
        //        {
        //            age--;
        //        }
        //    }

        //    TicketType ticketType = TicketType.WeekdayBase; // default TicketType

        //    if ((bool)schedule.SpecialEvent)
        //    {
        //        ticketType = TicketType.SpecialEvent;
        //    }
        //    else if (schedule.StartTime.DayOfWeek != DayOfWeek.Saturday &&
        //             schedule.StartTime.DayOfWeek != DayOfWeek.Sunday &&
        //             schedule.StartTime.TimeOfDay < new TimeSpan(12, 0, 0))
        //    {
        //        ticketType = TicketType.Matinee;
        //    }
        //    else if (schedule.StartTime.DayOfWeek == DayOfWeek.Tuesday &&
        //             schedule.StartTime.TimeOfDay >= new TimeSpan(12, 0, 0) &&
        //             schedule.StartTime.TimeOfDay < new TimeSpan(17, 0, 0))
        //    {
        //        ticketType = TicketType.DiscountTuesday;
        //    }
        //    else if (schedule.StartTime.DayOfWeek == DayOfWeek.Friday &&
        //             schedule.StartTime.TimeOfDay >= new TimeSpan(12, 0, 0) ||
        //             schedule.StartTime.DayOfWeek == DayOfWeek.Saturday ||
        //             schedule.StartTime.DayOfWeek == DayOfWeek.Sunday)
        //    {
        //        ticketType = TicketType.Weekends;
        //    }
        //    // Check for senior discount
        //    if (age >= 60)
        //    {
        //        schedule.TicketType.TicketPrice -= 2.00m; // Senior discount
        //    }

        //    // Find the price in the database
        //    Price price = _context.Prices.FirstOrDefault(p => p.TicketTypes == ticketType);

        //    // Return the price
        //    return price?.TicketPrice ?? 0; // If there's no matching price, return 0 or handle as needed
        //}

        // GET: OrderDetails/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //user did not specify a registration detail to edit
            if (id == null)
            {
                return View("Error", new String[] { "Please specify an order detail to edit!" });
            }

            //find the registration detail
            OrderDetail orderDetail = await _context.OrderDetails
                                                   .Include(rd => rd.MovieTitle)
                                                   .Include(rd => rd.Order)
                                                   .FirstOrDefaultAsync(rd => rd.OrderDetailID == id);
            if (orderDetail == null)
            {
                return View("Error", new String[] { "This order detail was not found" });
            }

            if (orderDetail.Order.OrderStatus != OrderStatus.Purchased)
            {
                return View("Error", new String[] { "You cannot edit completed orders!" });
            }
            return View(orderDetail);
        }

        // POST: OrderDetails/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderDetailID,Quantity,MoviePrice,TotalPrice")] OrderDetail orderDetail)
        {
            //this is a security check to make sure they are editing the correct record
            if (id != orderDetail.OrderDetailID)
            {
                return View("Error", new String[] { "There was a problem editing this record. Try again!" });
            }

            //information is not valid, try again
            if (ModelState.IsValid == false)
            {
                return View(orderDetail);
            }

            if (orderDetail.Order.Quantity < 1)
            {
                return View("Error", new String[] { "You must order at least 1 copy!" });
            }

            //create a new registration detail
            OrderDetail dbRD;
            //if code gets this far, update the record
            try
            {
                //find the existing registration detail in the database
                //include both registration and course
                dbRD = _context.OrderDetails
                      .Include(rd => rd.MovieTitle)
                      .Include(rd => rd.Order)
                      .FirstOrDefault(rd => rd.OrderDetailID == orderDetail.OrderDetailID);

                //update the scalar properties
                //dbRD.Quantity = dbRD.Order.Quantity;
                dbRD.TicketPrice = dbRD.Schedule.TicketType.TicketPrice;
                dbRD.TotalPrice = dbRD.Order.Quantity * dbRD.TicketPrice;

                if (dbRD.Order.Quantity < 1)
                {
                    return View("Error", new String[] { "You must select atleast one movie ticket" });
                }

                //save changes
                _context.Update(dbRD);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was a problem editing this record", ex.Message });
            }

            //if code gets this far, go back to the registration details index page
            return RedirectToAction("Details", "Orders", new { id = dbRD.Order.OrderID });
        }

        // GET: OrderDetails/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //user did not specify a registration detail to delete
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a order detail to delete!" });
            }

            //find the registration detail in the database
            OrderDetail orderDetail = await _context.OrderDetails
                                                    .Include(r => r.Order)
                                                   .FirstOrDefaultAsync(m => m.OrderDetailID == id);

            //registration detail was not found in the database
            if (orderDetail == null)
            {
                return View("Error", new String[] { "This order detail was not in the database!" });
            }

            //send the user to the delete confirmation page
            return View(orderDetail);
        }

        // POST: OrderDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //find the registration detail to delete
            OrderDetail orderDetail = await _context.OrderDetails
                                                   .Include(r => r.Order)
                                                   .FirstOrDefaultAsync(r => r.OrderDetailID == id);

            //delete the registration detail
            _context.OrderDetails.Remove(orderDetail);
            await _context.SaveChangesAsync();

            //return the user to the registration/details page
            return RedirectToAction("Details", "Orders", new { id = orderDetail.Order.OrderID });
        }


        private SelectList GetAllMovies()
        {
            //create a list for all the courses
            List<Movie> allMovies = _context.Movies.ToList();

            //the user MUST select a course, so you don't need a dummy option for no course

            //use the constructor on select list to create a new select list with the options
            SelectList slAllMovies = new SelectList(allMovies, nameof(Movie.MovieID), nameof(Movie.MovieTitle));

            return slAllMovies;
        }

        private bool OrderDetailExists(int id)
        {
            return _context.OrderDetails.Any(e => e.OrderDetailID == id);
        }
    }
}
