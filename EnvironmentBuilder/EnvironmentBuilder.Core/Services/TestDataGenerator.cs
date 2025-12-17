using Bogus;
using EnvironmentBuilder.Core.Models;

namespace EnvironmentBuilder.Core.Services;

/// <summary>
/// Generates realistic test data using Bogus library
/// </summary>
public class TestDataGenerator
{
    private readonly Faker _faker;
    private readonly string _locale;

    public TestDataGenerator(string locale = "en_US")
    {
        _locale = locale;
        _faker = new Faker(locale);
    }

    /// <summary>
    /// Generate a batch of test users with realistic data
    /// </summary>
    public List<TestUser> GenerateUsers(UserGenerationConfig config)
    {
        var users = new List<TestUser>();
        
        for (int i = 0; i < config.Count; i++)
        {
            var user = config.RandomizeData 
                ? GenerateRandomUser(config, i) 
                : GenerateSequentialUser(config, i);
            users.Add(user);
        }

        return users;
    }

    /// <summary>
    /// Generate a user with randomized realistic data
    /// </summary>
    private TestUser GenerateRandomUser(UserGenerationConfig config, int index)
    {
        var firstName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var username = $"{config.Prefix}{config.StartNumber + index}";
        
        return new TestUser
        {
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            Email = _faker.Internet.Email(firstName, lastName),
            Password = config.PasswordMatchesUsername ? username : config.DefaultPassword,
            Title = _faker.Name.JobTitle(),
            Department = _faker.Commerce.Department(),
            PhoneNumber = _faker.Phone.PhoneNumber(),
            Location = $"{_faker.Address.City()}, {_faker.Address.StateAbbr()}",
            ObjectClasses = config.ObjectClasses,
            DistinguishedName = $"cn={username},{config.UserContainer}"
        };
    }

    /// <summary>
    /// Generate a user with sequential/predictable data
    /// </summary>
    private TestUser GenerateSequentialUser(UserGenerationConfig config, int index)
    {
        var number = config.StartNumber + index;
        var username = $"{config.Prefix}{number}";
        
        return new TestUser
        {
            Username = username,
            FirstName = $"Test{number}",
            LastName = $"User{number}",
            Email = $"{username}@test.local",
            Password = config.PasswordMatchesUsername ? username : config.DefaultPassword,
            Title = "Test User",
            Department = "Testing",
            PhoneNumber = $"555-{number:D4}",
            Location = "Test Location",
            ObjectClasses = config.ObjectClasses,
            DistinguishedName = $"cn={username},{config.UserContainer}"
        };
    }

    /// <summary>
    /// Generate organizational unit names
    /// </summary>
    public List<string> GenerateOUNames(int count)
    {
        var names = new List<string>();
        var departments = new[] { "Engineering", "Sales", "Marketing", "Finance", "HR", "IT", "Operations", "Legal", "Support", "Research" };
        
        for (int i = 0; i < count; i++)
        {
            if (i < departments.Length)
                names.Add(departments[i]);
            else
                names.Add(_faker.Commerce.Department() + i);
        }

        return names;
    }

    /// <summary>
    /// Generate group names
    /// </summary>
    public List<string> GenerateGroupNames(int count)
    {
        var groups = new List<string>();
        var prefixes = new[] { "Team", "Project", "Access", "Role", "Department" };
        
        for (int i = 0; i < count; i++)
        {
            var prefix = prefixes[i % prefixes.Length];
            groups.Add($"{prefix}_{_faker.Hacker.Noun()}_{i + 1}");
        }

        return groups;
    }

    /// <summary>
    /// Generate a random password meeting complexity requirements
    /// </summary>
    public string GeneratePassword(int length = 12)
    {
        return _faker.Internet.Password(length, false, @"[A-Za-z0-9!@#$%^&*]", "Aa1!");
    }

