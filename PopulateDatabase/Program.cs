using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using Assignment3;

namespace PopulateDatabase
{
    public class Program
    {
        private static AppDbContext database;

        public static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            database = new AppDbContext();

            // Clear the database.

            var tickets = database.Tickets.ToList();
            database.Tickets.RemoveRange(tickets);

            var screenings = database.Screenings.ToList();
            database.Screenings.RemoveRange(screenings);

            var movies = database.Movies.ToList();
            database.Movies.RemoveRange(movies);

            var cinemas = database.Cinemas.ToList();
            database.Cinemas.RemoveRange(cinemas);

            database.SaveChanges();

            // Load movies.
            string[] movieLines = File.ReadAllLines("SampleMovies.csv");
            foreach (string line in movieLines)
            {
                string[] parts = line.Split(',');
                string title = parts[0];
                string releaseDateString = parts[1];
                string runtimeString = parts[2];
                string posterPath = parts[3];

                int releaseYear = int.Parse(releaseDateString.Split('-')[0]);
                int releaseMonth = int.Parse(releaseDateString.Split('-')[1]);
                int releaseDay = int.Parse(releaseDateString.Split('-')[2]);
                var releaseDate = new DateTime(releaseYear, releaseMonth, releaseDay);

                int runtime = int.Parse(runtimeString);

                Movie movie = new Movie();
                movie.Title = title;
                movie.ReleaseDate = releaseDate;
                movie.Runtime = (short)runtime;
                movie.PosterPath = posterPath;

                database.Add(movie);
                database.SaveChanges();

            }

            // Load cinemas.
            string[] cinemaLines = File.ReadAllLines("SampleCinemasWithPositions.csv");
            foreach (string line in cinemaLines)
            {
                string[] parts = line.Split(',');
                string city = parts[0];
                string name= parts[1];
                string latitude = parts[2];
                string longitude = parts[3];

                Cinema cinema = new Cinema();
                cinema.City = city;
                cinema.Name = name;
                cinema.Latitude = float.Parse(latitude);
                cinema.Longitude = float.Parse(longitude);

                database.Add(cinema);
                database.SaveChanges();

            }

            // Generate random screenings.

            // Get all Cinemas.
            var cinemasList = database.Cinemas.ToList();

            // Get all Movies.
            var moviesList = database.Movies.ToList();

            // Create random screenings for each cinema.
            var random = new Random();
            foreach (Cinema cinema in cinemasList)
            {
                // Choose a random number of screenings.
                int numberOfScreenings = random.Next(2, 6);
                foreach (int n in Enumerable.Range(0, numberOfScreenings)) {
                    // Pick a random movie.
                    Movie movie = moviesList[random.Next(moviesList.Count)];

                    // Pick a random hour and minute.
                    int hour = random.Next(24);
                    double[] minuteOptions = { 0, 10, 15, 20, 30, 40, 45, 50 };
                    double minute = minuteOptions[random.Next(minuteOptions.Length)];
                    var time = TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);

                    // Insert the screening into the Screenings table.
                    var screening = new Screening();
                    screening.Cinema = cinema;
                    screening.Movie = movie;
                    screening.Time = time;

                    database.Add(screening);
                    database.SaveChanges();
                }
            }
        }
    }
}
