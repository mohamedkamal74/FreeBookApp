using System.Reflection;
using FreeBook.Services;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace FreeBook.Tests;

public class CrudDtoContractTests
{
    private static readonly Type[] CrudDtoTypes =
    {
        typeof(FlashMessageDto),
        typeof(RoleDto), typeof(UserDto),
        typeof(AuthorDto), typeof(AuthorCommand),
        typeof(CategoryDto), typeof(CategoryCommand), typeof(NewCategory),
        typeof(SubCategoryDto), typeof(SubCategoryCommand),
        typeof(BookDto), typeof(BookCommand),
        typeof(BookCopyDto), typeof(BookCopyCommand),
        typeof(GovernorateDto), typeof(GovernorateCommand),
        typeof(AreaDto), typeof(AreaCommand),
        typeof(SubscriberDto), typeof(SubscriberCommand),
        typeof(SubscriptionDto), typeof(SubscriptionCommand),
        typeof(RentalDto), typeof(RentalCommand), typeof(RentalCopyDto), typeof(RentalCopyCommand),
        typeof(LogCategoryDto),
        typeof(NotificationDto), typeof(NotificationCommand)
    };

    private static readonly Type[] RepositoryContracts =
    {
        typeof(IServiceRepository<,>),
        typeof(IServiceRepositoryLog<>),
        typeof(IAccountQueryService),
        typeof(IAuthorRepository),
        typeof(ISubscriptionRepository),
        typeof(IRentalRepository),
        typeof(INotificationRepository)
    };

    private static readonly Type[] AccountViewModels =
    {
        typeof(RegisterViewModel), typeof(RolesViewModel)
    };

    [Theory]
    [MemberData(nameof(CrudDtoTypesData))]
    public void CrudDto_DoesNotExposeDominEntityTypes(Type dtoType)
    {
        foreach (var property in dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Assert.False(ContainsDominEntity(property.PropertyType),
                $"{dtoType.Name}.{property.Name} exposes a Domin.Entity type: {property.PropertyType}");
        }
    }

    [Theory]
    [MemberData(nameof(RepositoryContractsData))]
    public void RepositoryContract_DoesNotExposeDominEntityTypes(Type contract)
    {
        var methods = contract.IsGenericTypeDefinition
            ? contract.GetMethods()
            : contract.GetMethods().Concat(contract.GetInterfaces().SelectMany(i => i.GetMethods()));

        foreach (var method in methods)
        {
            Assert.False(ContainsDominEntity(method.ReturnType),
                $"{contract.Name}.{method.Name} returns a Domin.Entity type: {method.ReturnType}");
            foreach (var parameter in method.GetParameters())
            {
                Assert.False(ContainsDominEntity(parameter.ParameterType),
                    $"{contract.Name}.{method.Name}({parameter.Name}) exposes a Domin.Entity type: {parameter.ParameterType}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(AccountViewModelsData))]
    public void AccountViewModel_DoesNotExposePersistenceTypes(Type viewModelType)
    {
        foreach (var property in viewModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Assert.False(ContainsDominEntity(property.PropertyType),
                $"{viewModelType.Name}.{property.Name} exposes a Domin.Entity type: {property.PropertyType}");
            Assert.False(ContainsIdentityEntity(property.PropertyType),
                $"{viewModelType.Name}.{property.Name} exposes an Identity entity: {property.PropertyType}");
        }
    }

    [Fact]
    public void FlashMessage_RoundTripsThroughTempDataOnce()
    {
        var provider = new MemoryTempDataProvider();
        var factory = new TempDataDictionaryFactory(provider);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        IFlashMessageService flash = new FlashMessageService(factory, accessor);

        flash.Success("Saved", "The book was saved.");

        var message = flash.Consume();
        Assert.NotNull(message);
        Assert.Equal(FlashMessageDto.SuccessType, message!.Type);
        Assert.Equal("Saved", message.Title);
        Assert.Equal("The book was saved.", message.Text);
        Assert.Null(flash.Consume());
    }

    private static bool ContainsDominEntity(Type type)
    {
        if (type.Namespace?.StartsWith("Domin.Entity", StringComparison.Ordinal) == true)
            return true;
        if (type.IsGenericType)
            return type.GetGenericArguments().Any(ContainsDominEntity);
        return type.IsArray && ContainsDominEntity(type.GetElementType()!);
    }

    private static bool ContainsIdentityEntity(Type type)
    {
        if (typeof(IdentityRole).IsAssignableFrom(type) || typeof(IdentityUser<string>).IsAssignableFrom(type))
            return true;
        if (type.IsGenericType)
            return type.GetGenericArguments().Any(ContainsIdentityEntity);
        return type.IsArray && ContainsIdentityEntity(type.GetElementType()!);
    }

    public static IEnumerable<object[]> CrudDtoTypesData() => CrudDtoTypes.Select(t => new object[] { t });
    public static IEnumerable<object[]> RepositoryContractsData() => RepositoryContracts.Select(t => new object[] { t });
    public static IEnumerable<object[]> AccountViewModelsData() => AccountViewModels.Select(t => new object[] { t });

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();

        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>(_data);

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data.Clear();
            foreach (var pair in values)
                _data[pair.Key] = pair.Value;
        }
    }
}
