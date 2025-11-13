using EolBot.Models;
using EolBot.Repositories;
using MockQueryable;
using Moq;

namespace EolBot.Tests.Repositories
{
    public class DatabaseUserRepositoryTests
    {
        private readonly List<User> _testUsers;

        private readonly DatabaseUserRepository _repository;

        public DatabaseUserRepositoryTests()
        {
            _testUsers = [
                new User
                {
                    TelegramId = 1012345678,
                    IsActive = true,
                    SubscribedAt = new DateTime(2025, 07, 10, 12, 0, 0),
                    CreatedAt = new DateTime(2025, 06, 23, 12, 0, 0)
                },
                new User
                {
                    TelegramId = 1098765432,
                    IsActive = false,
                    CreatedAt = new DateTime(2025, 05, 20, 12, 0, 0)
                },
                new User
                {
                    TelegramId = 1023456789,
                    IsActive = true,
                    SubscribedAt = new DateTime(2025, 07, 22, 12, 0, 0),
                    CreatedAt = new DateTime(2025, 07, 22, 12, 0, 0)
                },
                new User
                {
                    TelegramId = 1034567890,
                    IsActive = true,
                    SubscribedAt = new DateTime(2025, 07, 08, 12, 0, 0),
                    CreatedAt = new DateTime(2025, 07, 03, 12, 0, 0)
                },
                new User
                {
                    TelegramId = 1045678901,
                    IsActive = false,
                    CreatedAt = new DateTime(2025, 03, 10, 12, 0, 0)
                },
                new User
                {
                    TelegramId = 1056789012,
                    IsActive = true,
                    SubscribedAt = new DateTime(2025, 07, 18, 12, 0, 0),
                    CreatedAt = new DateTime(2025, 07, 13, 12, 0, 0)
                },
                new User
                {
                    TelegramId = 1067890123,
                    IsActive = false,
                    CreatedAt = new DateTime(2024, 12, 01, 12, 0, 0)
                },
                new User
                {
                    TelegramId = 1078901234,
                    IsActive = true,
                    SubscribedAt = new DateTime(2025, 07, 20, 12, 0, 0),
                    CreatedAt = new DateTime(2025, 07, 20, 12, 0, 0)
                },
                new User
                {
                    TelegramId = 1089012345,
                    IsActive = false,
                    CreatedAt = new DateTime(2024, 09, 25, 12, 0, 0)
                },
                new User
                {
                    TelegramId = 1001234567,
                    IsActive = true,
                    SubscribedAt = new DateTime(2025, 07, 16, 12, 0, 0),
                    CreatedAt = new DateTime(2025, 07, 09, 12, 0, 0)
                }
            ];

            var mockUserRepository = new Mock<DatabaseUserRepository>(null!);
            mockUserRepository.Setup(x => x.GetQueryable()).Returns(_testUsers.BuildMock());
            _repository = mockUserRepository.Object;
        }

        [Fact]
        public async Task Get_ReturnsAllUsers()
        {
            var actual = await _repository.GetAsync(limit: _testUsers.Count);
            Assert.Equal(_testUsers.Count, actual.Result.Count());
            Assert.Null(actual.Next);
        }

        [Fact]
        public async Task Get_ReturnsAllUsersOneByOne()
        {
            List<User> actual = [];
            int start = 1;
            while (start > 0)
            {
                var current = await _repository.GetAsync(start: start, limit: 1);
                if (current.Result.Any())
                {
                    actual.AddRange(current.Result);
                }
                start = current.Next.GetValueOrDefault();
            }
            Assert.Equal(_testUsers.Count, actual.Count);
        }

        [Fact]
        public async Task Get_ReturnsLastThreeUsers()
        {
            int start = _testUsers.Count - 2;
            var actual = await _repository.GetAsync(start: start, limit: 3);
            Assert.Equal(3, actual.Result.Count());
            Assert.Null(actual.Next);
        }

        [Fact]
        public async Task Get_ReturnsEmptyResult_WhenStartExceedsTotalCount()
        {
            int start = _testUsers.Count + 1;
            var actual = await _repository.GetAsync(start: start);
            Assert.Empty(actual.Result);
            Assert.Null(actual.Next);
        }

        [Fact]
        public async Task Get_ReturnsFirstUser_WhenHasMoreUsers()
        {
            var actual = await _repository.GetAsync(start: 1, limit: 1);
            Assert.Single(actual.Result);
            Assert.True(actual.Next > 0);
        }

        [Fact]
        public async Task Get_ReturnsAllActiveUsers()
        {
            var activeUsers = _testUsers.Where(u => u.IsActive).ToArray();
            var actual = await _repository.GetAsync(
                filter: u => u.IsActive,
                limit: activeUsers.Length);
            Assert.Equal(activeUsers.Length, actual.Result.Count());
            Assert.Null(actual.Next);
        }
    }
}