    /// <summary>
    /// Generate users in specific archetypes
    /// </summary>
    public List<TestUser> GenerateUsersByArchetype(UserArchetype archetype, int count, UserGenerationConfig baseConfig)
    {
        var users = new List<TestUser>();
        var archetypeFaker = new Faker(_locale);

        for (int i = 0; i < count; i++)
        {
            var user = archetype switch
            {
                UserArchetype.Executive => GenerateExecutive(archetypeFaker, baseConfig, i),
                UserArchetype.Developer => GenerateDeveloper(archetypeFaker, baseConfig, i),
                UserArchetype.SalesRep => GenerateSalesRep(archetypeFaker, baseConfig, i),
                UserArchetype.Support => GenerateSupportAgent(archetypeFaker, baseConfig, i),
                UserArchetype.Contractor => GenerateContractor(archetypeFaker, baseConfig, i),
                _ => GenerateRandomUser(baseConfig, i)
            };
            users.Add(user);
        }

        return users;
    }

    private TestUser GenerateExecutive(Faker faker, UserGenerationConfig config, int index)
    {
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        var username = $"exec{config.StartNumber + index}";
        var titles = new[] { "CEO", "CFO", "CTO", "VP", "Director", "Senior Director", "Managing Director" };

        return new TestUser
        {
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@executive.test",
            Password = config.DefaultPassword,
            Title = faker.PickRandom(titles),
            Department = "Executive",
            PhoneNumber = faker.Phone.PhoneNumber(),
            Location = "Corporate HQ",
            ObjectClasses = config.ObjectClasses,
            DistinguishedName = $"cn={username},{config.UserContainer}"
        };
    }

    private TestUser GenerateDeveloper(Faker faker, UserGenerationConfig config, int index)
    {
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        var username = $"dev{config.StartNumber + index}";
        var titles = new[] { "Software Engineer", "Senior Developer", "Tech Lead", "Architect", "DevOps Engineer" };

        return new TestUser
        {
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{username}@dev.test",
            Password = config.DefaultPassword,
            Title = faker.PickRandom(titles),
            Department = "Engineering",
            PhoneNumber = faker.Phone.PhoneNumber(),
            Location = faker.Address.City(),
            ObjectClasses = config.ObjectClasses,
            DistinguishedName = $"cn={username},{config.UserContainer}"
        };
    }

    private TestUser GenerateSalesRep(Faker faker, UserGenerationConfig config, int index)
    {
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        var username = $"sales{config.StartNumber + index}";

        return new TestUser
        {
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@sales.test",
            Password = config.DefaultPassword,
            Title = faker.PickRandom(new[] { "Sales Rep", "Account Executive", "Sales Manager", "Business Development" }),
            Department = "Sales",
            PhoneNumber = faker.Phone.PhoneNumber(),
            Location = $"{faker.Address.City()}, {faker.Address.StateAbbr()}",
            ObjectClasses = config.ObjectClasses,
            DistinguishedName = $"cn={username},{config.UserContainer}"
        };
    }

    private TestUser GenerateSupportAgent(Faker faker, UserGenerationConfig config, int index)
    {
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        var username = $"support{config.StartNumber + index}";

        return new TestUser
        {
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            Email = $"support{index + 1}@helpdesk.test",
            Password = config.DefaultPassword,
            Title = faker.PickRandom(new[] { "Support Agent", "Support Specialist", "Help Desk", "Customer Success" }),
            Department = "Support",
            PhoneNumber = faker.Phone.PhoneNumber(),
            Location = "Support Center",
            ObjectClasses = config.ObjectClasses,
            DistinguishedName = $"cn={username},{config.UserContainer}"
        };
    }

    private TestUser GenerateContractor(Faker faker, UserGenerationConfig config, int index)
    {
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        var username = $"contractor{config.StartNumber + index}";

        return new TestUser
        {
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{username}@contractors.external",
            Password = config.DefaultPassword,
            Title = "Contractor",
            Department = "External",
            PhoneNumber = faker.Phone.PhoneNumber(),
            Location = "Remote",
            ObjectClasses = config.ObjectClasses,
            DistinguishedName = $"cn={username},{config.UserContainer}"
        };
    }
}

public enum UserArchetype
{
    Generic,
    Executive,
    Developer,
    SalesRep,
    Support,
    Contractor
}

