using MovieCatalog.Models;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace MovieCatalog.Tests
{
    [TestFixture]
    public class MovieCatalogApiTests
    {
        private RestClient client = default!;
        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string LoginEmail = "some@email.com";
        private const string LoginPassword = "Qwe_12345";

        private static string? lastCreatedMovieId;
        private static string Email =>
        Environment.GetEnvironmentVariable("MOVIE_EMAIL")
        ?? LoginEmail;

        private static string Password =>
        Environment.GetEnvironmentVariable("MOVIE_PASSWORD")
        ?? LoginPassword;


        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var jwtToken = AuthenticateAndGetJwtToken(Email, Password);


            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }


        private static string AuthenticateAndGetJwtToken(string email, string password)
        {
            var temp = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post)
            .AddJsonBody(new { email, password });


            var response = temp.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException($"Auth failed: {response.StatusCode} {response.Content}");


            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            var token = json.GetProperty("accessToken").GetString();
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("JWT access token missing in response.");


            return token!;
        }

        [Test, Order(1)]
        public void CreateMovie_WithRequiredFields_ShouldReturnOkAndMessage()
        {
            var body = new MovieDTO
            {
                Title = $"Test Movie {Guid.NewGuid():N}",
                Description = "Some description here"
            };


            var request = new RestRequest("/api/Movie/Create", Method.Post)
            .AddJsonBody(body);


            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected 200 OK on create.");


            var api = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);
            Assert.That(api, Is.Not.Null);
            Assert.That(api!.Msg, Is.EqualTo("Movie created successfully!"));

            lastCreatedMovieId = api!.Movie.Id;
        }

        [Test, Order(2)]
        public void EditExistingMovie_ShouldReturnOkAndEditedMessage()
        {
            var edit = new MovieDTO
            {
                Title = "Edited Movie",
                Description = "Edited description"
            };


            var request = new RestRequest("/api/Movie/Edit", Method.Put)
            .AddQueryParameter("movieId", lastCreatedMovieId)
            .AddJsonBody(edit);


            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");


            var api = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);
            Assert.That(api, Is.Not.Null);
            Assert.That(api!.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Test, Order(3)]
        public void GetAllMovies_ShouldReturnNonEmptyArray_AndCaptureLastId()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected 200 OK on get all.");

            var items = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content!);
            Assert.That(items, Is.Not.Null);
            Assert.That(items!, Is.Not.Empty);
        }

        [Test, Order(4)]
        public void DeleteExistingMovie_ShouldReturnOkAndDeletedMessage()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete)
            .AddQueryParameter("movieId", lastCreatedMovieId);


            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");


            var api = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);
            Assert.That(api, Is.Not.Null);
            Assert.That(api!.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateMovie_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var bad = new MovieDTO
            {
                Title = string.Empty,
                Description = string.Empty
            };
            var request = new RestRequest("/api/Movie/Create", Method.Post).AddJsonBody(bad);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest.");
        }

        [Test, Order(6)]
        public void EditNonExistingMovie_ShouldReturnBadRequest_AndMessage()
        {
            string fakeId = "123";
            var edit = new MovieDTO
            {
                Title = "Edited Revue",
                Description = "Edited description"
            };


            var request = new RestRequest("/api/Movie/Edit", Method.Put)
            .AddQueryParameter("movieId", fakeId)
            .AddJsonBody(edit);


            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest.");
            Assert.That(response.Content, Does.Contain("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Test, Order(7)]
        public void DeleteNonExistingMovie_ShouldReturnBadRequest_AndMessage()
        {
            string fakeId = "123";
            var request = new RestRequest("/api/Movie/Delete", Method.Delete)
            .AddQueryParameter("movieId", fakeId);

            var response = client.Execute(request);
            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest.");
            Assert.That(response.Content, Does.Contain("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            client?.Dispose();
        }
    }
}