using SimpleApp.Pages;
using SimpleApp.Models;
using SimpleApp.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Moq;
using SimpleApp.Services;
using System.Net.Http;
using Moq.Protected;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;

public class Tests
{
    [Fact]
    public void CalculateOdds_ShouldReturnBalancedOdds_WhenTeamsAreEvenlyMatched()
    {

        var placeBetModel = new PlaceBetModel(null, null, null);

        var homeTeam = new Team { Name = "Tottenham Hotspur FC", TeamRatingScale = 7 };
        var awayTeam = new Team { Name = "Manchester United FC", TeamRatingScale = 7 };


        var result = placeBetModel.CalculateOdds(homeTeam, awayTeam);


        Assert.Equal(2.0, result.HomeWin);
        Assert.Equal(4.0, result.Draw);
        Assert.Equal(3.0, result.AwayWin);
    }

    [Fact]
    public void CalculateOdds_ShouldReturnHomeFavoriteOdds_WhenHomeTeamIsBetter()
    {

        var placeBetModel = new PlaceBetModel(null, null, null);

        var homeTeam = new Team { Name = "Manchester City FC", TeamRatingScale = 10 };
        var awayTeam = new Team { Name = "Aston Villa FC", TeamRatingScale = 6 };


        var result = placeBetModel.CalculateOdds(homeTeam, awayTeam);


        Assert.Equal(2.0, result.HomeWin);
        Assert.Equal(3.0, result.Draw);
        Assert.Equal(5.0, result.AwayWin);
    }

    [Fact]
    public void CalculateOdds_ShouldReturnAwayFavoriteOdds_WhenAwayTeamIsBetter()
    {

        var placeBetModel = new PlaceBetModel(null, null, null);

        var homeTeam = new Team { Name = "Chelsea", TeamRatingScale = 7 };
        var awayTeam = new Team { Name = "Liverpool FC", TeamRatingScale = 10 };


        var result = placeBetModel.CalculateOdds(homeTeam, awayTeam);


        Assert.Equal(5.0, result.HomeWin);
        Assert.Equal(4.0, result.Draw);
        Assert.Equal(3.0, result.AwayWin);
    }
}
public class LeaderBoardModelTests
{
    private ApplicationDbContext GetTestDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        return new ApplicationDbContext(options);
    }
    [Fact]
    public async Task LeaderBoardModel_OnGetAsync_ReturnsUsersOrderedByPoints()
    {

        var dbContext = GetTestDbContext();

        dbContext.Users.AddRange(
      new User { Id = "1", UserName = "Vini", Points = 40, FirstName = "Vinicius" },
      new User { Id = "2", UserName = "Regex", Points = 87, FirstName = "Reginald" },
      new User { Id = "3", UserName = "Rasmus", Points = 330, FirstName = "Rasmus" }
  );

        await dbContext.SaveChangesAsync();

        var leaderboardModel = new LeaderBoardModel(dbContext);


        await leaderboardModel.OnGetAsync();


        Assert.NotNull(leaderboardModel.Users);
        Assert.Equal(3, leaderboardModel.Users.Count);


        var expectedOrder = new[] { "Rasmus", "Regex", "Vini" };
        var actualOrder = leaderboardModel.Users.Select(u => u.UserName).ToArray();

        Assert.Equal(expectedOrder, actualOrder);
    }
}

public class FootballDataServiceTests
    {
        [Fact]
        public async Task GetTeamsAsync_ShouldAssignTeamRatingScaleToAllTeams()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            var fakeTeamsResponse = new
            {
                teams = new[]
                {
                new { id = 1, name = "Real Madrid CF" },
                new { id = 2, name = "Liverpool FC" }
            }
            };

            var jsonResponse = JsonConvert.SerializeObject(fakeTeamsResponse);

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var service = new FootballDataService(httpClient);

            // Act
            var teams = await service.GetTeamsAsync();
            var teamsList = teams
            .GroupBy(team => team.Name)
            .Select(group => group.First())
            .ToList();
        
            // Assert
            Assert.NotNull(teamsList);
            Assert.NotEmpty(teamsList);
            Assert.All(teamsList, team => Assert.NotNull(team.TeamRatingScale));
            Assert.Contains(teamsList, team => team.Name == "Liverpool FC" && team.TeamRatingScale == 10);
        }
    
}