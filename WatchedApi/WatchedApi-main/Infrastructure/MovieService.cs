using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;

public class MovieService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private const string ApiKey = "5e42dc53";

    public MovieService(ApplicationDbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task<List<Movie>> FetchMoviesFromAPI()
    {
        var moviesFetch = new List<string>
        {
            //list of the 30 movies that will be fetched
            "Inception", "Interstellar", "The Dark Knight", "Titanic", "Avatar",
            "The Matrix", "Gladiator", "The Godfather", "The Shawshank Redemption", "Pulp Fiction",
            "Forrest Gump", "Fight Club", "The Lord of the Rings", "The Avengers", "Iron Man",
            "The Lion King", "Star Wars", "Jurassic Park", "Harry Potter", "Deadpool",
            "Spider-Man", "The Batman", "Dune", "Doctor Strange", "Black Panther",
            "Joker", "The Prestige", "Mad Max: Fury Road", "Shutter Island", "Parasite"
        };

        //create new list 
        var moviesSaved = new List<Movie>();

        foreach (var movieName in moviesFetch)
        {
            //get data from that link
            var response = await _httpClient.GetAsync($"https://www.omdbapi.com/?t={movieName}&apikey={ApiKey}");

            //read the string
            var json = await response.Content.ReadAsStringAsync();
            //deserialize it with the model given below
            var apiMovie = JsonSerializer.Deserialize<OmdbMovieResponse>(json);

            var movie = new Movie
            {
                Title = apiMovie.Title,
                Year = apiMovie.Year,
                Genre = apiMovie.Genre,
                Director = apiMovie.Director,
                PosterUrl = apiMovie.Poster,
                Actors = apiMovie.Actors,
                Plot = apiMovie.Plot
            };

            _context.Movies.Add(movie);
            moviesSaved.Add(movie);
        }

        await _context.SaveChangesAsync();
        return moviesSaved;
    }

    //to add and recalc average
    public async Task<bool> RateMovie(int userId, int movieId, int rating)
    {
        //pull movie id
        var movie = await _context.Movies.FindAsync(movieId);
        if (movie == null) return false;

        //check if the user has already rated this movie
        bool alreadyRated = await _context.MovieRatings
            .AnyAsync(mr => mr.UserId == userId && mr.MovieId == movieId);

        if (alreadyRated)
        {
            return false;
        }

        //save new rating
        var newRating = new MovieRating
        {
            UserId = userId,
            MovieId = movieId,
            Rating = rating
        };

        _context.MovieRatings.Add(newRating);
        await _context.SaveChangesAsync();

        //recalculate and update the movie's average rating
        double? newAverage = await _context.MovieRatings
            .Where(mr => mr.MovieId == movieId)
            .Select(mr => (double?)mr.Rating)
            .AverageAsync();

        //make average rating equal the new average
        movie.AverageRating = newAverage ?? 0;

        await _context.SaveChangesAsync();
        return true;
    }


}

public class OmdbMovieResponse
{
    public string Title { get; set; }
    public string Year { get; set; }
    public string Genre { get; set; }
    public string Director { get; set; }
    public string Poster { get; set; }
    public string Actors { get; set; }
    public string Plot { get; set; }

}
