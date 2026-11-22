using System;
using System.Text.Json;
using Infrastructure.Common.Interfaces;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace FreeBook.Services
{
    /// <summary>
    /// TempData-backed implementation. Consume removes the entry so the alert is
    /// rendered exactly once after a redirect.
    /// </summary>
    public sealed class FlashMessageService : IFlashMessageService
    {
        internal const string TempDataKey = "FlashMessage";

        private readonly ITempDataDictionaryFactory _factory;
        private readonly IHttpContextAccessor _accessor;

        public FlashMessageService(ITempDataDictionaryFactory factory, IHttpContextAccessor accessor)
        {
            _factory = factory;
            _accessor = accessor;
        }

        public void Success(string title, string text) => Set(FlashMessageDto.Success(title, text));

        public void Error(string title, string text) => Set(FlashMessageDto.Error(title, text));

        public void Set(FlashMessageDto message)
        {
            var httpContext = _accessor.HttpContext
                ?? throw new InvalidOperationException("No HTTP context is available for flash messages.");
            var tempData = _factory.GetTempData(httpContext);
            // Cookie TempData only supports a fixed set of primitives/collections,
            // so the DTO travels as JSON and is rehydrated on consume.
            tempData[TempDataKey] = JsonSerializer.Serialize(message);
            tempData.Save();
        }

        public FlashMessageDto? Consume()
        {
            var httpContext = _accessor.HttpContext;
            if (httpContext is null)
                return null;

            var tempData = _factory.GetTempData(httpContext);
            if (!tempData.TryGetValue(TempDataKey, out var value))
                return null;

            var message = value is string json ? JsonSerializer.Deserialize<FlashMessageDto>(json) : null;
            tempData.Save();
            return message;
        }
    }
}
